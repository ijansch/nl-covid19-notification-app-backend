﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using Eu.Interop;
using Google.Protobuf;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.DkProcessors;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.IksInbound
{
    /// <summary>
    /// Read and process single inbound IKS
    /// </summary>
    public class IksImportCommand
    {
        private readonly DkSourceDbContext _DkSourceDbContext;
        private readonly IDiagnosticKeyProcessor[] _ImportProcessors;

        public IksImportCommand(DkSourceDbContext dkSourceDbContext, IDiagnosticKeyProcessor[] importProcessors)
        {
            _DkSourceDbContext = dkSourceDbContext ?? throw new ArgumentNullException(nameof(dkSourceDbContext));
            _ImportProcessors = importProcessors ?? throw new ArgumentNullException(nameof(importProcessors));
        }

        public async Task Execute(IksInEntity entity)
        {
            var parser = new MessageParser<DiagnosisKeyBatch>(() => new DiagnosisKeyBatch());
            var batch = parser.ParseFrom(entity.Content);

            //TODO basic validity of contents
            //TODO filter out or bin the iks?

            var items = batch.Keys.Select(x => (DkProcessingItem?)new DkProcessingItem
            {
                DiagnosisKey = new DiagnosisKeyEntity 
                { 
                    DailyKey = new DailyKey(x.KeyData.ToByteArray(), (int)x.RollingStartIntervalNumber, (int)x.RollingPeriod),
                    Origin = TekOrigin.Efgs,
                    Efgs = new EfgsTekInfo 
                    {
                        CountriesOfInterest = string.Join(",", x.VisitedCountries),
                        DaysSinceSymptomsOnset = x.DaysSinceOnsetOfSymptoms,
                        TransmissionRiskLevel =  (TransmissionRiskLevel)x.TransmissionRiskLevel,
                        ReportType = (ReportType)x.ReportType,
                        BatchTag = entity.BatchTag
                    }, 
                    PublishedLocally = false,
                    PublishedToEfgs = true, //Always for Origin = TekOrigin.Efgs
                }, 
                Metadata = new Dictionary<string, object> 
                { 
                    {"Countries", x.VisitedCountries.ToArray() },
                    {"TRL", x.TransmissionRiskLevel },
                    {"ReportType", x.ReportType }
                }
            }).ToArray();

            items = _ImportProcessors.Execute(items);

            var result = items.Where(_ => _ != null).Select(_ => _!.DiagnosisKey).ToList();
            await _DkSourceDbContext.BulkInsertAsync2(result, new SubsetBulkArgs());
        }
    }
}