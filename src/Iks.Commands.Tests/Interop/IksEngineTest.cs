// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NCrunch.Framework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Commands.Outbound;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Commands.Publishing;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Downloader.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Downloader.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Publishing.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Uploader.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Workflow.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.TestDataGeneration.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.TestFramework;
using Serilog.Extensions.Logging;
using Xunit;
using EfgsReportType = Iks.Protobuf.EfgsReportType;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Commands.Tests.Interop
{
    /// <summary>
    /// Tests the command sequence for:
    /// Fake inbound IKS in DB
    /// Snapshot to DK Source
    /// Snapshot for EKS
    /// Build EKS
    /// </summary>
    public abstract class IksEngineTest
    {
        private readonly IDbProvider<WorkflowDbContext> _workflowDbContextProvider;
        private readonly IDbProvider<IksInDbContext> _iksInDbContextProvider;
        private readonly IDbProvider<DkSourceDbContext> _dkSourceDbContextProvider;
        private readonly IDbProvider<IksPublishingJobDbContext> _iksPublishingJobDbContextProvider;
        private readonly IDbProvider<IksOutDbContext> _iksOutDbContextProvider;

        private readonly IWrappedEfExtensions _efExtensions;

        private readonly ILoggerFactory _loggerFactory = new SerilogLoggerFactory();

        private readonly Mock<IIksConfig> _iksConfigMock = new Mock<IIksConfig>(MockBehavior.Strict);
        private readonly Mock<IOutboundFixedCountriesOfInterestSetting> _countriesConfigMock = new Mock<IOutboundFixedCountriesOfInterestSetting>(MockBehavior.Strict);
        private readonly Mock<IUtcDateTimeProvider> _utcDateTimeProviderMock = new Mock<IUtcDateTimeProvider>(MockBehavior.Strict);

        protected IksEngineTest(IDbProvider<WorkflowDbContext> workflowDbContextProvider, IDbProvider<IksInDbContext> iksInDbContextProvider, IDbProvider<DkSourceDbContext> dkSourceDbContextProvider, IDbProvider<IksPublishingJobDbContext> iksPublishingJobDbContextProvider, IDbProvider<IksOutDbContext> iksOutDbContextProvider, IWrappedEfExtensions efExtensions)
        {
            _iksInDbContextProvider = iksInDbContextProvider ?? throw new ArgumentNullException(nameof(iksInDbContextProvider));
            _dkSourceDbContextProvider = dkSourceDbContextProvider ?? throw new ArgumentNullException(nameof(dkSourceDbContextProvider));
            _iksPublishingJobDbContextProvider = iksPublishingJobDbContextProvider ?? throw new ArgumentNullException(nameof(iksPublishingJobDbContextProvider));
            _iksOutDbContextProvider = iksOutDbContextProvider ?? throw new ArgumentNullException(nameof(iksOutDbContextProvider));
            _workflowDbContextProvider = workflowDbContextProvider ?? throw new ArgumentNullException(nameof(workflowDbContextProvider));
            _efExtensions = efExtensions ?? throw new ArgumentNullException(nameof(efExtensions));
        }

        private IksEngine Create()
        {
            _iksConfigMock.Setup(x => x.ItemCountMax).Returns(750);
            _iksConfigMock.Setup(x => x.PageSize).Returns(1000);
            _countriesConfigMock.Setup(x => x.CountriesOfInterest).Returns(new[] { "GB", "AU" });
            return new IksEngine(
                _loggerFactory.CreateLogger<IksEngine>(),
                new IksInputSnapshotCommand(_loggerFactory.CreateLogger<IksInputSnapshotCommand>(), _dkSourceDbContextProvider.CreateNew(), _iksPublishingJobDbContextProvider.CreateNew, _countriesConfigMock.Object),
                new IksFormatter(),
                _iksConfigMock.Object,
                _utcDateTimeProviderMock.Object,
                new MarkDiagnosisKeysAsUsedByIks(_dkSourceDbContextProvider.CreateNew, _iksConfigMock.Object, _iksPublishingJobDbContextProvider.CreateNew, _loggerFactory.CreateLogger<MarkDiagnosisKeysAsUsedByIks>()),
                new IksJobContentWriter(_iksOutDbContextProvider.CreateNew, _iksPublishingJobDbContextProvider.CreateNew, _loggerFactory.CreateLogger<IksJobContentWriter>()),
                _iksPublishingJobDbContextProvider.CreateNew,
                _efExtensions
            );
        }


        [InlineData(2)]
        [Theory]
        [ExclusivelyUses(nameof(IksEngineTest))]
        public async Task Execute(int iksCount)
        {
            //Mocks
            _iksConfigMock.Setup(x => x.ItemCountMax).Returns(750);
            _iksConfigMock.Setup(x => x.PageSize).Returns(1000);
            _utcDateTimeProviderMock.Setup(x => x.Snapshot).Returns(new DateTime(2020, 11, 16, 15, 14, 13, DateTimeKind.Utc));

            GenerateIks(iksCount);

            Assert.Equal(iksCount, _iksInDbContextProvider.CreateNew().Received.Count(x => x.Accepted == null));
            Assert.Equal(0, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count(x => x.PublishedLocally == false));
            Assert.Equal(0, _iksOutDbContextProvider.CreateNew().Iks.Count());

            //Act
            var result = await Create().ExecuteAsync();

            //TODO Assert.Equal(tekCount, result.InputCount);
            Assert.Equal(0, result.OutputCount);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.ReconcileEksSumCount);
            Assert.Equal(0, result.ReconcileOutputCount);

            //Don't publish DKs from EFGS
            Assert.Equal(0, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count());
            Assert.Equal(0, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count(x => x.PublishedToEfgs));
        }

        private void GenerateIks(int iksCount)
        {
            //Add an IKS or 2.
            var idk = new InteropKeyFormatterArgs
            {
                TransmissionRiskLevel = 1,
                CountriesOfInterest = new[] { "DE" },
                ReportType = EfgsReportType.ConfirmedTest,
                Origin = "DE",
                DaysSinceSymtpomsOnset = 0,
                Value = new DailyKey
                {
                    RollingStartNumber = _utcDateTimeProviderMock.Object.Snapshot.Date.ToRollingStartNumber(),
                    RollingPeriod = UniversalConstants.RollingPeriodRange.Hi,
                    KeyData = new byte[UniversalConstants.DailyKeyDataByteCount]
                }
            };

            var input = Enumerable.Range(0, iksCount).Select(_ =>
                new IksInEntity
                {
                    Created = _utcDateTimeProviderMock.Object.Snapshot,
                    BatchTag = "argle",
                    Content = new IksFormatter().Format(new[] { idk }),
                    //Accepted = 
                }).ToArray();

            var iksInDb = _iksInDbContextProvider.CreateNew();
            iksInDb.Received.AddRange(input);
            iksInDb.SaveChanges();
        }

        [Fact]
        [ExclusivelyUses(nameof(IksEngineTest))]
        public async Task Empty()
        {
            //Mocks
            _utcDateTimeProviderMock.Setup(x => x.Snapshot).Returns(new DateTime(2020, 11, 16, 15, 14, 13, DateTimeKind.Utc));

            Assert.Equal(0, _iksInDbContextProvider.CreateNew().Received.Count());
            Assert.Equal(0, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count());
            Assert.Equal(0, _iksOutDbContextProvider.CreateNew().Iks.Count());

            //Act
            var result = await Create().ExecuteAsync();

            Assert.Equal(0, result.InputCount);
            Assert.Equal(0, result.OutputCount);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.ReconcileEksSumCount);
            Assert.Equal(0, result.ReconcileOutputCount);
            Assert.Equal(0, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count());
            Assert.Equal(0, _iksOutDbContextProvider.CreateNew().Iks.Count());
        }

        [Fact]
        [ExclusivelyUses(nameof(IksEngineTest))]
        public async Task ExecuteFromWorkflows()
        {
            //Mocks
            _iksConfigMock.Setup(x => x.ItemCountMax).Returns(750);
            _iksConfigMock.Setup(x => x.PageSize).Returns(1000);
            _utcDateTimeProviderMock.Setup(x => x.Snapshot).Returns(new DateTime(2020, 11, 16, 15, 14, 13, DateTimeKind.Utc));

            var usableDkCount = await new WorkflowTestDataGenerator(
                _workflowDbContextProvider.CreateNew(),
                _dkSourceDbContextProvider,
                _efExtensions
            ).GenerateAndAuthoriseWorkflowsAsync();

            //Act
            var result = await Create().ExecuteAsync();

            Assert.Equal(usableDkCount, result.InputCount);
            Assert.Equal(usableDkCount, result.OutputCount); //No filters...
            Assert.Single(result.Items);

            Assert.Equal(0, result.ReconcileEksSumCount);
            Assert.Equal(0, result.ReconcileOutputCount);

            var itemResult = result.Items[0];
            Assert.Equal(usableDkCount, itemResult.ItemCount);

            Assert.Equal(usableDkCount, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count());
            Assert.Equal(usableDkCount, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count(x => x.PublishedToEfgs));
            Assert.Equal(1, _iksOutDbContextProvider.CreateNew().Iks.Count());
        }

        [Fact]
        [ExclusivelyUses(nameof(IksEngineTest))]
        public async Task ExecuteFromWorkflowsTwice()
        {
            //Mocks
            _iksConfigMock.Setup(x => x.ItemCountMax).Returns(750);
            _iksConfigMock.Setup(x => x.PageSize).Returns(1000);
            _utcDateTimeProviderMock.Setup(x => x.Snapshot).Returns(new DateTime(2020, 11, 16, 15, 14, 13, DateTimeKind.Utc));

            var usableDkCount = await new WorkflowTestDataGenerator(
                _workflowDbContextProvider.CreateNew(),
                _dkSourceDbContextProvider,
                _efExtensions
            ).GenerateAndAuthoriseWorkflowsAsync();

            //Act
            var result = await Create().ExecuteAsync();

            Assert.Equal(usableDkCount, result.InputCount);
            Assert.Equal(usableDkCount, result.OutputCount); //No filters...
            Assert.Single(result.Items);

            Assert.Equal(0, result.ReconcileEksSumCount);
            Assert.Equal(0, result.ReconcileOutputCount);

            var itemResult = result.Items[0];
            Assert.Equal(usableDkCount, itemResult.ItemCount);

            Assert.Equal(usableDkCount, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count());
            Assert.Equal(usableDkCount, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count(x => x.PublishedToEfgs));
            Assert.Equal(1, _iksOutDbContextProvider.CreateNew().Iks.Count());

            //Act
            var result2 = await Create().ExecuteAsync();
            Assert.Equal(0, result2.InputCount);
            Assert.Equal(0, result2.OutputCount); //No filters...
            Assert.Empty(result2.Items);
            Assert.Equal(0, result2.ReconcileEksSumCount);
            Assert.Equal(0, result2.ReconcileOutputCount);
            //Unchanged
            Assert.Equal(usableDkCount, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count());
            Assert.Equal(usableDkCount, _dkSourceDbContextProvider.CreateNew().DiagnosisKeys.Count(x => x.PublishedToEfgs));
            Assert.Equal(1, _iksOutDbContextProvider.CreateNew().Iks.Count());
        }
    }
}
