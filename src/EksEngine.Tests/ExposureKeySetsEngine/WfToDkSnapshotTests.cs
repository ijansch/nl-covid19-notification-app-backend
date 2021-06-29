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
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.Processors;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain.Rcp;
using NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands.DiagnosisKeys.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Workflow.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Workflow.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.TestFramework;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Tests.ExposureKeySetsEngine
{
    public abstract class WfToDkSnapshotTests : IDisposable
    {
        #region Implementation

        private readonly IDbProvider<WorkflowDbContext> _workflowDbProvider;
        private readonly IDbProvider<DkSourceDbContext> _dkSourceDbProvider;
        private readonly IWrappedEfExtensions _efExtensions;
        private readonly LoggerFactory _lf;
        private readonly Mock<IUtcDateTimeProvider> _dateTimeProvider;

        protected WfToDkSnapshotTests(IDbProvider<WorkflowDbContext> workflowFac, IDbProvider<DkSourceDbContext> dkSourceFac, IWrappedEfExtensions efExtensions)
        {
            _workflowDbProvider = workflowFac ?? throw new ArgumentNullException(nameof(workflowFac));
            _dkSourceDbProvider = dkSourceFac ?? throw new ArgumentNullException(nameof(dkSourceFac));
            _efExtensions = efExtensions ?? throw new ArgumentNullException(nameof(efExtensions));
            _dateTimeProvider = new Mock<IUtcDateTimeProvider>();
            _lf = new LoggerFactory();
        }

        private SnapshotWorkflowTeksToDksCommand Create()
        {
            return new SnapshotWorkflowTeksToDksCommand(_lf.CreateLogger<SnapshotWorkflowTeksToDksCommand>(),
                _dateTimeProvider.Object,
                new TransmissionRiskLevelCalculationMk2(),
                _workflowDbProvider.CreateNew(),
                _workflowDbProvider.CreateNew(),
                _dkSourceDbProvider.CreateNew,
                _efExtensions,
                new IDiagnosticKeyProcessor[0]
            );
        }

        private void Write(TekReleaseWorkflowStateEntity[] workflows)
        {
            var db = _workflowDbProvider.CreateNew();
            db.KeyReleaseWorkflowStates.AddRange(workflows);
            db.TemporaryExposureKeys.AddRange(workflows.SelectMany(x => x.Teks));
            db.SaveChanges();
            Assert.Equal(workflows.Length, db.KeyReleaseWorkflowStates.Count());
            Assert.Equal(workflows.Sum(x => x.Teks.Count), db.TemporaryExposureKeys.Count());
        }

        private void GenerateWorkflowTeks(int wfCount, int tekPerWfCount)
        {
            Write(Enumerable.Range(0, wfCount).Select(x => GenWorkflow(x, GenTeks(tekPerWfCount))).ToArray());
        }

        private TekEntity[] GenTeks(int tekPerWfCount)
        {
            var t = _dateTimeProvider.Object.Snapshot;
            return Enumerable.Range(0, tekPerWfCount).Select(x =>
                new TekEntity
                {
                    RollingStartNumber = t.AddDays(-x).ToUniversalTime().Date.ToRollingStartNumber(),
                    RollingPeriod = 2, //Corrected by a processor.
                    KeyData = new byte[UniversalConstants.DailyKeyDataByteCount],
                    PublishAfter = t.AddHours(2)
                }
            ).ToArray();
        }

        private TekReleaseWorkflowStateEntity GenWorkflow(int key, params TekEntity[] items)
        {
            var now = _dateTimeProvider.Object.Snapshot;
            var b = BitConverter.GetBytes(key);

            return new TekReleaseWorkflowStateEntity
            {
                BucketId = b,
                ConfirmationKey = b,
                AuthorisedByCaregiver = now,
                Created = now,
                ValidUntil = now.AddDays(1),
                StartDateOfTekInclusion = now.AddDays(-1).Date, //Yesterday
                IsSymptomatic = InfectiousPeriodType.Symptomatic,
                Teks = items
            };
        }

        public void Dispose()
        {
            _workflowDbProvider.Dispose();
            _dkSourceDbProvider.Dispose();
            _lf.Dispose();
        }

        #endregion

        [InlineData(0, 0, 120, 0)] //Null case
        [InlineData(1, 10, 119, 0)] //Just before
        [InlineData(1, 10, 120, 10)] //Exactly
        [InlineData(1, 10, 121, 10)] //After
        [Theory]
        [ExclusivelyUses(nameof(WfToDkSnapshotTests))]
        public async Task PublishAfter(int wfCount, int tekPerWfCount, int addMins, int resultCount)
        {

            var t = new DateTime(2020, 11, 5, 12, 0, 0, DateTimeKind.Utc);
            _dateTimeProvider.Setup(x => x.Snapshot).Returns(t);
            var tekCount = wfCount * tekPerWfCount;
            GenerateWorkflowTeks(wfCount, tekPerWfCount);

            Assert.Equal(tekCount, _workflowDbProvider.CreateNew().TemporaryExposureKeys.Count(x => x.PublishingState == PublishingState.Unpublished));
            Assert.Equal(0, _dkSourceDbProvider.CreateNew().DiagnosisKeys.Count());

            _dateTimeProvider.Setup(x => x.Snapshot).Returns(t.AddMinutes(addMins));
            var c = Create();
            var result = await c.ExecuteAsync();

            Assert.Equal(resultCount, result.TekReadCount);
            Assert.Equal(tekCount - resultCount, _workflowDbProvider.CreateNew().TemporaryExposureKeys.Count(x => x.PublishingState == PublishingState.Unpublished));
            Assert.Equal(resultCount, _dkSourceDbProvider.CreateNew().DiagnosisKeys.Count());
        }

        [InlineData(0, 0, 0)] //Null case
        [InlineData(1, 1, 1)]
        [InlineData(1, 10, 10)]
        [Theory]
        [ExclusivelyUses(nameof(WfToDkSnapshotTests))]
        public async Task SecondRunShouldChangeNothing(int wfCount, int tekPerWfCount, int resultCount)
        {
            _dateTimeProvider.Setup(x => x.Snapshot).Returns(new DateTime(2020, 11, 5, 14, 00, 0, DateTimeKind.Utc));
            GenerateWorkflowTeks(wfCount, tekPerWfCount);

            //Two hours later
            _dateTimeProvider.Setup(x => x.Snapshot).Returns(new DateTime(2020, 11, 5, 16, 00, 0, DateTimeKind.Utc));
            var tekCount = wfCount * tekPerWfCount;
            Assert.Equal(tekCount, _workflowDbProvider.CreateNew().TemporaryExposureKeys.Count(x => x.PublishingState == PublishingState.Unpublished));
            Assert.Equal(0, _dkSourceDbProvider.CreateNew().DiagnosisKeys.Count());
            Assert.True(_dkSourceDbProvider.CreateNew().DiagnosisKeys.All(x => x.DailyKey.RollingPeriod == UniversalConstants.RollingPeriodRange.Hi)); //Compatible with Apple API

            var result = await Create().ExecuteAsync();
            Assert.Equal(resultCount, result.TekReadCount);
            Assert.Equal(resultCount, _workflowDbProvider.CreateNew().TemporaryExposureKeys.Count(x => x.PublishingState != PublishingState.Unpublished));
            Assert.Equal(resultCount, _dkSourceDbProvider.CreateNew().DiagnosisKeys.Count());

            //Second run
            result = await Create().ExecuteAsync();

            //No changes
            Assert.Equal(0, result.TekReadCount);
            Assert.Equal(resultCount, _workflowDbProvider.CreateNew().TemporaryExposureKeys.Count(x => x.PublishingState != PublishingState.Unpublished));
            Assert.Equal(resultCount, _dkSourceDbProvider.CreateNew().DiagnosisKeys.Count());
        }
    }
}
