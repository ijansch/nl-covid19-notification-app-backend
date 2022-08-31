// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System.Collections.Generic;
using System.Security.Claims;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Icc.Commands.Models;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Icc.Commands.Authorisation.Code
{
    public interface IAuthCodeService
    {
        string GenerateAuthCode(ClaimsPrincipal claimsPrincipal);

        List<AuthClaim> GetClaimsByAuthCode(string authCode);

        void RevokeAuthCode(string authCode);
    }
}
