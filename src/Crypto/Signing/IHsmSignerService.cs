// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System.Threading.Tasks;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Crypto.Signing;

public interface IHsmSignerService
{
    Task<byte[]> GetCmsSignatureAsync(byte[] content);
    Task<byte[]> GetGaenSignatureAsync(byte[] content);
}
