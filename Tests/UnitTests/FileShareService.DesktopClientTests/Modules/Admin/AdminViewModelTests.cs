using System.Collections.Generic;
using System.ComponentModel;
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
        private IEnvironmentsManager fakeEnvironmentsManager = null!;

        [SetUp]
        public void Setup()
        {
            mockFileSystem = new MockFileSystem();
            fakeKeyValueStore = A.Fake<IKeyValueStore>();
            fakeJobsParser = A.Fake<IJobsParser>();
            fakeEnvironmentsManager = A.Fake<IEnvironmentsManager>();
        }

        [Test]
        public void TestLoadBatchJobsCreatesJobViewModels()
        {
            var vm = new AdminViewModel(mockFileSystem, fakeKeyValueStore, fakeJobsParser,
                A.Fake<IFileShareApiAdminClientFactory>(),
                A.Fake<ICurrentDateTimeProvider>(),
                fakeEnvironmentsManager);
            var jobsFilePath = @"c:\jobs.json";
            mockFileSystem.AddFile(jobsFilePath, new MockFileData("JsonContent"));

            A.CallTo(() => fakeJobsParser.Parse("JsonContent"))
                .Returns(new Jobs
                    {jobs = new List<IJob> {new NewBatchJob(), new AppendAclJob(), new SetExpiryDateJob()}});

            vm.LoadBatchJobsFile(jobsFilePath);


            var batchJobViewModels = vm.BatchJobs.ToList();
            Assert.IsInstanceOf<NewBatchJobViewModel>(batchJobViewModels[0]);
            Assert.IsInstanceOf<AppendAclJobViewModel>(batchJobViewModels[1]);
            Assert.IsInstanceOf<SetExpiryDateJobViewModel>(batchJobViewModels[2]);
        }

        [Test]
        public void TestChangeToEnvironmentsClearsBatchJobs()
        {
            var vm = new AdminViewModel(mockFileSystem, fakeKeyValueStore, fakeJobsParser,
                A.Fake<IFileShareApiAdminClientFactory>(),
                A.Fake<ICurrentDateTimeProvider>(),
                fakeEnvironmentsManager);
            var jobsFilePath = @"c:\jobs.json";
            mockFileSystem.AddFile(jobsFilePath, new MockFileData("JsonContent"));

            A.CallTo(() => fakeJobsParser.Parse("JsonContent"))
                .Returns(new Jobs
                    {jobs = new List<IJob> {new NewBatchJob(), new AppendAclJob(), new SetExpiryDateJob()}});

            vm.LoadBatchJobsFile(jobsFilePath);

            Assert.AreEqual(3, vm.BatchJobs.Count());

            vm.AssertPropertyChanged(nameof(vm.BatchJobs), () =>
            {
                fakeEnvironmentsManager.PropertyChanged +=
                    Raise.FreeForm<PropertyChangedEventHandler>.With(fakeEnvironmentsManager,
                        new PropertyChangedEventArgs(nameof(fakeEnvironmentsManager.CurrentEnvironment)));
            });

            Assert.AreEqual(0, vm.BatchJobs.Count());
        }
    }
}