// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.DailyCleanup.Commands.Workflow
{
    public class RemoveExpiredWorkflowsResult : CommandResult
    {
        public WorkflowStats Before { get; } = new WorkflowStats();
        public bool DeletionsOn { get; set; }
        public int GivenMercy { get; set; }
        public WorkflowStats After { get; } = new WorkflowStats();
    }
}
