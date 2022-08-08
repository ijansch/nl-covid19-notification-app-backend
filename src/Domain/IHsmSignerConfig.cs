// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;

public interface IHsmSignerConfig
{
    public string NlJwt { get; }
    public string GaenJwt { get; }
    public string NlPem { get; }
    public string GaenPem { get; }
}
