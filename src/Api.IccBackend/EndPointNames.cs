﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

namespace NL.Rijksoverheid.ExposureNotification.Api.IccBackend
{
    public static class EndPointNames
    {
        /// <summary>
        /// Use the same ones for CaregiversPortalDataApi
        /// </summary>
        public static class CaregiversPortalApi
        {
            private const string Prefix = "/CaregiversPortalApi/v1";
            public const string LabConfirmation = Prefix + "/labconfirm";
        }
    }
}
