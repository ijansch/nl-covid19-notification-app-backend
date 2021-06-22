// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Domain
{
    public interface IWorkflowConfig
    {
        public int TimeToLiveMinutes { get; }
        public int PermittedMobileDeviceClockErrorMinutes { get; }

        /// <summary>
        /// TODO this one should be moved to a new config concerning the Daily Cleanup itself or the functionality removed entirely
        /// </summary>
        bool CleanupDeletesData { get; }
    }
}
