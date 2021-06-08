﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using System.Threading.Tasks;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Crypto.Signing;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands.FormatV1
{
    public class EksBuilderV1 : IEksBuilder
    {
        private const string Header = "EK Export v1    ";

        private readonly IGaContentSigner _gaenContentSigner;
        private readonly IContentSigner _nlContentSigner;
        private readonly IUtcDateTimeProvider _dateTimeProvider;
        private readonly IEksContentFormatter _eksContentFormatter;
        private readonly IEksHeaderInfoConfig _config;
        private readonly EksBuilderV1LoggingExtensions _logger;

        public EksBuilderV1(
            IEksHeaderInfoConfig headerInfoConfig,
            IGaContentSigner gaenContentSigner,
            IContentSigner nlContentSigner,
            IUtcDateTimeProvider dateTimeProvider,
            IEksContentFormatter eksContentFormatter,
            EksBuilderV1LoggingExtensions logger
            )
        {
            _gaenContentSigner = gaenContentSigner ?? throw new ArgumentNullException(nameof(gaenContentSigner));
            _nlContentSigner = nlContentSigner ?? throw new ArgumentNullException(nameof(nlContentSigner));
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _eksContentFormatter = eksContentFormatter ?? throw new ArgumentNullException(nameof(eksContentFormatter));
            _config = headerInfoConfig ?? throw new ArgumentNullException(nameof(headerInfoConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<byte[]> BuildAsync(TemporaryExposureKeyArgs[] keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keys.Any(x => x == null))
            {
                throw new ArgumentException("At least one key in null.", nameof(keys));
            }

            var securityInfo = GetGaenSignatureInfo();

            var content = new ExposureKeySetContentArgs
            {
                Header = Header,
                Region = "NL",
                BatchNum = 1,
                BatchSize = 1,
                SignatureInfos = new[] { securityInfo },
                StartTimestamp = _dateTimeProvider.Snapshot.AddDays(-1).ToUnixTimeU64(),
                EndTimestamp = _dateTimeProvider.Snapshot.ToUnixTimeU64(),
                Keys = keys
            };

            var contentBytes = _eksContentFormatter.GetBytes(content);
            var nlSig = _nlContentSigner.GetSignature(contentBytes);
            var gaenSig = _gaenContentSigner.GetSignature(contentBytes);

            _logger.WriteNlSig(nlSig);
            _logger.WriteGaenSig(gaenSig);

            var signatures = new ExposureKeySetSignaturesContentArgs
            {
                Items = new[]
                {
                    new ExposureKeySetSignatureContentArgs
                    {
                        SignatureInfo = securityInfo,
                        Signature = gaenSig,
                        BatchSize = content.BatchSize,
                        BatchNum = content.BatchNum
                    },
                }
            };

            var gaenSigFile = _eksContentFormatter.GetBytes(signatures);
            return await new ZippedContentBuilder().BuildEksAsync(contentBytes, gaenSigFile, nlSig);
        }

        private SignatureInfoArgs GetGaenSignatureInfo()
            => new SignatureInfoArgs
            {
                SignatureAlgorithm = _gaenContentSigner.SignatureOid,
                VerificationKeyId = _config.VerificationKeyId,
                VerificationKeyVersion = _config.VerificationKeyVersion,
                AppBundleId = _config.AppBundleId
            };
    }
}
