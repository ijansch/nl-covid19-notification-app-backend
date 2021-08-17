// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Threading.Tasks;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Commands.Inbound
{
    public interface IHttpGetIksCommand
    {
        Task<HttpGetIksResult> ExecuteAsync(DateTime date, string batchTag);
    }
}
