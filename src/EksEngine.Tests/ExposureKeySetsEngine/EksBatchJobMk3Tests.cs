// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NCrunch.Framework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.Processors;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.Processors.Rcp;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain.Rcp;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Eks.Publishing.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands.DiagnosisKeys.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands.FormatV1;
using NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands.Stuffing;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Workflow.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Workflow.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.TestFramework;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Tests.ExposureKeySetsEngine
{
    public abstract class EksBatchJobMk3Tests
    {
        private readonly FakeEksConfig _fakeEksConfig;

        private readonly WorkflowDbContext _workflowContext;
        private readonly DkSourceDbContext _dkSourceContext;
        private readonly EksPublishingJobDbContext _eksPublishingJobDbContext;
        private readonly ContentDbContext _contentDbContext;

        private readonly IWrappedEfExtensions _efExtensions;
        private readonly Mock<IOutboundFixedCountriesOfInterestSetting> _outboundCountriesMock;

        private readonly LoggerFactory _lf;
        private readonly IUtcDateTimeProvider _dateTimeProvider;

        private ExposureKeySetBatchJobMk3 _engine;
        private readonly SnapshotWorkflowTeksToDksCommand _snapshot;

        protected EksBatchJobMk3Tests(DbContextOptions<WorkflowDbContext> workflowContextOptions, DbContextOptions<DkSourceDbContext> dkSourceDbContextOptions, DbContextOptions<EksPublishingJobDbContext> eksPublishingJobDbContextOptions, DbContextOptions<ContentDbContext> contentDbContextOptions, IWrappedEfExtensions efExtensions)
        {
            _efExtensions = efExtensions ?? throw new ArgumentNullException(nameof(efExtensions));

            _workflowContext = new WorkflowDbContext(workflowContextOptions);
            _workflowContext.Database.EnsureCreated();
            _dkSourceContext = new DkSourceDbContext(dkSourceDbContextOptions);
            _dkSourceContext.Database.EnsureCreated();
            _eksPublishingJobDbContext = new EksPublishingJobDbContext(eksPublishingJobDbContextOptions);
            _eksPublishingJobDbContext.Database.EnsureCreated();
            _contentDbContext = new ContentDbContext(contentDbContextOptions);
            _contentDbContext.Database.EnsureCreated();

            _dateTimeProvider = new StandardUtcDateTimeProvider();
            _fakeEksConfig = new FakeEksConfig { LifetimeDays = 14, PageSize = 1000, TekCountMax = 10, TekCountMin = 5 };
            _lf = new LoggerFactory();

            _snapshot = new SnapshotWorkflowTeksToDksCommand(_lf.CreateLogger<SnapshotWorkflowTeksToDksCommand>(),
                new StandardUtcDateTimeProvider(),
                new TransmissionRiskLevelCalculationMk2(),
                _workflowContext,
                _dkSourceContext,
                _efExtensions,
                new IDiagnosticKeyProcessor[] { }
            );

            _outboundCountriesMock = new Mock<IOutboundFixedCountriesOfInterestSetting>();
            _outboundCountriesMock.Setup(x => x.CountriesOfInterest).Returns(new[] { "CY", "BG" });
        }

        private void Write(TekReleaseWorkflowStateEntity[] workflows)
        {
            _workflowContext.BulkDelete(_workflowContext.KeyReleaseWorkflowStates.AsNoTracking().ToList());

            _workflowContext.KeyReleaseWorkflowStates.AddRange(workflows);
            _workflowContext.TemporaryExposureKeys.AddRange(workflows.SelectMany(x => x.Teks));
            _workflowContext.SaveChanges();

            Assert.Equal(workflows.Length, _workflowContext.KeyReleaseWorkflowStates.Count());
            Assert.Equal(workflows.Sum(x => x.Teks.Count), _workflowContext.TemporaryExposureKeys.Count());

            _snapshot.ExecuteAsync().GetAwaiter().GetResult();
        }

        private static TekEntity CreateTek(int rsn)
        {
            return new TekEntity { RollingStartNumber = rsn, RollingPeriod = 2, KeyData = new byte[16], PublishAfter = DateTime.UtcNow.AddHours(-1) };
        }

        private static TekReleaseWorkflowStateEntity Create(DateTime now, InfectiousPeriodType symptomatic, params TekEntity[] items)
        {
            return new TekReleaseWorkflowStateEntity
            {
                BucketId = new byte[0],
                ConfirmationKey = new byte[0],
                AuthorisedByCaregiver = now.AddHours(1),
                Created = now,
                ValidUntil = now.AddDays(1),
                StartDateOfTekInclusion = now.AddDays(-1).Date,
                IsSymptomatic = symptomatic,
                Teks = items
            };
        }

        private EksEngineResult RunEngine()
        {
            _engine = new ExposureKeySetBatchJobMk3(
                _fakeEksConfig,
                new FakeEksBuilder(),
                _eksPublishingJobDbContext,
                new StandardUtcDateTimeProvider(),
                new EksEngineLoggingExtensions(_lf.CreateLogger<EksEngineLoggingExtensions>()),
                new EksStuffingGeneratorMk2(new TransmissionRiskLevelCalculationMk2(), new StandardRandomNumberGenerator(), _dateTimeProvider, _fakeEksConfig),
                new SnapshotDiagnosisKeys(new SnapshotLoggingExtensions(new TestLogger<SnapshotLoggingExtensions>()), _dkSourceContext, _eksPublishingJobDbContext,
                    new Infectiousness(new Dictionary<InfectiousPeriodType, HashSet<int>>{
                        {
                            InfectiousPeriodType.Symptomatic,
                            new HashSet<int>() { -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }
                        },
                        {
                            InfectiousPeriodType.Asymptomatic,
                            new HashSet<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }
                        }
                    })),
                new MarkDiagnosisKeysAsUsedLocally(_dkSourceContext, _fakeEksConfig, _eksPublishingJobDbContext, _lf.CreateLogger<MarkDiagnosisKeysAsUsedLocally>()),
                new EksJobContentWriter(_contentDbContext, _eksPublishingJobDbContext, new Sha256HexPublishingIdService(),
                    new EksJobContentWriterLoggingExtensions(_lf.CreateLogger<EksJobContentWriterLoggingExtensions>())),
                new WriteStuffingToDiagnosisKeys(_dkSourceContext, _eksPublishingJobDbContext,
                    new IDiagnosticKeyProcessor[] {
                    new FixedCountriesOfInterestOutboundDiagnosticKeyProcessor(_outboundCountriesMock.Object),
                    new NlToEfgsDsosDiagnosticKeyProcessorMk1()
                    }
                    ),
                _efExtensions);

            return _engine.ExecuteAsync().GetAwaiter().GetResult();
        }

        private class FakeEksConfig : IEksConfig
        {
            public int LifetimeDays { get; set; } = 14;
            public int TekCountMax { get; set; } = 20;
            public int TekCountMin { get; set; } = 10;
            public int PageSize { get; set; } = 100;
            public bool CleanupDeletesData => throw new NotImplementedException(); //ncrunch: no coverage
        }

        private class FakeEksBuilder : IEksBuilder
        {
            public async Task<byte[]> BuildAsync(TemporaryExposureKeyArgs[] keys) => new byte[] { 1 };
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void FireSameEngineTwice()
        {
            RunEngine();
            Assert.Throws<InvalidOperationException>(() => _engine.ExecuteAsync().GetAwaiter().GetResult());
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void FireTwice()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();
            _dkSourceContext.Truncate<DiagnosisKeyEntity>();

            var wfs = new[]
            {
                Create(_dateTimeProvider.Snapshot, InfectiousPeriodType.Symptomatic, CreateTek(DateTime.UtcNow.Date.AddDays(-2).ToRollingStartNumber()))
            };

            Write(wfs);

            // Act
            var result = RunEngine();

            // Assert
            Assert.True(result.Started > new DateTime(2020, 8, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(1, result.InputCount);
            Assert.Equal(1, result.FilteredInputCount);
            Assert.Equal(4, result.StuffingCount);
            Assert.Equal(5, result.OutputCount);
            Assert.Equal(0, result.TransmissionRiskNoneCount);
            Assert.NotEmpty(result.EksInfo);

            Assert.Equal(0, result.ReconcileOutputCount);
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(_contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet), result.EksInfo.Length);
            Assert.Equal(_dkSourceContext.DiagnosisKeys.Count(x => x.PublishedLocally), result.InputCount + result.StuffingCount);

            Assert.True(result.TotalSeconds > 0);

            result = RunEngine();

            Assert.Equal(0, result.InputCount);
            Assert.Equal(0, result.StuffingCount);
            Assert.Equal(0, result.OutputCount);
            Assert.Equal(0, result.TransmissionRiskNoneCount);
            Assert.Empty(result.EksInfo);

            Assert.Equal(0, result.ReconcileOutputCount);
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(1, _contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet));
            Assert.Equal(5, _dkSourceContext.DiagnosisKeys.Count(x => x.PublishedLocally));

            Assert.True(result.TotalSeconds > 0);
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void Teks0_NothingToSeeHereMoveAlong()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();
            _workflowContext.Truncate<TekEntity>();

            // Act
            var result = RunEngine();

            // Assert
            Assert.True(result.Started > new DateTime(2020, 8, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(0, result.InputCount);
            Assert.Equal(0, result.FilteredInputCount);
            Assert.Equal(0, result.StuffingCount);
            Assert.Equal(0, result.OutputCount);
            Assert.Empty(result.EksInfo);
            Assert.Equal(0, result.TransmissionRiskNoneCount);

            Assert.Equal(0, result.ReconcileOutputCount);
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(_contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet), result.EksInfo.Length);
            Assert.Equal(_workflowContext.TemporaryExposureKeys.Count(x => x.PublishingState == PublishingState.Published), result.InputCount);

            Assert.True(result.TotalSeconds > 0);
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void Teks1_GetStuffed()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();
            _dkSourceContext.Truncate<DiagnosisKeyEntity>();

            var wfs = new[]
            {
                Create(_dateTimeProvider.Snapshot, InfectiousPeriodType.Symptomatic, CreateTek(DateTime.UtcNow.Date.AddDays(-2).ToRollingStartNumber()))
            };

            Write(wfs);

            // Act
            var result = RunEngine();

            // Assert
            Assert.True(result.Started > new DateTime(2020, 8, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(1, result.InputCount);
            Assert.Equal(1, result.FilteredInputCount);
            Assert.Equal(4, result.StuffingCount);
            Assert.Equal(5, result.OutputCount);
            Assert.Single(result.EksInfo);
            Assert.Equal(5, result.EksInfo[0].TekCount);
            Assert.Equal(0, result.TransmissionRiskNoneCount);

            Assert.Equal(0, result.ReconcileOutputCount);
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(_contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet), result.EksInfo.Length);
            Assert.Equal(_dkSourceContext.DiagnosisKeys.Count(x => x.PublishedLocally), result.InputCount + result.StuffingCount);

            Assert.True(result.TotalSeconds > 0);
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void Tek5_NotStuffed()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();
            _dkSourceContext.Truncate<DiagnosisKeyEntity>();

            var teks = Enumerable.Range(1, 5)
                .Select(x => CreateTek(DateTime.UtcNow.Date.AddDays(-2).ToRollingStartNumber()))
                .ToArray();

            var wfs = new[]
            {
                Create(_dateTimeProvider.Snapshot, InfectiousPeriodType.Symptomatic, teks)
            };

            Write(wfs);

            // Act
            var result = RunEngine();

            // Assert
            Assert.True(result.Started > new DateTime(2020, 8, 10, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(5, result.InputCount);
            Assert.Equal(5, result.FilteredInputCount);
            Assert.Equal(0, result.StuffingCount);
            Assert.Equal(5, result.OutputCount);
            Assert.Single(result.EksInfo);
            Assert.Equal(5, result.EksInfo[0].TekCount);
            Assert.Equal(0, result.TransmissionRiskNoneCount);

            Assert.Equal(0, result.ReconcileOutputCount);
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(_contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet), result.EksInfo.Length);
            Assert.Equal(_dkSourceContext.DiagnosisKeys.Count(x => x.PublishedLocally), result.InputCount);

            Assert.True(result.TotalSeconds > 0);
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void Tek5_Stuffed()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();
            _dkSourceContext.Truncate<DiagnosisKeyEntity>();

            var teks = Enumerable.Range(1, 5)
                .Select(x => CreateTek(DateTime.UtcNow.Date.AddDays(-2).ToRollingStartNumber()))
                .ToArray();

            var wfs = new[]
            {
                Create(_dateTimeProvider.Snapshot, InfectiousPeriodType.Asymptomatic, teks)
            };

            Write(wfs);

            // Act
            var result = RunEngine();

            // Assert
            Assert.True(result.Started > new DateTime(2020, 8, 10, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(5, result.InputCount);
            Assert.Equal(0, result.FilteredInputCount);
            Assert.Equal(5, result.StuffingCount);
            Assert.Equal(5, result.OutputCount);
            Assert.Single(result.EksInfo);
            Assert.Equal(5, result.EksInfo[0].TekCount);
            Assert.Equal(0, result.TransmissionRiskNoneCount);

            Assert.Equal(5, result.ReconcileOutputCount);
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(_contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet), result.EksInfo.Length);
            Assert.Equal(_dkSourceContext.DiagnosisKeys.Count(x => x.PublishedLocally), result.InputCount);

            Assert.True(result.TotalSeconds > 0);
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void Tek5_AsymptomaticNotStuffed()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();
            _dkSourceContext.Truncate<DiagnosisKeyEntity>();

            var teks = Enumerable.Range(1, 5)
                .Select(x => CreateTek(DateTime.UtcNow.Date.AddDays(0).ToRollingStartNumber()))
                .ToArray();

            var wfs = new[]
            {
                Create(_dateTimeProvider.Snapshot, InfectiousPeriodType.Asymptomatic, teks)
            };

            Write(wfs);

            // Act
            var result = RunEngine();

            // Assert
            Assert.True(result.Started > new DateTime(2020, 8, 10, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(5, result.InputCount);
            Assert.Equal(5, result.FilteredInputCount);
            Assert.Equal(0, result.StuffingCount);
            Assert.Equal(5, result.OutputCount);
            Assert.Single(result.EksInfo);
            Assert.Equal(5, result.EksInfo[0].TekCount);
            Assert.Equal(0, result.TransmissionRiskNoneCount);

            Assert.Equal(0, result.ReconcileOutputCount); //InputCount + StuffingCount - TransmissionRiskNoneCount - OutputCount;
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(_contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet), result.EksInfo.Length);
            Assert.Equal(_dkSourceContext.DiagnosisKeys.Count(x => x.PublishedLocally), result.InputCount);

            Assert.True(result.TotalSeconds > 0);
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void Tek5_Asymptomatic2Stuffed()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();
            _dkSourceContext.Truncate<DiagnosisKeyEntity>();

            var teks = new List<TekEntity>();
            teks.AddRange(Enumerable.Range(1, 3).Select(x => CreateTek(DateTime.UtcNow.Date.AddDays(-1).ToRollingStartNumber()))); // dsos = 0
            teks.AddRange(Enumerable.Range(1, 2).Select(x => CreateTek(DateTime.UtcNow.Date.AddDays(-2).ToRollingStartNumber()))); // dsos = -1

            var wfs = new[]
            {
                Create(_dateTimeProvider.Snapshot, InfectiousPeriodType.Asymptomatic, teks.ToArray())
            };

            Write(wfs);

            // Act
            var result = RunEngine();

            // Assert
            Assert.True(result.Started > new DateTime(2020, 8, 10, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(5, result.InputCount);
            Assert.Equal(3, result.FilteredInputCount);
            Assert.Equal(2, result.StuffingCount);
            Assert.Equal(5, result.OutputCount);
            Assert.Single(result.EksInfo);
            Assert.Equal(5, result.EksInfo[0].TekCount);
            Assert.Equal(0, result.TransmissionRiskNoneCount);

            Assert.Equal(2, result.ReconcileOutputCount); //InputCount + StuffingCount - TransmissionRiskNoneCount - OutputCount;
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(_contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet), result.EksInfo.Length);
            Assert.Equal(_dkSourceContext.DiagnosisKeys.Count(x => x.PublishedLocally), result.InputCount);

            Assert.True(result.TotalSeconds > 0);
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void Tek10_NotStuffed()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();
            _dkSourceContext.Truncate<DiagnosisKeyEntity>();

            var teks = Enumerable.Range(1, 10)
                .Select(x => CreateTek(DateTime.UtcNow.Date.AddDays(-2).ToRollingStartNumber()))
                .ToArray();

            var wfs = new[]
            {
                Create(_dateTimeProvider.Snapshot, InfectiousPeriodType.Symptomatic, teks)
            };

            Write(wfs);

            // Act
            var result = RunEngine();

            // Assert
            Assert.True(result.Started > new DateTime(2020, 8, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(10, result.InputCount);
            Assert.Equal(10, result.FilteredInputCount);
            Assert.Equal(0, result.StuffingCount);
            Assert.Equal(10, result.OutputCount);
            Assert.Single(result.EksInfo);
            Assert.Equal(10, result.EksInfo[0].TekCount);
            Assert.Equal(0, result.TransmissionRiskNoneCount);

            Assert.Equal(0, result.ReconcileOutputCount);
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(_contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet), result.EksInfo.Length);
            Assert.Equal(_dkSourceContext.DiagnosisKeys.Count(x => x.PublishedLocally), result.InputCount);

            Assert.True(result.TotalSeconds > 0);
        }

        [Fact]
        [ExclusivelyUses(nameof(EksBatchJobMk3Tests))]
        public void Tek11_NotStuffed_2Eks()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();
            _dkSourceContext.Truncate<DiagnosisKeyEntity>();

            var teks = Enumerable.Range(1, 11)
                .Select(x => CreateTek(DateTime.UtcNow.Date.AddDays(-2).ToRollingStartNumber()))
                .ToArray();

            var wfs = new[]
            {
                Create(_dateTimeProvider.Snapshot, InfectiousPeriodType.Symptomatic, teks)
            };

            Write(wfs);

            // Act
            var result = RunEngine();

            // Assert
            Assert.True(result.Started > new DateTime(2020, 8, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(11, result.InputCount);
            Assert.Equal(11, result.FilteredInputCount);
            Assert.Equal(0, result.StuffingCount);
            Assert.Equal(11, result.OutputCount);
            Assert.Equal(2, result.EksInfo.Length);
            Assert.Equal(10, result.EksInfo[0].TekCount);
            Assert.Equal(1, result.EksInfo[1].TekCount);
            Assert.Equal(0, result.TransmissionRiskNoneCount);

            Assert.Equal(0, result.ReconcileOutputCount);
            Assert.Equal(0, result.ReconcileEksSumCount);

            Assert.Equal(_contentDbContext.Content.Count(x => x.Type == ContentTypes.ExposureKeySet), result.EksInfo.Length);
            Assert.Equal(_dkSourceContext.DiagnosisKeys.Count(x => x.PublishedLocally), result.InputCount);

            Assert.True(result.TotalSeconds > 0);
        }
    }
}
