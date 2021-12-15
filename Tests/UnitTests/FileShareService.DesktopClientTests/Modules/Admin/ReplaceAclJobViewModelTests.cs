using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.FileShareAdminClient;
using AdminClientModell = UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;
using System;
using System.Threading;
using UKHO.FileShareAdminClient.Models.Response;

namespace FileShareService.DesktopClientTests.Modules.Admin
{
    [TestFixture]
    public class ReplaceAclJobViewModelTests
    {
        private IFileShareApiAdminClient fakeFileShareApiAdminClient = null!;
        private ILogger<ReplaceAclJobViewModel> fakeLoggerReplaceAclJobVM = null!;


        [SetUp]
        public void Setup()
        {
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            fakeLoggerReplaceAclJobVM = A.Fake<ILogger<ReplaceAclJobViewModel>>();
        }

        [Test]
        public async Task TestExceuteReplaceAclJobReturns204()
        {
            var vm = new ReplaceAclJobViewModel(new ReplaceAclJob
            {
                DisplayName = "Replace Acl",
                ActionParams = new ReplaceAclJobParams
                {
                   BatchId = Guid.NewGuid().ToString(),
                   ReadGroups = new List<string> { "ReplaceTest"},
                   ReadUsers = new List<string> { "public"}
                }
            },
            () => fakeFileShareApiAdminClient,
            fakeLoggerReplaceAclJobVM);

            Assert.AreEqual("Replace Acl", vm.DisplayName);

            Result<ReplaceAclResponse> result = new Result<ReplaceAclResponse>
            {
                IsSuccess = true,
                StatusCode = 204
            };

            A.CallTo(() => fakeFileShareApiAdminClient.ReplaceAclAsync(A<string>.Ignored, A<AdminClientModell.Acl>.Ignored, CancellationToken.None))
                             .Returns(result);

            await vm.OnExecuteCommand();
            Assert.AreEqual($"File Share Service replace Access Control List completed for batch ID: {vm.BatchId}", vm.ExecutionResult);
        }
   
        [Test]
        public async Task TestExceuteReplaceAclJobReturns400()
        {
            var vm = new ReplaceAclJobViewModel(new ReplaceAclJob
            {
                DisplayName = "Replace Acl",
                ActionParams = new ReplaceAclJobParams
                {
                    BatchId = Guid.NewGuid().ToString(),
                    ReadGroups = new List<string> { "ReplaceTest" },
                    ReadUsers = new List<string> { "public" }
                }
            },
            () => fakeFileShareApiAdminClient,
            fakeLoggerReplaceAclJobVM);

            Result<ReplaceAclResponse> result = new Result<ReplaceAclResponse>
            {
                IsSuccess = false,
                StatusCode = 400,
                Errors = new List<Error> { new Error { Source = "source", Description = "Bad Request" } }
            };

            Assert.AreEqual("Replace Acl", vm.DisplayName);

            A.CallTo(() => fakeFileShareApiAdminClient.ReplaceAclAsync(A<string>.Ignored, A<AdminClientModell.Acl>.Ignored, CancellationToken.None))
                             .Returns(result);

            await vm.OnExecuteCommand();
            Assert.AreEqual("Bad Request", vm.ExecutionResult);
        }

        [Test]
        public void TestExceuteReplaceAclJobHasValidationErrors()
        {
            var replaceAclJob = A.Fake<ReplaceAclJob>();
            replaceAclJob.ErrorMessages.Add("Test validation error message.");
            replaceAclJob.DisplayName = "Test - Replace Acl";
            replaceAclJob.ActionParams = new ReplaceAclJobParams
            {
                BatchId = "batch_id",
            };

            var vm = new ReplaceAclJobViewModel(replaceAclJob,
                                                  () => fakeFileShareApiAdminClient,
                                                    fakeLoggerReplaceAclJobVM);

            Assert.AreEqual("Test - Replace Acl", vm.DisplayName);
            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());
            StringAssert.StartsWith("Test validation error", vm.ValidationErrors[0]);
        }
    }
}
