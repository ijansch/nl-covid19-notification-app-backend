// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Manifest.Commands
{
    public class ManifestContent : IEquatable<ManifestContent>
    {
        public string[] ExposureKeySets { get; set; }

        public string RiskCalculationParameters { get; set; }

        public string AppConfig { get; set; }
        public string ResourceBundle { get; set; }

        public bool Equals(ManifestContent other)
        {
            return ExposureKeySets.SequenceEqual(other.ExposureKeySets)
                && RiskCalculationParameters == other.RiskCalculationParameters
                && AppConfig == other.AppConfig
                && ResourceBundle == other.ResourceBundle;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((ManifestContent)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ExposureKeySets.GetHashCode();
                hashCode = (hashCode * 397) ^ RiskCalculationParameters.GetHashCode();
                hashCode = (hashCode * 397) ^ AppConfig.GetHashCode();
                hashCode = (hashCode * 397) ^ ResourceBundle.GetHashCode();
                return hashCode;
            }
        }
    }
}
