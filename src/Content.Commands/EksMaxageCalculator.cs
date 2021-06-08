// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands
{
    public class EksMaxageCalculator
    {
        private readonly IUtcDateTimeProvider _dateTimeProvider;
        private readonly IEksConfig _eksConfig;
        private readonly ITaskSchedulingConfig _taskSchedulingConfig;

        public EksMaxageCalculator(IUtcDateTimeProvider dateTimeProvider, IEksConfig eksConfig, ITaskSchedulingConfig taskSchedulingConfig)
        {
            _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
            _eksConfig = eksConfig ?? throw new ArgumentNullException(nameof(eksConfig));
            _taskSchedulingConfig = taskSchedulingConfig ?? throw new ArgumentNullException(nameof(taskSchedulingConfig));
        }

        public int Execute(DateTime created)
        {
            var ttl = TimeSpan.FromDays(_eksConfig.LifetimeDays) + TimeSpan.FromHours(_taskSchedulingConfig.DailyCleanupHoursAfterMidnight);
            var life = _dateTimeProvider.Snapshot - created;
            var remaining = (int)Math.Floor((ttl - life).TotalSeconds);
            return Math.Max(remaining, 60); //Give it another minute in case the Daily Cleanup is late.
        }
    }
}
