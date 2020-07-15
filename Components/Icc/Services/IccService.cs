// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.EfDatabase.Contexts;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Icc.Exceptions;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Icc.Models;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ICC.Models;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.ICC.Services;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Services;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Workflow.RegisterSecret;
using Microsoft.Extensions.Logging;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Components.Icc.Services
{
    public class IccService : IIccService
    {
        private readonly IccBackendContentDbContext _DbContext;
        private readonly IConfiguration _Configuration;
        private readonly IUtcDateTimeProvider _DateTimeProvider;
        private readonly IRandomNumberGenerator _RandomGenerator;
        private readonly ILogger _Logger;

        public IccService(IccBackendContentDbContext dbContext, IConfiguration configuration, IUtcDateTimeProvider dateTimeProvider, IRandomNumberGenerator randomGenerator, ILogger<IccService> logger)
        {
            _DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _DateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _RandomGenerator = randomGenerator ?? throw new ArgumentNullException(nameof(randomGenerator));
            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get InfectionConfirmationCodeEntity by raw icc string
        /// </summary>
        /// <param name="icc"></param>
        /// <returns></returns>
        /// <exception cref="IccNotFoundException"></exception>
        public async Task<InfectionConfirmationCodeEntity> Get(string icc)
        {
            //TODO if(string.IsNullOrWhiteSpace(icc) ???

            var infectionConfirmationCodeEntity = await _DbContext.InfectionConfirmationCodes.FindAsync(icc);
            if (infectionConfirmationCodeEntity == null) throw new IccNotFoundException();
            return infectionConfirmationCodeEntity;
        }

        /// <summary>
        /// Checks if Icc exists and is valid
        /// </summary>
        /// <param name="iccCodeString"></param>
        /// <returns>Icc if valid else null</returns>
        public async Task<InfectionConfirmationCodeEntity> Validate(string iccCodeString)
        {
            //TODO if(string.IsNullOrWhiteSpace(iccCodeString) ???

            var infectionConfirmationCodeEntity = await Get(iccCodeString);
            if (infectionConfirmationCodeEntity != null && infectionConfirmationCodeEntity.IsValid())
                return infectionConfirmationCodeEntity;
            return null;
        }

        /// <summary>
        /// Generate Icc with configuration length and A-Z, 0-9 characters
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="save"></param>
        /// <param name="batchId"></param>
        /// <returns></returns>
        public async Task<InfectionConfirmationCodeEntity> GenerateIcc(string userId, string batchId)
        {
            //TODO if(string.IsNullOrWhiteSpace(userId) ???
            //TODO if(string.IsNullOrWhiteSpace(batchId) ???

            var length = Convert.ToInt32(_Configuration.GetSection("IccConfig:Code:Length").Value); //TODO default if not present?
            var generatedIcc = _RandomGenerator.GenerateToken(length);

            var icc = new InfectionConfirmationCodeEntity
            {
                Code = generatedIcc, GeneratedBy = userId, Created = _DateTimeProvider.Now(), BatchId = batchId
            };


            await _DbContext.InfectionConfirmationCodes.AddAsync(icc);
            return icc;
        }

        /// <summary>
        /// Generate Icc batch with size [count]
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public async Task<IccBatch> GenerateBatch(string userId, int count = 20) //TODO configuration for default
        {
            //TODO if(string.IsNullOrWhiteSpace(userId) ???
            //TODO if(count <= 0) ???

            string batchId = _RandomGenerator.GenerateToken();
            IccBatch batch = new IccBatch(batchId);

            for (var i = 0; i < count; i++)
            {
                batch.AddIcc(await GenerateIcc(userId, batchId));
            }
            
            return batch;
        }

        public async Task<List<InfectionConfirmationCodeEntity>> GetBatchItems(string batchId)
        {
            //TODO if(string.IsNullOrWhiteSpace(batchId) ???

            var batch = await _DbContext.InfectionConfirmationCodes.Where(_ => _.BatchId == batchId).ToListAsync();

            return batch ?? new List<InfectionConfirmationCodeEntity>();
        }

        public async Task<InfectionConfirmationCodeEntity> RedeemIcc(string icc)
        {
            //TODO if(string.IsNullOrWhiteSpace(icc) ???

            var infectionConfirmationCodeEntity = await Get(icc);
            infectionConfirmationCodeEntity.Used = _DateTimeProvider.Now();

            return infectionConfirmationCodeEntity;
        }

        public async Task<bool> RevokeBatch(RevokeBatchInput revokeBatchInput)
        {
            //TODO if (revokeBatchInput == null) throw new ArgumentNullException(nameof(revokeBatchInput)); ???

            List<InfectionConfirmationCodeEntity> iccList =
                await _DbContext.InfectionConfirmationCodes.Where(i => i.BatchId == revokeBatchInput.BatchId)
                    .ToListAsync();

            //TODO LINQ
            if (iccList.Count > 0)
            {
                foreach (InfectionConfirmationCodeEntity infectionConfirmationCodeEntity in iccList)
                {
                    infectionConfirmationCodeEntity.Revoked =
                        revokeBatchInput.RevokeDateTime ?? _DateTimeProvider.Now(); //TODO time moves... snapshot a value before all the comparisons
                }

                return true;
            }

            return false;
        }
    }
}