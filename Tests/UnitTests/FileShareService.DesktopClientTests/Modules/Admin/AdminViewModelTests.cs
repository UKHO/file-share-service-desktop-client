using System.IO.Abstractions.TestingHelpers;
using FakeItEasy;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Admin;

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
        public void TestWibble()
        {
            var vm = new AdminViewModel(mockFileSystem, fakeKeyValueStore, fakeJobsParser,
                A.Fake<IFileShareApiAdminClientFactory>(),
                A.Fake<ICurrentDateTimeProvider>());
            var jobsFilePath = @"c:\jobs.json";
            mockFileSystem.AddFile(jobsFilePath, new MockFileData("JsonContent"));
            vm.LoadBatchJobsFile(jobsFilePath);

            A.CallTo(() => fakeJobsParser.Parse("JsonContent")).MustHaveHappened();
        }
    }
}