﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using System.Threading.Tasks;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.DevOps;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Configuration;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase
{
    public class ProvisionDatabasesCommand
    {
        private readonly WorkflowDatabaseCreateCommand _Workflow;
        private readonly ContentDatabaseCreateCommand _Content;
        private readonly PublishingJobDatabaseCreateCommand _Job;
        private readonly DbProvisionLoggingExtensions _Logger;

        public ProvisionDatabasesCommand(WorkflowDatabaseCreateCommand workflow, ContentDatabaseCreateCommand content, PublishingJobDatabaseCreateCommand job, DbProvisionLoggingExtensions logger)
        {
            _Workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
            _Content = content ?? throw new ArgumentNullException(nameof(content));
            _Job = job ?? throw new ArgumentNullException(nameof(job));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync(string[] args)
        {
            var nuke = !args.Contains("nonuke");

            _Logger.WriteStart();

            _Logger.WriteWorkFlowDb();
            await _Workflow.ExecuteAsync(nuke);

            _Logger.WriteContentDb();
            await _Content.ExecuteAsync(nuke);

            _Logger.WriteJobDb();
            await _Job.ExecuteAsync(nuke);

            _Logger.WriteFinished();
        }
    }
}
