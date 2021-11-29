using FakeItEasy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Core.Models;
using UKHO.FileShareService.DesktopClient.Helper;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;

namespace FileShareService.DesktopClientTests.Modules.Admin
{
    [TestFixture]
    public class SetExpiryDateJobViewModelTests
    {
        private IFileShareApiAdminClient fakeFileShareApiAdminClient = null!;
        private ICurrentDateTimeProvider fakeCurrentDateTimeProvider = null!;
        private ILogger<SetExpiryDateJobViewModel> fakeLogger = null!;
        private MacroTransformer macroTransformer = null!;
        private readonly string expiryDateString =
            DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        [SetUp]
        public void Setup()
        {
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

            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(A<string>.Ignored, A<BatchExpiryModel>.Ignored, A<CancellationToken>.Ignored))
                .Returns(new HttpResponseMessage { StatusCode = HttpStatusCode.NoContent });

            await vm.OnExecuteCommand();
            StringAssert.StartsWith("Job successfully completed for batch ID:", vm.ExecutionResult);
        }

        [Test]
        public async Task TestSetExpiryDateJobReturns400()
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

            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(A<string>.Ignored, A<BatchExpiryModel>.Ignored, A<CancellationToken>.Ignored))
            .Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json")
            });

            await vm.OnExecuteCommand();
            Assert.AreEqual("Bad Error", vm.ExecutionResult);
        }

        [Test]
        public async Task TestSetExpiryDateJobReturns500WithNoContent()
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

            A.CallTo(() => fakeFileShareApiAdminClient.SetExpiryDateAsync(A<string>.Ignored, A<BatchExpiryModel>.Ignored, A<CancellationToken>.Ignored))
            .Returns(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError });

            await vm.OnExecuteCommand();
            Assert.AreEqual("Job failed for batch id: batch_id with 500 - Internal Server Error", vm.ExecutionResult);
        }

        [Test]
        public void TestSetExpiryDateJobHasValidationErrors()
        {
            var setBatchExpiryJob = A.Fake<SetExpiryDateJob>();
            setBatchExpiryJob.ErrorMessages.Add("Test validation error message.");
            setBatchExpiryJob.DisplayName = "Test - Set expiry date";
            setBatchExpiryJob.ActionParams = new SetExpiryDateJobParams
            {
                BatchId = "batch_id",
                ExpiryDate = expiryDateString
            };

            var vm = new SetExpiryDateJobViewModel(setBatchExpiryJob, fakeLogger, () => fakeFileShareApiAdminClient, macroTransformer);

            Assert.AreEqual("Test - Set expiry date", vm.DisplayName);

            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute());

            StringAssert.StartsWith("Test validation error", vm.ValidationErrors[0]);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("10/10/2022")]
        [TestCase("2022-01-10")]
        [TestCase("2022-01-1T10:00:00Z")]
        [TestCase("2022-01-01T10:70:00Z")]
        [TestCase("now.AddDays(28)")]
        [TestCase("(now.AddDays(2))")]
        [TestCase("#(now.AddDays(2))")]
        [TestCase("$(now.AddDaysAndMonths(2))")]
        [TestCase("$(now.AddDays(xyz))")]
        [TestCase("$(now.AddDays(10x))")]
        [TestCase("$(now.AddDays(1).AddMonths(1))")]
        public void TestSetExpiryDateJobHasInvalidExpiryDateFormat(string invalidExpiryDate)
        {
            var setBatchExpiryJob = new SetExpiryDateJob();
            setBatchExpiryJob.DisplayName = "Test - Set expiry date";
            setBatchExpiryJob.ActionParams = new SetExpiryDateJobParams
            {
                BatchId = "batch_id",
                ExpiryDate = invalidExpiryDate
            };
            typeof(SetExpiryDateJob).GetProperty(nameof(setBatchExpiryJob.IsExpiryDateKeyExist))?.SetValue(setBatchExpiryJob, true, null);

            var vm = new SetExpiryDateJobViewModel(setBatchExpiryJob, fakeLogger, () => fakeFileShareApiAdminClient, macroTransformer);

            Assert.AreEqual("Test - Set expiry date", vm.DisplayName);
            Assert.IsFalse(vm.ExcecuteJobCommand.CanExecute(), $"Expected validation error message for format { invalidExpiryDate}, but no validation error message.");
            StringAssert.StartsWith("Expiry date is either invalid or in an invalid format", vm.ValidationErrors[0], $"Expected error message for format {invalidExpiryDate}, but no validation message.");
            Assert.IsNull(vm.ExpiryDate);
        }

        [TestCase(null)]
        [TestCase("2022-02-14T10:00:00")]
        [TestCase("2022-02-14T10:00:00Z")]
        [TestCase("2022-02-14T10:00:00+05:30")]
        [TestCase("2022-02-14T10:00:00-02:00")]
        [TestCase("$(now.AddDays(-1))")]
        public void TestSetExpiryDateJobHasValidExpiryDateFormat(string validExpiryDate)
        {
            var setBatchExpiryJob = new SetExpiryDateJob();
            setBatchExpiryJob.DisplayName = "Test - Set expiry date";
            setBatchExpiryJob.ActionParams = new SetExpiryDateJobParams
            {
                BatchId = "batch_id",
                ExpiryDate = validExpiryDate
            };

            typeof(SetExpiryDateJob).GetProperty(nameof(setBatchExpiryJob.IsExpiryDateKeyExist))?.SetValue(setBatchExpiryJob, true, null);

            A.CallTo(() => fakeCurrentDateTimeProvider.CurrentDateTime)
                .Returns(DateTime.UtcNow);

            var vm = new SetExpiryDateJobViewModel(setBatchExpiryJob, fakeLogger, () => fakeFileShareApiAdminClient, macroTransformer);

            Assert.AreEqual("Test - Set expiry date", vm.DisplayName);
            Assert.IsTrue(vm.ExcecuteJobCommand.CanExecute(), $"Expected no validation error message for format {validExpiryDate}, but validation error message is generated.");
            Assert.AreEqual(0, vm.ValidationErrors.Count, $"Expected no error message for format {validExpiryDate}, but validation error message is generated.");
            Assert.IsTrue((string.IsNullOrEmpty(validExpiryDate) && !vm.ExpiryDate.HasValue) || (vm.ExpiryDate.HasValue));
        }
    }
}
