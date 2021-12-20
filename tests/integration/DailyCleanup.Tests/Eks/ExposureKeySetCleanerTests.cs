// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Linq;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Content.Commands.EntityFramework;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.DailyCleanup.Commands.Eks;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Domain;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.DailyCleanup.Tests.Eks
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
                ContentTypeName = "ExposureKeySet",
                Created = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) - TimeSpan.FromDays(id),
                Release = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) - TimeSpan.FromDays(id),
                Type = ContentTypes.ExposureKeySet
            });

            _contentDbContext.Content.Add(new ContentEntity
            {
                Content = new byte[0],
                PublishingId = id.ToString(),
                ContentTypeName = "ExposureKeySetV2",
                Created = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) - TimeSpan.FromDays(id),
                Release = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) - TimeSpan.FromDays(id),
                Type = ContentTypes.ExposureKeySetV2
            });

            _contentDbContext.Content.Add(new ContentEntity
            {
                Content = new byte[0],
                PublishingId = id.ToString(),
                ContentTypeName = "ExposureKeySetV3",
                Created = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) - TimeSpan.FromDays(id),
                Release = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) - TimeSpan.FromDays(id),
                Type = ContentTypes.ExposureKeySetV3
            });

            _contentDbContext.SaveChanges();
        }

        [Fact]
        public async Task Cleaner()
        {
            // Arrange
            await _contentDbContext.TruncateAsync<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksCommand>();
            var command = new RemoveExpiredEksCommand(_contentDbContext, new FakeEksConfig(), new StandardUtcDateTimeProvider(), expEksLogger);

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(0, result.Found);
            Assert.Equal(0, result.Zombies);
            Assert.Equal(0, result.GivenMercy);
            Assert.Equal(0, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }

        [Fact]
        public async Task NoKill()
        {
            // Arrange
            await _contentDbContext.TruncateAsync<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksCommand>();
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var command = new RemoveExpiredEksCommand(_contentDbContext, new FakeEksConfig(), fakeDtp, expEksLogger);

            Add(15);

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(1, result.Found);
            Assert.Equal(1, result.Zombies);
            Assert.Equal(0, result.GivenMercy);
            Assert.Equal(1, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }


        [Fact]
        public async Task Kill()
        {
            // Arrange
            await _contentDbContext.TruncateAsync<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksCommand>();
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var fakeEksConfig = new FakeEksConfig() { CleanupDeletesData = true };
            var command = new RemoveExpiredEksCommand(_contentDbContext, fakeEksConfig, fakeDtp, expEksLogger);

            Add(15);

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(1, result.Found);
            Assert.Equal(1, result.Zombies);
            Assert.Equal(1, result.GivenMercy);
            Assert.Equal(0, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }


        [Fact]
        public async Task MoreRealistic()
        {
            // Arrange
            await _contentDbContext.BulkDeleteAsync(_contentDbContext.Content.ToList());

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksCommand>();
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var fakeEksConfig = new FakeEksConfig() { CleanupDeletesData = true };
            var command = new RemoveExpiredEksCommand(_contentDbContext, fakeEksConfig, fakeDtp, expEksLogger);

            for (var i = 0; i < 20; i++)
            {
                Add(i);
            }

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(20, result.Found);
            Assert.Equal(5, result.Zombies);
            Assert.Equal(5, result.GivenMercy);
            Assert.Equal(15, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }

        [Fact]
        public async Task CleanerV2()
        {
            // Arrange
            await _contentDbContext.TruncateAsync<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksV2Command>();
            var command = new RemoveExpiredEksV2Command(_contentDbContext, new FakeEksConfig(), new StandardUtcDateTimeProvider(), expEksLogger);

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(0, result.Found);
            Assert.Equal(0, result.Zombies);
            Assert.Equal(0, result.GivenMercy);
            Assert.Equal(0, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }

        [Fact]
        public async Task NoKillV2()
        {
            // Arrange
            await _contentDbContext.TruncateAsync<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksV2Command>();
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var command = new RemoveExpiredEksV2Command(_contentDbContext, new FakeEksConfig(), fakeDtp, expEksLogger);

            Add(15);

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(1, result.Found);
            Assert.Equal(1, result.Zombies);
            Assert.Equal(0, result.GivenMercy);
            Assert.Equal(1, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }


        [Fact]
        public async Task KillV2()
        {
            // Arrange
            await _contentDbContext.TruncateAsync<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksV2Command>();
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var fakeEksConfig = new FakeEksConfig() { CleanupDeletesData = true };
            var command = new RemoveExpiredEksV2Command(_contentDbContext, fakeEksConfig, fakeDtp, expEksLogger);

            Add(15);

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(1, result.Found);
            Assert.Equal(1, result.Zombies);
            Assert.Equal(1, result.GivenMercy);
            Assert.Equal(0, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }


        [Fact]
        public async Task MoreRealisticV2()
        {
            // Arrange
            await _contentDbContext.BulkDeleteAsync(_contentDbContext.Content.ToList());

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksV2Command>();
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var fakeEksConfig = new FakeEksConfig() { CleanupDeletesData = true };
            var command = new RemoveExpiredEksV2Command(_contentDbContext, fakeEksConfig, fakeDtp, expEksLogger);

            for (var i = 0; i < 20; i++)
            {
                Add(i);
            }

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(20, result.Found);
            Assert.Equal(5, result.Zombies);
            Assert.Equal(5, result.GivenMercy);
            Assert.Equal(15, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }

        [Fact]
        public async Task CleanerV3()
        {
            // Arrange
            await _contentDbContext.TruncateAsync<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksV3Command>();
            var command = new RemoveExpiredEksV3Command(_contentDbContext, new FakeEksConfig(), new StandardUtcDateTimeProvider(), expEksLogger);

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(0, result.Found);
            Assert.Equal(0, result.Zombies);
            Assert.Equal(0, result.GivenMercy);
            Assert.Equal(0, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }

        [Fact]
        public async Task NoKillV3()
        {
            // Arrange
            await _contentDbContext.TruncateAsync<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksV3Command>();
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var command = new RemoveExpiredEksV3Command(_contentDbContext, new FakeEksConfig(), fakeDtp, expEksLogger);

            Add(15);

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(1, result.Found);
            Assert.Equal(1, result.Zombies);
            Assert.Equal(0, result.GivenMercy);
            Assert.Equal(1, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }


        [Fact]
        public async Task KillV3()
        {
            // Arrange
            await _contentDbContext.TruncateAsync<ContentEntity>();

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksV3Command>();
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var fakeEksConfig = new FakeEksConfig() { CleanupDeletesData = true };
            var command = new RemoveExpiredEksV3Command(_contentDbContext, fakeEksConfig, fakeDtp, expEksLogger);

            Add(15);

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(1, result.Found);
            Assert.Equal(1, result.Zombies);
            Assert.Equal(1, result.GivenMercy);
            Assert.Equal(0, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }


        [Fact]
        public async Task MoreRealisticV3()
        {
            // Arrange
            await _contentDbContext.BulkDeleteAsync(_contentDbContext.Content.ToList());

            var lf = new LoggerFactory();
            var expEksLogger = lf.CreateLogger<RemoveExpiredEksV3Command>();
            var fakeDtp = new FakeDtp() { Snapshot = new DateTime(2020, 6, 20, 0, 0, 0, DateTimeKind.Utc) };
            var fakeEksConfig = new FakeEksConfig() { CleanupDeletesData = true };
            var command = new RemoveExpiredEksV3Command(_contentDbContext, fakeEksConfig, fakeDtp, expEksLogger);

            for (var i = 0; i < 20; i++)
            {
                Add(i);
            }

            // Act
            var result = (RemoveExpiredEksCommandResult)await command.ExecuteAsync();

            // Assert
            Assert.Equal(20, result.Found);
            Assert.Equal(5, result.Zombies);
            Assert.Equal(5, result.GivenMercy);
            Assert.Equal(15, result.Remaining);
            Assert.Equal(0, result.Reconciliation);
        }
    }
}
