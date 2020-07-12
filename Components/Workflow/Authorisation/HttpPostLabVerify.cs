﻿// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Threading.Tasks;
using JWT.Exceptions;
using Microsoft.AspNetCore.Mvc;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflow.Authorisation
{
    public class HttpPostLabVerify
    {
        private readonly LabVerifyChecker _LabVerifyChecker;
        private readonly WorkflowDbContext _DbContextProvider;
        private readonly PollTokenGenerator _PollTokenGenerator;

        public HttpPostLabVerify(LabVerifyChecker labVerifyChecker, WorkflowDbContext dbContextProvider,
            PollTokenGenerator pollTokenGenerator)
        {
            _LabVerifyChecker = labVerifyChecker;
            _DbContextProvider = dbContextProvider;
            _PollTokenGenerator = pollTokenGenerator;
        }

        public async Task<IActionResult> Execute(LabVerifyArgs args)
        {
            try
            {
                var keyReleaseWorkflowState = await _LabVerifyChecker.Execute(args);
                // _DbContextProvider.SaveAndCommit();
                return new OkObjectResult(new AuthorisationResponse {Valid = true, PollToken = keyReleaseWorkflowState.PollToken});
            }
            catch (KeysUploadedNotValidException e)
            {
                return new OkObjectResult(new AuthorisationResponse
                    {Valid = false, PollToken = e.KeyReleaseWorkflowState.PollToken});
            }
            catch (TokenExpiredException e)
            {
                return new OkObjectResult(new AuthorisationResponse {Valid = false});
            }
            catch (LabFlowNotFoundException e)
            {
                return new OkObjectResult(new AuthorisationResponse {Valid = false});
            }
        }
    }
}