// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;
using NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Commands;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.EksEngine.Tests.ExposureKeySets
{
    public abstract class ExposureKeySetCleanerTests
    {
        private readonly ContentDbContext _contentDbContext;

        protected ExposureKeySetCleanerTests(DbContextOptions<ContentDbContext> contentDbContextOptions)
        {
            _contentDbContext = new ContentDbContext(contentDbContextOptions ?? throw new ArgumentNullException(nameof(contentDbContextOptions)));
            _contentDbContext.Database.EnsureCreated();
        }

        private class FakeDtp : IUtcDateTimeProvider
        {
            public DateTime Now() => throw new NotImplementedException();
            public DateTime TakeSnapshot() => throw new NotImplementedException();
            public DateTime Snapshot { get; set; }
        }
        private class FakeEksConfig : IEksConfig
        {
            public int LifetimeDays => 14;
            public int TekCountMax => throw new NotImplementedException();
            public int TekCountMin => throw new NotImplementedException();
            public int PageSize => throw new NotImplementedException();
            public bool CleanupDeletesData { get; set; }
        }
        private void Add(int id)
        {
            _contentDbContext.Content.Add(new ContentEntity
            {
                Content = new byte[0],
                PublishingId = id.ToString(),
                ContentTypeName = "meh",
                Created = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) - TimeSpan.FromDays(id),
                Release = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) - TimeSpan.FromDays(id),
                Type = ContentTypes.ExposureKeySet
            });

            _contentDbContext.SaveChanges();
        }

        [Fact]
        public void Cleaner()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = new ExpiredEksLoggingExtensions(lf.CreateLogger<ExpiredEksLoggingExtensions>());
            var command = new RemoveExpiredEksCommand(_contentDbContext, new FakeEksConfig(), new StandardUtcDateTimeProvider(), expEksLogger);

            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(0, result.Found);
            Assert.Equal(0, result.Zombies);
            Assert.Equal(0, result.GivenMercy);
            Assert.Equal(0, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }

        [Fact]
        public void NoKill()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = new ExpiredEksLoggingExtensions(lf.CreateLogger<ExpiredEksLoggingExtensions>());
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var command = new RemoveExpiredEksCommand(_contentDbContext, new FakeEksConfig(), fakeDtp, expEksLogger);

            Add(15);
 
            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(1, result.Found);
            Assert.Equal(1, result.Zombies);
            Assert.Equal(0, result.GivenMercy);
            Assert.Equal(1, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }


        [Fact]
        public void Kill()
        {
            // Arrange
            _contentDbContext.Truncate<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = new ExpiredEksLoggingExtensions(lf.CreateLogger<ExpiredEksLoggingExtensions>());
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var fakeEksConfig = new FakeEksConfig() { CleanupDeletesData = true };
            var command = new RemoveExpiredEksCommand(_contentDbContext, fakeEksConfig, fakeDtp, expEksLogger);

            Add(15);

            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(1, result.Found);
            Assert.Equal(1, result.Zombies);
            Assert.Equal(1, result.GivenMercy);
            Assert.Equal(0, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }


        [Fact]
        public void MoreRealistic()
        {
            // Arrange
            _contentDbContext.BulkDelete(_contentDbContext.Content.ToList());

            var lf = new LoggerFactory();
            var expEksLogger = new ExpiredEksLoggingExtensions(lf.CreateLogger<ExpiredEksLoggingExtensions>());
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var fakeEksConfig = new FakeEksConfig() { CleanupDeletesData = true };
            var command = new RemoveExpiredEksCommand(_contentDbContext, fakeEksConfig, fakeDtp, expEksLogger);

            for (var i = 0; i < 20; i++)
            {
                Add(i);
            }

            // Act
            var result = command.Execute();

            // Assert
            Assert.Equal(20, result.Found);
            Assert.Equal(5, result.Zombies);
            Assert.Equal(5, result.GivenMercy);
            Assert.Equal(15, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }
    }
}
