// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DiagnosisKeys.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Commands.Tests.Interop.TestData
{
    public class DiagnosisKeyTestDataGenerator
    {
        private readonly DiagnosisKeysDbContext _diagnosisKeysDbContext;
        public DateTime BaseDate { get; set; } = DateTime.UtcNow.Date;

        public DiagnosisKeyTestDataGenerator(DiagnosisKeysDbContext diagnosisKeysDbContext)
        {
            _diagnosisKeysDbContext = diagnosisKeysDbContext;
        }

        public long[] Write(DiagnosisKeyEntity[] items)
        {
            _diagnosisKeysDbContext.DiagnosisKeys.AddRange(items);
            _diagnosisKeysDbContext.SaveChanges();
            return items.Select(x => x.Id).ToArray();
        }

        public DiagnosisKeyEntity[] AlreadyPublished(DiagnosisKeyEntity[] items)
        {
            foreach (var i in items)
            {
                i.Origin = TekOrigin.Local;
                i.PublishedToEfgs = true;
                i.PublishedLocally = true;
            }

            return items;
        }

        public DiagnosisKeyEntity[] LocalDksForLocalPeople(DiagnosisKeyEntity[] items)
        {
            foreach (var i in items)
            {
                i.Origin = TekOrigin.Local;
                i.PublishedLocally = true;
                i.Efgs = new EfgsTekInfo
                {
                    DaysSinceSymptomsOnset = 4,
                    CountriesOfInterest = "",
                    ReportType = ReportType.ConfirmedTest,
                    //TransmissionRiskLevel = TransmissionRiskLevel.High,
                };
            }

            return items;
        }

        public DiagnosisKeyEntity[] NotLocal(DiagnosisKeyEntity[] items)
        {
            foreach (var i in items)
            {
                i.Origin = TekOrigin.Efgs;
                i.PublishedToEfgs = true;
                i.Local = new LocalTekInfo
                {
                    TransmissionRiskLevel = TransmissionRiskLevel.Medium,
                    DaysSinceSymptomsOnset = 3
                };
            }

            return items;
        }

        public DiagnosisKeyEntity[] GenerateDks(int count)
        {
            return Enumerable.Range(0, count).Select(x =>
                new DiagnosisKeyEntity
                {
                    DailyKey = new DailyKey
                    {
                        RollingStartNumber = BaseDate.AddDays(-x % 14).ToUniversalTime().Date.ToRollingStartNumber(),
                        RollingPeriod = 144, //Already corrected in DKs
                        KeyData = new byte[UniversalConstants.DailyKeyDataByteCount],
                    },
                    Local = new LocalTekInfo
                    {
                        DaysSinceSymptomsOnset = -2
                    }
                }).ToArray();
        }
    }
}
