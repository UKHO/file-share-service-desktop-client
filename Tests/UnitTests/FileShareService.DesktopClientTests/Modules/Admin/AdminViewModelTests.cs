﻿using System;
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
using UKHO.FileShareService.DesktopClient.Helper;
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
        private ILogger<AppendAclJobViewModel> fakeLoggerAppenAclVM = null!;
        private ILogger<SetExpiryDateJobViewModel> fakeLoggerSetExpiryDateVM = null!;
        private ILogger<ReplaceAclJobViewModel> fakeLoggerReplaceAclVM = null!;
        private ILogger<ErrorDeserializingJobsJobViewModel> fakeLoggerErrorDeserialisingVM = null!;
        private IMacroTransformer fakeMacroTransformer = null!;
        private IDateTimeValidator fakeDateTimeValidator = null!;
        private IMessageBoxService fakeMessageBoxService = null!;

        [SetUp]
        public void Setup()
        {
            mockFileSystem = new MockFileSystem();
            fakeKeyValueStore = A.Fake<IKeyValueStore>();
            fakeJobsParser = A.Fake<IJobsParser>();
            fakeEnvironmentsManager = A.Fake<IEnvironmentsManager>();
            fakeLoggerAdminVM = A.Fake<ILogger<AdminViewModel>>();
            fakeLoggerNewBatchVM = A.Fake<ILogger<NewBatchJobViewModel>>();
            fakeLoggerAppenAclVM = A.Fake<ILogger<AppendAclJobViewModel>>();
            fakeLoggerSetExpiryDateVM = A.Fake<ILogger<SetExpiryDateJobViewModel>>();
            fakeLoggerReplaceAclVM = A.Fake<ILogger<ReplaceAclJobViewModel>>();
            fakeLoggerErrorDeserialisingVM = A.Fake<ILogger<ErrorDeserializingJobsJobViewModel>>();
            fakeMacroTransformer = A.Fake<IMacroTransformer>();
            fakeDateTimeValidator = A.Fake<DateTimeValidator>();
            fakeMessageBoxService = A.Fake<IMessageBoxService>();
        }

        [Test]
        public void TestLoadBatchJobsCreatesJobViewModels()
        {
            var vm = new AdminViewModel(mockFileSystem, fakeKeyValueStore, fakeJobsParser,
                A.Fake<IFileShareApiAdminClientFactory>(),
                A.Fake<ICurrentDateTimeProvider>(),
                fakeMacroTransformer, fakeDateTimeValidator,
                fakeEnvironmentsManager,fakeLoggerAdminVM,fakeLoggerNewBatchVM, 
                fakeLoggerAppenAclVM, fakeLoggerSetExpiryDateVM, fakeLoggerReplaceAclVM, fakeLoggerErrorDeserialisingVM, fakeMessageBoxService);
            var jobsFilePath = @"c:\jobs.json";
            mockFileSystem.AddFile(jobsFilePath, new MockFileData("JsonContent"));

            A.CallTo(() => fakeJobsParser.Parse("JsonContent"))
                .Returns(new Jobs
                    {jobs = new List<IJob> {new NewBatchJob(), new AppendAclJob(), new SetExpiryDateJob(), new ReplaceAclJob()}});

            vm.LoadBatchJobsFile(jobsFilePath);


            var batchJobViewModels = vm.BatchJobs.ToList();
            Assert.IsInstanceOf<NewBatchJobViewModel>(batchJobViewModels[0]);
            Assert.IsInstanceOf<AppendAclJobViewModel>(batchJobViewModels[1]);
            Assert.IsInstanceOf<SetExpiryDateJobViewModel>(batchJobViewModels[2]);
            Assert.IsInstanceOf<ReplaceAclJobViewModel>(batchJobViewModels[3]);
        }

        [Test]
        public void TestLoadBadJobsFileCreatesErrorJobViewModel()
        {
            var vm = new AdminViewModel(mockFileSystem, fakeKeyValueStore, fakeJobsParser,
                A.Fake<IFileShareApiAdminClientFactory>(),
                A.Fake<ICurrentDateTimeProvider>(),
                fakeMacroTransformer, fakeDateTimeValidator,
                fakeEnvironmentsManager,fakeLoggerAdminVM,fakeLoggerNewBatchVM,
                 fakeLoggerAppenAclVM, fakeLoggerSetExpiryDateVM, fakeLoggerReplaceAclVM, fakeLoggerErrorDeserialisingVM, fakeMessageBoxService);
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
                fakeMacroTransformer, fakeDateTimeValidator,
                fakeEnvironmentsManager, fakeLoggerAdminVM, fakeLoggerNewBatchVM,
                 fakeLoggerAppenAclVM, fakeLoggerSetExpiryDateVM, fakeLoggerReplaceAclVM, fakeLoggerErrorDeserialisingVM, fakeMessageBoxService);
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