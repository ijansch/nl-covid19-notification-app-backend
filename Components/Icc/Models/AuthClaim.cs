// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Icc.Models
{
    public class AuthClaim
    {
        public string Type;
        public string Value;

        public AuthClaim(string type, string value)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}