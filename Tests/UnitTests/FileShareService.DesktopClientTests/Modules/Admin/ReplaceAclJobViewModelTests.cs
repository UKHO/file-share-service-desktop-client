using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.FileShareAdminClient;
using AdminClientModell = UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;
using System.Net.Http;
using UKHO.FileShareService.DesktopClient.Core.Models;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Threading;

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

            
            A.CallTo(() => fakeFileShareApiAdminClient.ReplaceAclAsync(A<string>.Ignored, A<AdminClientModell.Acl>.Ignored, CancellationToken.None))
                             .Returns(new HttpResponseMessage { StatusCode= System.Net.HttpStatusCode.NoContent });

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

            ErrorDescriptionModel content = new ErrorDescriptionModel
            {
                Errors = new List<Error> { new Error { Source = "source", Description = "Bad Request" } }
            };

            
            Assert.AreEqual("Replace Acl", vm.DisplayName);


            A.CallTo(() => fakeFileShareApiAdminClient.ReplaceAclAsync(A<string>.Ignored, A<AdminClientModell.Acl>.Ignored, CancellationToken.None))
                             .Returns(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.BadRequest, Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8,"application/json")});

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
