// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Content;

namespace CdnDataPurge
{
    //TODO Dead code walking?

    public class PurgeFunction
    {
        private readonly CdnContentPurgeCommand _Command;

        public PurgeFunction(CdnContentPurgeCommand command)
        {
            _Command = command;
        }

        [FunctionName("PurgeFunction")]
        public async Task Run([TimerTrigger("%PurgeJobTriggerTime%")] TimerInfo _, ILogger<PurgeFunction> log)
        {
            log.LogInformation($"CdnDataPurge triggered.");
            await _Command.Execute();
        }
    }
}
