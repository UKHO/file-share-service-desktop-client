using FakeItEasy;
using NUnit.Framework;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareAdminClient.Models.Response;
using UKHO.FileShareClient.Models;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Events;
using UKHO.FileShareService.DesktopClient.Modules.Search;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    [TestFixture]
    public class BatchDetailViewModelTest
    {
        private IFileShareApiAdminClientFactory fakeFileShareApiAdminClientFactory = null!;
        private IMessageBoxService fakeMessageBoxService = null!;
        private IFileService fakeFileService = null!;
        private IFileShareApiAdminClient fakeFileShareApiAdminClient = null!;
        private ISaveFileDialogService fakesaveFileDialogService = null!;
        private IEventAggregator fakeEventAggregator = null!;
        private BatchExpiredEvent fakeBatchExpiredEvent = null!;

        [SetUp]
        public void Setup()
        {
            fakeFileShareApiAdminClientFactory = A.Fake<IFileShareApiAdminClientFactory>();
            fakeMessageBoxService = A.Fake<IMessageBoxService>();
            fakeFileService = A.Fake<IFileService>();
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            fakesaveFileDialogService = A.Fake<ISaveFileDialogService>();
            fakeEventAggregator = A.Fake<IEventAggregator>();
            fakeBatchExpiredEvent = A.Fake<BatchExpiredEvent>();
        }
        [Test]
        public void TestDownloadFileWithFileExistsAndYesResponse()
        {

            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService, fakesaveFileDialogService, fakeEventAggregator);

            BatchDetailVM.Files = new List<BatchDetailsFiles>() { new BatchDetailsFiles() { Filename = "AFilename.txt", FileSize = 100 } };
            A.CallTo(() => fakesaveFileDialogService.SaveFileDialog(A<string>.Ignored)).Returns(Environment.CurrentDirectory);

            BatchDetailVM.BatchId = Guid.NewGuid().ToString();
            string fileDownloadPath = Path.Combine(Environment.CurrentDirectory, "AFilename.txt");
            A.CallTo(() => fakeFileService.Exists(fileDownloadPath)).Returns(true);
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, MessageBoxButton.YesNo, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.Yes);
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<FileStream>.Ignored, A<long>.Ignored, A<CancellationToken>.Ignored)).Returns(new Result<DownloadFileResponse> { IsSuccess = true, StatusCode = 206 });

            BatchDetailVM.DownloadExecutionCommand.Execute("AFilename.txt");

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, MessageBoxButton.OK, A<MessageBoxImage>.Ignored)).MustHaveHappened();
            A.CallTo(() => fakesaveFileDialogService.SaveFileDialog("AFilename.txt")).MustHaveHappened();
            A.CallTo(() => fakeFileShareApiAdminClient.DownloadFileAsync(A<string>.Ignored, "AFilename.txt", A<FileStream>.Ignored, 100, A<CancellationToken>.Ignored)).MustHaveHappened();

        }

        [Test]
        public void TestDownloadFileWithFileExistsAndNoResponse()
        {
            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService, fakesaveFileDialogService, fakeEventAggregator);

            BatchDetailVM.Files = new List<BatchDetailsFiles>() { new BatchDetailsFiles() { Filename = "AFilename.txt", FileSize = 100 } };
            A.CallTo(() => fakesaveFileDialogService.SaveFileDialog(A<string>.Ignored)).Returns(Environment.CurrentDirectory);

            BatchDetailVM.BatchId = Guid.NewGuid().ToString();
            string fileDownloadPath = Path.Combine(Environment.CurrentDirectory, "AFilename.txt");
            A.CallTo(() => fakeFileService.Exists(fileDownloadPath)).Returns(true);
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, MessageBoxButton.YesNo, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.No);

            BatchDetailVM.DownloadExecutionCommand.Execute("AFilename.txt");

            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).MustNotHaveHappened();
            A.CallTo(() => fakeFileShareApiAdminClient.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<FileStream>.Ignored, A<long>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, MessageBoxButton.OK, A<MessageBoxImage>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        public void TestDownloadFileWithFileNotExists()
        {
            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService, fakesaveFileDialogService, fakeEventAggregator);

            BatchDetailVM.Files = new List<BatchDetailsFiles>() { new BatchDetailsFiles() { Filename = "AFilename.txt", FileSize = 100 } };
            A.CallTo(() => fakesaveFileDialogService.SaveFileDialog(A<string>.Ignored)).Returns(Environment.CurrentDirectory);

            BatchDetailVM.BatchId = Guid.NewGuid().ToString();
            string fileDownloadPath = Path.Combine(Environment.CurrentDirectory, "AFilename.txt");
            A.CallTo(() => fakeFileService.Exists(fileDownloadPath)).Returns(false);
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<FileStream>.Ignored, A<long>.Ignored, A<CancellationToken>.Ignored)).Returns(new Result<DownloadFileResponse> { IsSuccess = true, StatusCode = 206 });

            BatchDetailVM.DownloadExecutionCommand.Execute("AFilename.txt");

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, MessageBoxButton.YesNo, A<MessageBoxImage>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, MessageBoxButton.OK, A<MessageBoxImage>.Ignored)).MustHaveHappened();
        }

        [Test]
        public void WhenUserAnswersNoToWarningMessageThenDoNotExpireBatch()
        {
            var batchId = Guid.NewGuid().ToString();
            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService, fakesaveFileDialogService, fakeEventAggregator);

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Expire Batch", A<string>.Ignored, MessageBoxButton.YesNo, MessageBoxImage.Warning)).Returns(MessageBoxResult.No);
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);

            BatchDetailVM.ExpireBatchExecutionCommand.Execute(batchId);

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Expire Batch", A<string>.Ignored, MessageBoxButton.YesNo, MessageBoxImage.Warning)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).MustNotHaveHappened();
        }

        [Test]
        [TestCase(MessageBoxResult.Yes, true, HttpStatusCode.NoContent, null, TestName = "WhenSetExpiryDateResponseIsSuccessThenPublishBatchExpiredEvent")]
        [TestCase(MessageBoxResult.Yes, false, HttpStatusCode.BadRequest, null, TestName = "WhenSetExpiryDateResponseIsNotSuccessThenShowErrorMessage")]
        [TestCase(MessageBoxResult.Yes, false, HttpStatusCode.BadRequest, "test error description", TestName = "WhenSetExpiryDateResponseIsNotSuccessThenShowErrorMessageOfResponse")]
        public void TestExpireBatch(MessageBoxResult expireBatchWarning, bool setExpiryDateSuccessResponse,
            HttpStatusCode responseCode, string errorDescription)
        {
            var batchId = Guid.NewGuid().ToString();
            var errorMessage = $"File Share Service set expiry date failed for batch ID:{batchId} with status: {(int)responseCode}.";

            var expectedSetExpiryDateResponse = new Result<SetExpiryDateResponse>
            {
                IsSuccess = setExpiryDateSuccessResponse,
                StatusCode = (int)responseCode
            };

            if (!setExpiryDateSuccessResponse)
            {
                if (!string.IsNullOrEmpty(errorDescription))
                {
                    errorMessage = errorDescription;
                    expectedSetExpiryDateResponse.Errors.Add(new Error { Description = errorDescription });
                }

                _ = A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Error", errorMessage, MessageBoxButton.OK, MessageBoxImage.Error));
            }

            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService, fakesaveFileDialogService, fakeEventAggregator);

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Expire Batch", A<string>.Ignored, MessageBoxButton.YesNo, MessageBoxImage.Warning)).Returns(expireBatchWarning);
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(batchId, A<BatchExpiryModel>.Ignored, A<CancellationToken>.Ignored)).Returns(expectedSetExpiryDateResponse);
            A.CallTo(() => fakeEventAggregator.GetEvent<BatchExpiredEvent>()).Returns(fakeBatchExpiredEvent);
            _ = A.CallTo(() => fakeBatchExpiredEvent.Publish());

            BatchDetailVM.ExpireBatchExecutionCommand.Execute(batchId);

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Expire Batch", A<string>.Ignored, MessageBoxButton.YesNo, MessageBoxImage.Warning)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(batchId, A<BatchExpiryModel>.Ignored, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeEventAggregator.GetEvent<BatchExpiredEvent>()).MustHaveHappened(setExpiryDateSuccessResponse ? 1 : 0, Times.Exactly);
            A.CallTo(() => fakeBatchExpiredEvent.Publish()).MustHaveHappened(setExpiryDateSuccessResponse ? 1 : 0, Times.Exactly);
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Error", errorMessage, MessageBoxButton.OK, MessageBoxImage.Error)).MustHaveHappened(setExpiryDateSuccessResponse ? 0 : 1, Times.Exactly);
        }

        [Test]
        public void WhenExceptionIsThrownThenShowErrorMessage()
        {
            var batchId = Guid.NewGuid().ToString();
            var exception = new Exception("test error message");
            var errorMessage = $"File Share Service set expiry date failed for batch ID: {batchId}. \n Error: {exception.Message}";

            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService, fakesaveFileDialogService, fakeEventAggregator);

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Expire Batch", A<string>.Ignored, MessageBoxButton.YesNo, MessageBoxImage.Warning)).Returns(MessageBoxResult.Yes);
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Throws(new Exception("test error message"));
            _ = A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Error", errorMessage, MessageBoxButton.OK, MessageBoxImage.Error));

            BatchDetailVM.ExpireBatchExecutionCommand.Execute(batchId);

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Expire Batch", A<string>.Ignored, MessageBoxButton.YesNo, MessageBoxImage.Warning)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox("Error", errorMessage, MessageBoxButton.OK, MessageBoxImage.Error)).MustHaveHappenedOnceExactly();
        }
    }
}
