﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Entities;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ExposureKeySetsEngine.Interop
{
    public interface ITransmissionRiskLevelCalculationMk2
    {
        Range<int> SignificantDayRange { get; }
        TransmissionRiskLevel Calculate(int daysSinceOnsetSymptoms);
    }
}