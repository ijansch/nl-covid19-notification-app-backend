﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Publishing.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Uploader.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Uploader.EntityFramework;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Commands.Publishing
{
    public class IksJobContentWriter
    {
        private readonly Func<IksOutDbContext> _contentDbContext;
        private readonly Func<IksPublishingJobDbContext> _publishingDbContext;
        private readonly ILogger<IksJobContentWriter> _logger;

        public IksJobContentWriter(Func<IksOutDbContext> contentDbContext, Func<IksPublishingJobDbContext> publishingDbContext, ILogger<IksJobContentWriter> logger)
        {
            _contentDbContext = contentDbContext ?? throw new ArgumentNullException(nameof(contentDbContext));
            _publishingDbContext = publishingDbContext ?? throw new ArgumentNullException(nameof(publishingDbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task ExecuteAsyc()
        {
            await using var pdbc = _publishingDbContext();
            await using (pdbc.BeginTransaction()) //Read consistency
            {
                var move = pdbc.Output.Select(
                    x => new IksOutEntity
                    {
                        Created = x.Created,
                        ValidFor = x.Created,
                        Content = x.Content,
                        Qualifier = x.CreatingJobQualifier
                        //TODO batch id? use qualifier
                    }).ToArray();

                await using var cdbc = _contentDbContext();
                await using (cdbc.BeginTransaction())
                {
                    cdbc.Iks.AddRange(move);
                    cdbc.SaveAndCommit();
                }

                _logger.LogInformation("Published EKSs - Count:{Count}.", move.Length);
            }
        }
    }
}