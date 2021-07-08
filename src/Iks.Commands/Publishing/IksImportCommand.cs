// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Iks.Protobuf;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.Processors;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Downloader.Entities;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Commands.Publishing
{
    /// <summary>
    /// Read and process single inbound IKS
    /// </summary>
    public class IksImportCommand
    {
        private readonly DkSourceDbContext _dkSourceDbContext;
        private readonly IDiagnosticKeyProcessor[] _importProcessors;
        private readonly Iso3166RegionCodeValidator _countryValidator = new Iso3166RegionCodeValidator();
        private readonly ITekValidatorConfig _tekValidatorConfig;
        private readonly IUtcDateTimeProvider _dateTimeProvider;
        private readonly ILogger<IksImportCommand> _logger;

        public IksImportCommand(DkSourceDbContext dkSourceDbContext, IDiagnosticKeyProcessor[] importProcessors, ITekValidatorConfig tekValidatorConfig, IUtcDateTimeProvider dateTimeProvider, ILogger<IksImportCommand> logger)
        {
            _dkSourceDbContext = dkSourceDbContext ?? throw new ArgumentNullException(nameof(dkSourceDbContext));
            _importProcessors = importProcessors ?? throw new ArgumentNullException(nameof(importProcessors));
            _tekValidatorConfig = tekValidatorConfig ?? throw new ArgumentNullException(nameof(tekValidatorConfig));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Execute(IksInEntity entity)
        {

            if (!TryParse(entity.Content, out var batch))
            {
                entity.Error = true;
                return;
            }

            if (batch?.Keys == null || batch.Keys.Count == 0)
            {
                //TODO log.
                entity.Error = true;
                return;
            }

            var items = batch.Keys
                .Where(Valid)
                .Select(x => (DkProcessingItem)new DkProcessingItem
                {
                    DiagnosisKey = new DiagnosisKeyEntity
                    {
                        DailyKey = new DailyKey(x.KeyData.ToByteArray(), (int)x.RollingStartIntervalNumber, (int)x.RollingPeriod),
                        Origin = TekOrigin.Efgs,
                        PublishedToEfgs = true, //Do not send back to EFGS
                        Efgs = new EfgsTekInfo
                        {
                            CountryOfOrigin = x.Origin,
                            CountriesOfInterest = string.Join(",", x.VisitedCountries),
                            DaysSinceSymptomsOnset = x.DaysSinceOnsetOfSymptoms,
                            ReportType = (ReportType)x.ReportType,
                        },
                        Local = new LocalTekInfo
                        {
                            //Filled in by filters
                        }
                    },
                    Metadata = new Dictionary<string, object>
                {
                    {"Countries", x.VisitedCountries.ToArray() },
                    {"TRL", x.TransmissionRiskLevel },
                    {"ReportType", x.ReportType }
                }
                }).ToArray();

            items = _importProcessors.Execute(items);
            var result = items.Select(x => x.DiagnosisKey).ToList();
            await _dkSourceDbContext.BulkInsertAsync2(result, new SubsetBulkArgs());
        }

        private bool TryParse(byte[] buffer, out DiagnosisKeyBatch result)
        {
            result = null;
            try
            {
                var parser = new MessageParser<DiagnosisKeyBatch>(() => new DiagnosisKeyBatch());
                result = parser.ParseFrom(buffer);
                return true;
            }
            catch (InvalidProtocolBufferException e)
            {
                _logger.LogError("Error reading IKS protobuf.", e.ToString());
                return false;
            }
        }

        private bool Valid(DiagnosisKey value)
        {
            if (value == null)
            {
                return false;
            }

            if (!_countryValidator.IsValid(value.Origin))
            {
                return false;
            }

            var rollingStartMin = _tekValidatorConfig.RollingStartNumberMin;
            var rollingStartToday = _dateTimeProvider.Snapshot.Date.ToRollingStartNumber();

            if (!(rollingStartMin <= value.RollingStartIntervalNumber && value.RollingStartIntervalNumber <= rollingStartToday))
            {
                return false;
            }

            if (!(UniversalConstants.RollingPeriodRange.Lo <= value.RollingPeriod && value.RollingPeriod <= UniversalConstants.RollingPeriodRange.Hi))
            {
                return false;
            }

            if (value.KeyData == null)
            {
                return false;
            }

            if (value.KeyData.Length != UniversalConstants.DailyKeyDataByteCount)
            {
                return false;
            }

            return true;
        }
    }
}
