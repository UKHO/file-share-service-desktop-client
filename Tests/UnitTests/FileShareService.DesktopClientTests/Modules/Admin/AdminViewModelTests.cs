using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Modules.Admin;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;

namespace FileShareService.DesktopClientTests.Modules.Admin
{
    public class AdminViewModelTests
    {
        private MockFileSystem mockFileSystem = null!;
        private IKeyValueStore fakeKeyValueStore = null!;
        private IJobsParser fakeJobsParser = null!;

        [SetUp]
        public void Setup()
        {
            mockFileSystem = new MockFileSystem();
            fakeKeyValueStore = A.Fake<IKeyValueStore>();
            fakeJobsParser = A.Fake<IJobsParser>();
        }

        [TestCase]
        public void TestLoadBatchJobsCreatesJobViewModels()
        {
            var vm = new AdminViewModel(mockFileSystem, fakeKeyValueStore, fakeJobsParser,
                A.Fake<IFileShareApiAdminClientFactory>(),
                A.Fake<ICurrentDateTimeProvider>());
            var jobsFilePath = @"c:\jobs.json";
            mockFileSystem.AddFile(jobsFilePath, new MockFileData("JsonContent"));

            A.CallTo(() => fakeJobsParser.Parse("JsonContent"))
                .Returns(new Jobs {jobs =new List<IJob> {new NewBatchJob(), new AppendAclJob(), new SetExpiryDateJob()}});

            vm.LoadBatchJobsFile(jobsFilePath);


            var batchJobViewModels = vm.BatchJobs.ToList();
            Assert.IsInstanceOf<NewBatchJobViewModel>(batchJobViewModels[0]);
            Assert.IsInstanceOf<AppendAclJobViewModel>(batchJobViewModels[1]);
            Assert.IsInstanceOf<SetExpiryDateJobViewModel>(batchJobViewModels[2]);
        }
    }
}