using FakeItEasy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Core.Models;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;
using AdminClientModell = UKHO.FileShareAdminClient.Models;

namespace FileShareService.DesktopClientTests.Modules.Admin
{
    [TestFixture]
    public class AppendAclJobViewModelTest
    {
        private IFileShareApiAdminClient fakeFileShareApiAdminClient = null!;
        private ILogger<AppendAclJobViewModel> fakeLoggerAppendAclJobVM = null!;


        [SetUp]
        public void Setup()
        {          
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            fakeLoggerAppendAclJobVM = A.Fake<ILogger<AppendAclJobViewModel>>();
        }

   
        [Test]
        public async Task TestExceuteReplaceAclJobReturns204()
        {
            var vm = new AppendAclJobViewModel(() => fakeFileShareApiAdminClient, new AppendAclJob
            {
                DisplayName = "Append Acl",
                ActionParams = new AppendAclJobParams
                {
                    BatchId = "Batch_Id",
                    ReadGroups = new List<string> { "AppendAclTest" },
                    ReadUsers = new List<string> { "public" }
                }
            },
            
            fakeLoggerAppendAclJobVM); 

            Assert.AreEqual("Append Acl", vm.DisplayName);
            A.CallTo(() => fakeFileShareApiAdminClient.AppendAclAsync(A<string>.Ignored, A<AdminClientModell.Acl>.Ignored, CancellationToken.None))
            .Returns(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.NoContent }); 
            await vm.OnExecuteCommand();
            Assert.AreEqual("File Share Service append Access Control List completed for batch ID: Batch_Id", vm.ExecutionResult);
        }
        [Test]
        public async Task TestExceuteReplaceAclJobReturns400()
        {
            var vm = new AppendAclJobViewModel(() => fakeFileShareApiAdminClient, new AppendAclJob
            {
                DisplayName = "Append Acl",
                ActionParams = new AppendAclJobParams
                {
                    BatchId = "Batch_Id",
                    ReadGroups = new List<string> { "AppendAclTest" },
                    ReadUsers = new List<string> { "public" }
                }
            },         
            fakeLoggerAppendAclJobVM); 
            
            ErrorDescriptionModel content = new ErrorDescriptionModel
            {
                Errors = new List<Error> { new Error { Source = "source", Description = "Bad Request" } }
            };
            Assert.AreEqual("Append Acl", vm.DisplayName);
          
            A.CallTo(() => fakeFileShareApiAdminClient.AppendAclAsync(A<string>.Ignored, A<AdminClientModell.Acl>.Ignored,CancellationToken.None ))
            .Returns(new HttpResponseMessage { StatusCode = System.Net.HttpStatusCode.BadRequest, Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json") }); 
            await vm.OnExecuteCommand();
           
            Assert.AreEqual("Bad Request", vm.ExecutionResult);
        }

        [Test]
        public void TestCannotExecuteCommand()
        {
            var AppendAclJob = A.Fake<AppendAclJob>();
            AppendAclJob.ErrorMessages.Add("Test validation error message.");
            AppendAclJob.DisplayName = "Test - Append Acl";
            AppendAclJob.ActionParams = new AppendAclJobParams
            {
                BatchId = "Batch_Id",
                ReadGroups = new List<string> { "AppendAclTest" },
                ReadUsers = new List<string> { "public" }
            };

            var vm = new AppendAclJobViewModel(() => fakeFileShareApiAdminClient, 
               AppendAclJob,fakeLoggerAppendAclJobVM);

            Assert.AreEqual("Test - Append Acl", vm.DisplayName);

            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());

            StringAssert.StartsWith("Test validation error", vm.ValidationErrors[0]);
        }
        
        }
}
