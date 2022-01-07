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

        [SetUp]
        public void Setup()
        {
            fakeFileShareApiAdminClientFactory = A.Fake<IFileShareApiAdminClientFactory>();
            fakeMessageBoxService = A.Fake<IMessageBoxService>();
            fakeFileService = A.Fake<IFileService>();
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
        }
        [Test]
        public async Task TestDownloadFileWithFileExistsAndYesResponse()
        {           
            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService);
            var batchId = Guid.NewGuid().ToString();
            string fileDownloadPath = Path.Combine(Environment.CurrentDirectory, "AFilename.txt");
           
            A.CallTo(() => fakeFileService.Exists(fileDownloadPath)).Returns(true);
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.Yes);
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<FileStream>.Ignored, A<long>.Ignored, CancellationToken.None)).Returns(new Result<DownloadFileResponse> { IsSuccess=true ,StatusCode = 206 });
        
            var result = await BatchDetailVM.DownlodFile(batchId, fileDownloadPath, "AFilename.txt", 10, CancellationToken.None);
        
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(result.StatusCode, 206);
        }

        [Test]
        public async Task TestDownloadFileWithFileExistsAndNoResponse()
        {
            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService);
            var batchId = Guid.NewGuid().ToString();
            string fileDownloadPath = Path.Combine(Environment.CurrentDirectory, "AFilename.txt");
    
            A.CallTo(() => fakeFileService.Exists(fileDownloadPath)).Returns(true);
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).Returns(MessageBoxResult.No);
    
            await BatchDetailVM.DownlodFile(batchId, fileDownloadPath, "AFilename.txt", 10, CancellationToken.None);
    
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<FileStream>.Ignored, A<long>.Ignored, CancellationToken.None)).MustNotHaveHappened();
        }

        [Test]
        public async Task TestDownloadFileWithFileNotExists()
        {
            var BatchDetailVM = new BatchDetailsViewModel(fakeFileShareApiAdminClientFactory, fakeMessageBoxService, fakeFileService);

            var batchId = Guid.NewGuid().ToString();
            string fileDownloadPath = Path.Combine(Environment.CurrentDirectory, "AFilename.txt");

            A.CallTo(() => fakeFileService.Exists(fileDownloadPath)).Returns(false);
            A.CallTo(() => fakeFileShareApiAdminClientFactory.Build()).Returns(fakeFileShareApiAdminClient);
            A.CallTo(() => fakeFileShareApiAdminClient.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<FileStream>.Ignored, A<long>.Ignored, CancellationToken.None)).Returns(new Result<DownloadFileResponse> { IsSuccess = true, StatusCode = 206 });
            
            var result = await BatchDetailVM.DownlodFile(batchId, fileDownloadPath, "AFilename.txt", 10, CancellationToken.None);
           
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(result.StatusCode, 206);
            A.CallTo(() => fakeMessageBoxService.ShowMessageBox(A<string>.Ignored, A<string>.Ignored, A<MessageBoxButton>.Ignored, A<MessageBoxImage>.Ignored)).MustNotHaveHappened();
        }       
    }
}
