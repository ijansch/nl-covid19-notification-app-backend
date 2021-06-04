// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core.AspNet;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Content.WebApi
{
    public class CorsOptions : ICorsOptions
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ContentApiConfig _config;

        public CorsOptions(IWebHostEnvironment environment, ContentApiConfig config)
        {
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Build(CorsPolicyBuilder options)
        {
            options.WithMethods("GET").WithOrigins(GetEnvOrigins());
        }

        private string[] GetEnvOrigins()
        {
            // Swaggering via these urls
            if (_environment.IsDevelopment())
            {
                var originBuilder = new OriginBuilder(_config.Url);
                return new[]
                {
                    originBuilder.GetOrigin()
                };
            }

            return new[] { "" }; // Denies Swagger on acceptatie and productie 
        }
    }
}