// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands
{
    public class PublishContentLoggingExtensions
    {
        private const string Name = "PublishContent";
        private const int Base = LoggingCodex.PublishContent;

        private const int StartWriting = Base + 1;
        private const int FinishedWriting = Base + 2;

        private readonly ILogger _logger;

        public PublishContentLoggingExtensions(ILogger<PublishContentLoggingExtensions> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void WriteStartWriting(ContentTypes contentType)
        {
            _logger.LogDebug("[{name}/{id}] Writing {ContentType} to database.",
                Name, StartWriting,
                contentType);
        }

        public void WriteFinishedWriting(ContentTypes contentType)
        {
            _logger.LogDebug("[{name}/{id}] Done writing {ContentType} to database.",
                Name, FinishedWriting,
                contentType);
        }
    }
}
