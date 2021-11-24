using FakeItEasy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Helper;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;

namespace FileShareService.DesktopClientTests.Modules.Admin
{
    [TestFixture]
    public class SetExpiryDateJobViewModelTests
    {
        private MockFileSystem fileSystem = null!;
        private IFileShareApiAdminClient fakeFileShareApiAdminClient = null!;
        private ICurrentDateTimeProvider fakeCurrentDateTimeProvider = null!;
        private ILogger<SetExpiryDateJobViewModel> fakeLogger = null!;
        private MacroTransformer macroTransformer = null!;
        private string expiryDateString = 
            DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        [SetUp]
        public void Setup()
        {
            fileSystem = new MockFileSystem();
            fakeFileShareApiAdminClient = A.Fake<IFileShareApiAdminClient>();
            fakeCurrentDateTimeProvider = A.Fake<ICurrentDateTimeProvider>();
            fakeLogger = A.Fake<ILogger<SetExpiryDateJobViewModel>>();
            macroTransformer = new MacroTransformer(fakeCurrentDateTimeProvider);
        }

        [Test]
        public async Task TestSetExpiryDateJobReturns204()
        {
            var vm = new SetExpiryDateJobViewModel(new SetExpiryDateJob
            {
                DisplayName = "Test - Set expiry date",
                ActionParams = new SetExpiryDateJobParams
                {
                    BatchId = "batch_id",
                    ExpiryDate = expiryDateString
                }
            }, fakeLogger, () => fakeFileShareApiAdminClient, macroTransformer);


            Assert.AreEqual("Test - Set expiry date", vm.DisplayName);
            Assert.AreEqual(expiryDateString, vm.RawExpiryDate);
            Assert.AreEqual(DateTime.Parse(expiryDateString), vm.ExpiryDate);

            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(A<string>.Ignored, A<BatchExpiryModel>.Ignored))
                .Returns(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent});

            await vm.OnExecuteCommand();
            Assert.AreEqual($"File Share Service set expiry date completed for batch ID: batch_id", vm.ExecutionResult);
        }

        [Test]
        public async Task TestExceuteReplaceAclJobReturns400()
        {
            var vm = new SetExpiryDateJobViewModel(new SetExpiryDateJob
            {
                DisplayName = "Test - Set expiry date",
                ActionParams = new SetExpiryDateJobParams
                {
                    BatchId = "batch_id",
                    ExpiryDate = expiryDateString
                }
            }, fakeLogger, () => fakeFileShareApiAdminClient, macroTransformer);
            
            ErrorDescriptionModel content = new ErrorDescriptionModel
            {
                Errors = new List<Error> { new Error { Source = "source", Description = "Bad Error" } }
            };

            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(A<string>.Ignored, A<BatchExpiryModel>.Ignored))
            .Returns(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, 
                Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json") }); 
            
            await vm.OnExecuteCommand();
            Assert.AreEqual("File Share Service replace acl completed for batch ID: 123", vm.ExecutionResult);
        }

        [Test]
        public async Task TestExceuteReplaceAclJobReturns500()
        {
            var vm = new SetExpiryDateJobViewModel(new SetExpiryDateJob
            {
                DisplayName = "Test - Set expiry date",
                ActionParams = new SetExpiryDateJobParams
                {
                    BatchId = "batch_id",
                    ExpiryDate = expiryDateString
                }, 
            }, fakeLogger, () => fakeFileShareApiAdminClient, macroTransformer);

            //ErrorDescriptionModel content = new ErrorDescriptionModel
            //{
            //    Errors = new List<Error> { new Error { Source = "source", Description = "Bad Error" } }
            //};

            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(A<string>.Ignored, A<BatchExpiryModel>.Ignored))
            .Returns(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

            await vm.OnExecuteCommand();
            Assert.AreEqual("File Share Service replace acl completed for batch ID: 123", vm.ExecutionResult);
        }
    }
}
