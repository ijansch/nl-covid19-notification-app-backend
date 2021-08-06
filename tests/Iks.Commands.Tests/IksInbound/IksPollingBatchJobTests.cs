// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Core;
using NL.Rijksoverheid.ExposureNotification.BackEnd.EfgsDownloader.Jobs;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Commands.Inbound;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Downloader.Entities;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Downloader.EntityFramework;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Iks.Commands.Tests.IksInbound
{
    public class IksPollingBatchJobTests : IDisposable
    {
        private readonly IksInDbContext _iksInDbContext;
        private static DbConnection connection;

        public IksPollingBatchJobTests()
        {
            _iksInDbContext = new IksInDbContext(new DbContextOptionsBuilder<IksInDbContext>().UseSqlite(CreateInMemoryDatabase()).Options);
            _iksInDbContext.Database.EnsureCreated();
        }
        private static DbConnection CreateInMemoryDatabase()
        {
            connection = new SqliteConnection("Filename=:memory:");

            connection.Open();

            return connection;
        }

        public void Dispose() => connection.Dispose();

        [Fact]
        public async void Tests_that_entire_sequence_of_batches_are_downloaded()
        {
            // Assemble: test state
            var downloadedBatches = new List<IksWriteArgs>();

            // Assemble: other object
            var logger = new IksDownloaderLoggingExtensions(new NullLogger<IksDownloaderLoggingExtensions>());
            var now = DateTime.UtcNow;
            var dtp = new Mock<IUtcDateTimeProvider>();
            dtp.Setup(_ => _.Snapshot).Returns(now);
            var writer = new Mock<IIksWriterCommand>();
            writer.Setup(_ => _.Execute(It.IsAny<IksWriteArgs>()))
                .Callback((IksWriteArgs args) => downloadedBatches.Add(args));

            var datePart = dtp.Object.Snapshot.Date.ToString("yyyyMMdd");

            // Assemble: configure the receiver to return the first sequence of files
            var responses = new List<HttpGetIksSuccessResult>
            {
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-1", Content = new byte[] {0x0, 0x0}, NextBatchTag = $"{datePart}-2"},
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-2", Content = new byte[] {0x0, 0x0}, NextBatchTag = $"{datePart}-3"},
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-3", Content = new byte[] {0x0, 0x0}, NextBatchTag = null}
            };
            var receiver = FixedResultHttpGetIksCommand.Create(responses);

            // Assemble: create the job to be tested
            var sut = new IksPollingBatchJob(dtp.Object, receiver, writer.Object,
                _iksInDbContext, new EfgsConfigMock(), logger);

            // Act
            sut.Run();

            // Assert
            Assert.Equal(3, downloadedBatches.Count);
            Assert.Equal($"{datePart}-1", downloadedBatches[0].BatchTag);
            Assert.Equal($"{datePart}-2", downloadedBatches[1].BatchTag);
            Assert.Equal($"{datePart}-3", downloadedBatches[2].BatchTag);
        }

        [Fact]
        public async void Tests_that_batch_is_only_downloaded_once()
        {
            // Assemble: test state
            var downloadedBatches = new List<IksWriteArgs>();

            // Assemble: other object
            var logger = new IksDownloaderLoggingExtensions(new NullLogger<IksDownloaderLoggingExtensions>());
            var now = DateTime.UtcNow;
            var dtp = new Mock<IUtcDateTimeProvider>();
            dtp.Setup(_ => _.Snapshot).Returns(now);
            var writer = new Mock<IIksWriterCommand>();
            writer.Setup(_ => _.Execute(It.IsAny<IksWriteArgs>()))
                .Callback((IksWriteArgs args) => downloadedBatches.Add(args));

            var datePart = dtp.Object.Snapshot.Date.ToString("yyyyMMdd");

            // Assemble: configure the receiver to return the first sequence of files
            var responses = new List<HttpGetIksSuccessResult>
            {
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-1", Content = new byte[] {0x0, 0x0}, NextBatchTag = null}
            };
            var receiver = FixedResultHttpGetIksCommand.Create(responses);

            // Assemble: create the job to be tested
            var sut = new IksPollingBatchJob(dtp.Object, receiver, writer.Object,
                _iksInDbContext, new EfgsConfigMock(), logger);

            // Act
            sut.Run();
            sut.Run();
            sut.Run();

            // Assert
            Assert.Single(downloadedBatches);
        }

        [Fact]
        public async void Tests_that_batches_added_between_calls_are_downloaded()
        {
            // Assemble: test state
            var downloadedBatches = new List<IksWriteArgs>();

            // Assemble: other object
            var logger = new IksDownloaderLoggingExtensions(new NullLogger<IksDownloaderLoggingExtensions>());

            var now = DateTime.UtcNow;
            var dtp = new Mock<IUtcDateTimeProvider>();
            dtp.Setup(_ => _.Snapshot).Returns(now);
            var writer = new Mock<IIksWriterCommand>();
            writer.Setup(_ => _.Execute(It.IsAny<IksWriteArgs>()))
                .Callback((IksWriteArgs args) => downloadedBatches.Add(args));

            var datePart = dtp.Object.Snapshot.Date.ToString("yyyyMMdd");

            // Assemble: configure the receiver to return the first sequence of files
            var responses = new List<HttpGetIksSuccessResult>
            {
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-1", Content = new byte[] {0x0, 0x0}, NextBatchTag = null}
            };
            var receiver = FixedResultHttpGetIksCommand.Create(responses);

            // Assemble: create the job to be tested
            var sut = new IksPollingBatchJob(dtp.Object, receiver, writer.Object,
                _iksInDbContext, new EfgsConfigMock(), logger);

            // Act
            sut.Run();

            // Assert
            Assert.Single(downloadedBatches);

            // Assemble: add another batch
            receiver.AddItem(new HttpGetIksSuccessResult
            { BatchTag = $"{datePart}-2", Content = new byte[] { 0x0, 0x0 }, NextBatchTag = null });

            // Act
            sut.Run();

            // Assert
            Assert.Equal(2, downloadedBatches.Count);
            Assert.Equal($"{datePart}-1", downloadedBatches[0].BatchTag);
            Assert.Equal($"{datePart}-2", downloadedBatches[1].BatchTag);
        }

        [Fact]
        public async void Tests_that_batches_are_downloaded_once_if_received_multiple_times()
        {
            // Assemble: test state
            var downloadedBatches = new List<IksWriteArgs>();

            // Assemble: other object
            var logger = new IksDownloaderLoggingExtensions(new NullLogger<IksDownloaderLoggingExtensions>());
            var now = DateTime.UtcNow;
            var dtp = new Mock<IUtcDateTimeProvider>();
            dtp.Setup(_ => _.Snapshot).Returns(now);
            var writer = new Mock<IIksWriterCommand>();
            writer.Setup(_ => _.Execute(It.IsAny<IksWriteArgs>()))
                .Callback((IksWriteArgs args) =>
                {
                    downloadedBatches.Add(args);
                    _iksInDbContext.Received.Add(new IksInEntity
                    {
                        BatchTag = args.BatchTag,
                        Content = args.Content,
                        Created = now
                    });
                    _iksInDbContext.SaveChanges();
                });

            var datePart = dtp.Object.Snapshot.Date.ToString("yyyyMMdd");

            // Assemble: configure the receiver to return the first sequence of files
            var responses = new List<HttpGetIksSuccessResult>
            {
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-1", Content = new byte[] {0x0, 0x0}, NextBatchTag = $"{datePart}-2"},
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-2", Content = new byte[] {0x0, 0x0}, NextBatchTag = $"{datePart}-3"},
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-3", Content = new byte[] {0x0, 0x0}, NextBatchTag = $"{datePart}-1"},
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-1", Content = new byte[] {0x0, 0x0}, NextBatchTag = $"{datePart}-3"},
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-2", Content = new byte[] {0x0, 0x0}, NextBatchTag = $"{datePart}-2"},
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-3", Content = new byte[] {0x0, 0x0}, NextBatchTag = null}
            };
            var receiver = FixedResultHttpGetIksCommand.Create(responses);

            // Assemble: create the job to be tested
            var sut = new IksPollingBatchJob(dtp.Object, receiver, writer.Object,
                _iksInDbContext, new EfgsConfigMock(), logger);

            // Act
            sut.Run();

            // Assert
            Assert.Equal(3, downloadedBatches.Count);
            Assert.Equal($"{datePart}-1", downloadedBatches[0].BatchTag);
            Assert.Equal($"{datePart}-2", downloadedBatches[1].BatchTag);
            Assert.Equal($"{datePart}-3", downloadedBatches[2].BatchTag);
        }

        [Fact]
        public async void Tests_that_batch_downloads_over_multiple_days_are_processed_in_expected_order()
        {
            // Assemble: test state
            var downloadedBatches = new List<IksWriteArgs>();

            // Assemble: other object
            var logger = new IksDownloaderLoggingExtensions(new NullLogger<IksDownloaderLoggingExtensions>());

            var now = DateTime.UtcNow;
            var dtp = new Mock<IUtcDateTimeProvider>();
            dtp.Setup(_ => _.Snapshot).Returns(now);
            var writer = new Mock<IIksWriterCommand>();
            writer.Setup(_ => _.Execute(It.IsAny<IksWriteArgs>()))
                .Callback((IksWriteArgs args) => downloadedBatches.Add(args));

            var datePart = dtp.Object.Snapshot.Date.ToString("yyyyMMdd");

            // Assemble: configure receiver to return batches for the FIRST day
            var responses = new List<HttpGetIksSuccessResult>()
            {
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-1", Content = new byte[] {0x0, 0x0}, NextBatchTag = $"{datePart}-2"},
                new HttpGetIksSuccessResult {BatchTag = $"{datePart}-2", Content = new byte[] {0x0, 0x0}, NextBatchTag = null},
            };
            var receiver = FixedResultHttpGetIksCommand.Create(responses, now.AddDays(-1));

            // Assemble: create the job to be tested
            var sut = new IksPollingBatchJob(dtp.Object, receiver, writer.Object,
                _iksInDbContext, new EfgsConfigMock(), logger);

            // Act - process files for FIRST day
            sut.Run();

            // Assert
            Assert.Equal(2, downloadedBatches.Count);

            // Assemble: add the batches for the SECOND day to the receiver
            var secondDayDatePart = dtp.Object.Snapshot.Date.AddDays(1).ToString("yyyyMMdd");
            receiver.AddItem(new HttpGetIksSuccessResult
            { BatchTag = $"{secondDayDatePart}-1", Content = new byte[] { 0x0, 0x0 }, NextBatchTag = $"{secondDayDatePart}-2" }, now);
            receiver.AddItem(new HttpGetIksSuccessResult
            { BatchTag = $"{secondDayDatePart}-2", Content = new byte[] { 0x0, 0x0 }, NextBatchTag = null }, now);

            // Act - process files for SECOND day
            sut.Run();

            // Assert
            Assert.Equal(4, downloadedBatches.Count);
            Assert.Equal($"{datePart}-1", downloadedBatches[0].BatchTag);
            Assert.Equal($"{datePart}-2", downloadedBatches[1].BatchTag);
            Assert.Equal($"{secondDayDatePart}-1", downloadedBatches[2].BatchTag);
            Assert.Equal($"{secondDayDatePart}-2", downloadedBatches[3].BatchTag);
        }
    }
}
