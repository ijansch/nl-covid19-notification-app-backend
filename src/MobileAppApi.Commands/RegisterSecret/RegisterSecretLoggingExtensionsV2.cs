// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Commands.RegisterSecret
{
    public class RegisterSecretLoggingExtensionsV2
    {
        private const string Name = "RegisterV2";
        private const int Base = LoggingCodex.RegisterV2;

        private const int Start = Base;
        private const int Finished = Base + 99;

        private const int Writing = Base + 1;
        private const int Committed = Base + 2;
        private const int DuplicatesFound = Base + 3;

        private const int MaximumCreateAttemptsReached = Base + 4;
        private const int Failed = Base + 5;

        private readonly ILogger _logger;

        public RegisterSecretLoggingExtensionsV2(ILogger<RegisterSecretLoggingExtensionsV2> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void WriteStartSecret()
        {
            _logger.LogInformation("[{name}/{id}] POST register triggered.",
                Name, Start);
        }

        public void WriteFailed(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            _logger.LogCritical(exception, "[{name}/{id}] Failed to create an enrollment response.",
                Name, Failed);
        }

        public void WriteWritingStart()
        {
            _logger.LogDebug("[{name}/{id}] Writing.",
                Name, Writing);
        }

        public void WriteDuplicatesFound(int attemptCount)
        {
            _logger.LogWarning("[{name}/{id}] Duplicates found while creating workflow - Attempt:{AttemptCount}",
                Name, DuplicatesFound,
                attemptCount);
        }

        public void WriteMaximumCreateAttemptsReached()
        {
            _logger.LogCritical("[{name}/{id}] Maximum attempts made at creating workflow.",
                Name, MaximumCreateAttemptsReached);
        }

        public void WriteCommitted()
        {
            _logger.LogDebug("[{name}/{id}] Committed.",
                Name, Committed);
        }

        public void WriteFinished()
        {
            _logger.LogDebug("[{name}/{id}] Finished.",
                Name, Finished);
        }
    }
}
