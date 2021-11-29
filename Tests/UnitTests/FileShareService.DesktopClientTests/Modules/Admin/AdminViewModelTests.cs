using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FakeItEasy;
using Microsoft.Extensions.Logging;
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
        private ILogger<AdminViewModel> fakeLoggerAdminVM = null!;
        private ILogger<NewBatchJobViewModel> fakeLoggerNewBatchVM = null!;
        private ILogger<AppendAclJobViewModel> fakeLoggerAppendACLVM = null!;
        private ILogger<SetExpiryDateJobViewModel> fakeLoggerSetexpVM = null!;
        private ILogger<ErrorDeserializingJobsJobViewModel> fakeLoggerExpDtVM = null!;
       

        [SetUp]
        public void Setup()
        {
            mockFileSystem = new MockFileSystem();
            fakeKeyValueStore = A.Fake<IKeyValueStore>();
            fakeJobsParser = A.Fake<IJobsParser>();
            fakeEnvironmentsManager = A.Fake<IEnvironmentsManager>();
            fakeLoggerAdminVM = A.Fake<ILogger<AdminViewModel>>();
            fakeLoggerNewBatchVM = A.Fake<ILogger<NewBatchJobViewModel>>();
            fakeLoggerAppendACLVM = A.Fake<ILogger<AppendAclJobViewModel>>();
            fakeLoggerSetexpVM = A.Fake<ILogger<SetExpiryDateJobViewModel>>();
            fakeLoggerExpDtVM = A.Fake<ILogger<ErrorDeserializingJobsJobViewModel>>();         
        }

        [Test]
        public void TestLoadBatchJobsCreatesJobViewModels()
        {
            var vm = new AdminViewModel(mockFileSystem, fakeKeyValueStore, fakeJobsParser,
                A.Fake<IFileShareApiAdminClientFactory>(),
                A.Fake<ICurrentDateTimeProvider>(),
                fakeEnvironmentsManager,fakeLoggerAdminVM,fakeLoggerNewBatchVM,fakeLoggerAppendACLVM, fakeLoggerSetexpVM, fakeLoggerExpDtVM);
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
        public void TestLoadBadJobsFileCreatesErrorJobViewModel()
        {
            var vm = new AdminViewModel(mockFileSystem, fakeKeyValueStore, fakeJobsParser,
                A.Fake<IFileShareApiAdminClientFactory>(),
                A.Fake<ICurrentDateTimeProvider>(),
                fakeEnvironmentsManager,fakeLoggerAdminVM,fakeLoggerNewBatchVM, fakeLoggerAppendACLVM, fakeLoggerSetexpVM, fakeLoggerExpDtVM);
            var jobsFilePath = @"c:\jobs.json";
            mockFileSystem.AddFile(jobsFilePath, new MockFileData("JsonContent"));

            A.CallTo(() => fakeJobsParser.Parse("JsonContent"))
                .Returns(new Jobs
                    {jobs = new List<IJob> {new ErrorDeserializingJobsJob(new Exception("An error"))}});

            vm.LoadBatchJobsFile(jobsFilePath);


            var batchJobViewModels = vm.BatchJobs.ToList();
            Assert.IsInstanceOf<ErrorDeserializingJobsJobViewModel>(batchJobViewModels.Single());
        }

        [Test]
        public void TestChangeToEnvironmentsClearsBatchJobs()
        {
            var vm = new AdminViewModel(mockFileSystem, fakeKeyValueStore, fakeJobsParser,
                A.Fake<IFileShareApiAdminClientFactory>(),
                A.Fake<ICurrentDateTimeProvider>(),
                fakeEnvironmentsManager, fakeLoggerAdminVM, fakeLoggerNewBatchVM, fakeLoggerAppendACLVM, fakeLoggerSetexpVM, fakeLoggerExpDtVM);
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