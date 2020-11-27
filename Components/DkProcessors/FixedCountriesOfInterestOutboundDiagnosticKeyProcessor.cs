﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Entities;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.DkProcessors
{
    public class FixedCountriesOfInterestOutboundDiagnosticKeyProcessor : IDiagnosticKeyProcessor
    {
        private readonly string[] _Value;

        public FixedCountriesOfInterestOutboundDiagnosticKeyProcessor(IOutboundFixedCountriesOfInterestSetting settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            _Value = settings.CountriesOfInterest;
        }

        public DkProcessingItem? Execute(DkProcessingItem? value)
        {
            if (value.DiagnosisKey.Origin != TekOrigin.Local)
                throw new InvalidOperationException("This is a local processor for local DKs. You wouldn't like it here...");

            value.DiagnosisKey.Efgs.CountriesOfInterest = string.Join(",", _Value);
            return value;
        }
    }
}
