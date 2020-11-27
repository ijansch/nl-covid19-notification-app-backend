﻿using System;
using System.Linq;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ExposureKeySetsEngine.Interop;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.DkProcessors
{
    public class NlTrlFromDecodedDosDiagnosticKeyProcessor : IDiagnosticKeyProcessor
    {
        private readonly ITransmissionRiskLevelCalculationMk2 _TrlCalculation;

        public NlTrlFromDecodedDosDiagnosticKeyProcessor(ITransmissionRiskLevelCalculationMk2 trlCalculation)
        {
            _TrlCalculation = trlCalculation ?? throw new ArgumentNullException(nameof(trlCalculation));
        }

        public DkProcessingItem? Execute(DkProcessingItem? value)
        {
            if (value == null) return value;

            if (!value.Metadata.TryGetValue(DosDecodingDiagnosticKeyProcessor.DecodedDsosMetadataKey, out var decodedDos))
            {
                //TODO log not present... and fill up the log files...
                return null;
            }

            //decodedDos map to TRL...
            value.DiagnosisKey.Local.TransmissionRiskLevel = GetTrl((DsosDecodeResult)decodedDos);

            return value;
        }

        //Take the highest TRL that can be inferred from the DsosDecodeResult.
        private TransmissionRiskLevel GetTrl(DsosDecodeResult value)
        {
            if (value.SymptomObservation == SymptomObservation.Symptomatic)
            {
                var symptomaticValue = value.AsSymptomatic();

                if (symptomaticValue.SymptomsOnsetDatePrecision == SymptomsOnsetDatePrecision.Exact)
                    return _TrlCalculation.Calculate(symptomaticValue.DaysSinceOnsetOfSymptoms);

                if (symptomaticValue.SymptomsOnsetDatePrecision == SymptomsOnsetDatePrecision.Range)
                    return GetTrl(symptomaticValue.DaysSinceLastSymptoms);
            }

            //Default
            return _TrlCalculation.Calculate(value.DaysSinceSubmission);
        }

        private TransmissionRiskLevel GetTrl(Range<int> range)
        {
            var result = Enumerable.Range(range.Lo, range.Hi)
                .Select(x => _TrlCalculation.Calculate(x))
                .ToArray();

            if (result.All(x => x == TransmissionRiskLevel.None))
                return TransmissionRiskLevel.None;

            return result.Max();
        }
    }
}