using FakeItEasy;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UKHO.FileShareClient.Models;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Modules.Search;
using UKHO.FileShareAdminClient;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    [TestFixture]
    public class BatchDetailViewModelTest
    {
        private IFileShareApiAdminClientFactory fakeFileShareApiAdminClientFactory = null!;
        private IMessageBoxService fakeMessageBoxService = null!;
        private  IFileService fakeFileService = null!;
        private IFileShareApiAdminClient fakeFileShareApiAdminClient = null!;
        private ISaveFileDialogService fakesaveFileDialogService = null!;

        [SetUp]
        public void Setup()
        {
            fakeFileShareApiAdminClientFactory = A.Fake<IFileShareApiAdminClientFactory>();
            fakeMessageBoxService = A.Fake<IMessageBoxService>();
            fakeFileService = A.Fake<IFileService>();
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            fakesaveFileDialogService = A.Fake<ISaveFileDialogService>();
        }
        [Test]
        public void TestDownloadFileWithFileExistsAndYesResponse()
        {
           
            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService , fakesaveFileDialogService);
            
            BatchDetailVM.Files = new List<BatchDetailsFiles>() { new BatchDetailsFiles() { Filename = "AFilename.txt", FileSize = 100 } };
            A.CallTo(() => fakesaveFileDialogService.SaveFileDialog(A<string>.Ignored)).Returns(Environment.CurrentDirectory);

            BatchDetailVM.BatchId = Guid.NewGuid().ToString();
            string fileDownloadPath = Path.Combine(Environment.CurrentDirectory, "AFilename.txt");          
            A.CallTo(() => fakeFileService.Exists(fileDownloadPath)).Returns(true);
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored,MessageBoxButton.YesNo, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.Yes);
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
            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService, fakesaveFileDialogService);

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
            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService, fakesaveFileDialogService);

            BatchDetailVM.Files = new List<BatchDetailsFiles>() { new BatchDetailsFiles() { Filename = "AFilename.txt", FileSize = 100 } };
            A.CallTo(() => fakesaveFileDialogService.SaveFileDialog(A<string>.Ignored)).Returns(Environment.CurrentDirectory);

            BatchDetailVM.BatchId = Guid.NewGuid().ToString();
            string fileDownloadPath = Path.Combine(Environment.CurrentDirectory, "AFilename.txt");
            A.CallTo(() => fakeFileService.Exists(fileDownloadPath)).Returns(false);
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<FileStream>.Ignored, A<long>.Ignored, A<CancellationToken>.Ignored)).Returns(new Result<DownloadFileResponse> { IsSuccess = true, StatusCode = 206 });

            BatchDetailVM.DownloadExecutionCommand.Execute("AFilename.txt");

            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored,MessageBoxButton.YesNo, A<MessageBoxImage>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, MessageBoxButton.OK, A<MessageBoxImage>.Ignored)).MustHaveHappened();
        }       
    }
}
