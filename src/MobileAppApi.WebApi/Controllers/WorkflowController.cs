// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core.AspNet;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Commands.DecoyKeys;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Commands.RegisterSecret;
using NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Commands.SendTeks;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.MobileAppApi.Controllers
{
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        private readonly PostKeysLoggingExtensions _loggerPostKeys;
        private readonly DecoyKeysLoggingExtensions _loggerDecoyKeys;

        public WorkflowController(PostKeysLoggingExtensions loggerPostKeys, DecoyKeysLoggingExtensions loggerDecoyKeys)
        {
            _loggerPostKeys = loggerPostKeys ?? throw new ArgumentNullException(nameof(loggerPostKeys));
            _loggerDecoyKeys = loggerDecoyKeys ?? throw new ArgumentNullException(nameof(loggerDecoyKeys));
        }

        [ResponsePaddingFilterFactory]
        [SuppressErrorFactory]
        [DecoyTimeAggregatorAttributeFactory]
        [HttpPost]
        [Route("/v1/postkeys")]
        public async Task<IActionResult> PostWorkflow([FromQuery] byte[] sig, [FromServices] HttpPostReleaseTeksCommand2 command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            _loggerPostKeys.WriteStartPostKeys();
            return await command.ExecuteAsync(sig, Request);
        }

        [ResponsePaddingFilterFactory]
        [SuppressErrorFactory]
        [DecoyTimeAggregatorAttributeFactory]
        [HttpPost]
        [Route("/v2/register")]
        public async Task<IActionResult> PostSecretV2([FromServices] HttpPostRegisterSecretV2 command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            return await command.ExecuteAsync();
        }

        [ResponsePaddingFilterFactory]
        [SuppressErrorFactory]
        [DecoyTimeGeneratorAttributeFactory]
        [HttpPost]
        [Route("/v1/stopkeys")]
        public IActionResult StopKeys()
        {
            _loggerDecoyKeys.WriteStartDecoy();
            return new OkResult();
        }

        [HttpGet]
        [Route("/")]
        public IActionResult AssemblyDump([FromServices] IWebHostEnvironment env) => new DumpAssembliesToPlainText().Execute(env.IsDevelopment());
    }
}
