// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Eks.Publishing.EntityFramework;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands
{
    public class EksJobContentWriter : IEksJobContentWriter
    {
        private readonly ContentDbContext _contentDbContext;
        private readonly EksPublishingJobDbContext _eksPublishingJobDbContext;
        private readonly IPublishingIdService _publishingIdService;
        private readonly EksJobContentWriterLoggingExtensions _logger;

        public EksJobContentWriter(ContentDbContext contentDbContext, EksPublishingJobDbContext eksPublishingJobDbContext, IPublishingIdService publishingIdService, EksJobContentWriterLoggingExtensions logger)
        {
            _contentDbContext = contentDbContext ?? throw new ArgumentNullException(nameof(contentDbContext));
            _eksPublishingJobDbContext = eksPublishingJobDbContext ?? throw new ArgumentNullException(nameof(eksPublishingJobDbContext));
            _publishingIdService = publishingIdService ?? throw new ArgumentNullException(nameof(publishingIdService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync()
        {
            await using (_eksPublishingJobDbContext.BeginTransaction())
            {
                var move = _eksPublishingJobDbContext.EksOutput.AsNoTracking().Select(
                    x => new ContentEntity
                    {
                        Created = x.Release,
                        Release = x.Release,
                        ContentTypeName = MediaTypeNames.Application.Zip,
                        Content = x.Content,
                        Type = ContentTypes.ExposureKeySet,
                        PublishingId = _publishingIdService.Create(x.Content)
                    }).ToArray();

                await using (_contentDbContext.BeginTransaction())
                {
                    await _contentDbContext.Content.AddRangeAsync(move);
                    _contentDbContext.SaveAndCommit();
                }

                _logger.WritePublished(move.Length);
            }
        }
    }
}
