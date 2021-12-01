using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prism.Commands;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Core.Models;
using UKHO.FileShareService.DesktopClient.Helper;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class SetExpiryDateJobViewModel : BaseBatchJobViewModel
    {
        private readonly SetExpiryDateJob job;
        private readonly ILogger<SetExpiryDateJobViewModel> logger;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;
        private readonly IDateTimeValidator dateTimeValidator;

        public SetExpiryDateJobViewModel(SetExpiryDateJob job,
            ILogger<SetExpiryDateJobViewModel> logger,
            Func<IFileShareApiAdminClient> fileShareClientFactory,
            IDateTimeValidator dateTimeValidator) : base(job, logger)
        {
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
            this.job = job;
            this.logger = logger;
            this.fileShareClientFactory = fileShareClientFactory;
            this.dateTimeValidator = dateTimeValidator;
        }

        public string BatchId => job.ActionParams.BatchId;
        public bool IsExpiryDateKeyExist => job.IsExpiryDateKeyExist;

        public string RawExpiryDate => job.ActionParams.ExpiryDate;

        private DateTime? expiryDate = null;

        public string? ExpiryDate
        {
            get
            {
                if (!expiryDate.HasValue)
                {
                    expiryDate = dateTimeValidator.ValidateExpiryDate(IsExpiryDateKeyExist, RFC3339_FORMATS, RawExpiryDate, job.ErrorMessages);
                }

                return expiryDate.HasValue ?
                    ConvertToRFC3339Format(expiryDate.Value.ToUniversalTime()) : null;
            }
        }

        protected internal override async Task OnExecuteCommand()
        {
            IsExecuting = true;

            try
            {
                logger.LogInformation("Execute job started for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.",
                                                SetExpiryDateJob.JOB_ACTION, DisplayName, BatchId);

                var fileShareClient = fileShareClientFactory();
                var batchExpiryModel = BuildBatchExpiryModel();

                var response = await fileShareClient.SetExpiryDateAsync(BatchId, batchExpiryModel, CancellationToken.None);

                if (response.IsSuccessStatusCode)
                {
                    ExecutionResult = $"Job successfully completed for batch ID: {BatchId}";
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var errorMessage = JsonConvert.DeserializeObject<ErrorDescriptionModel>(content);

                    ExecutionResult = errorMessage != null ? string.Join(Environment.NewLine, errorMessage.Errors.Select(e => e.Description)) :
                        $"Job failed for batch id: {BatchId} with {(int)response.StatusCode} - {response.ReasonPhrase}";

                    logger.LogError("File Share Service set expiry date failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                        DisplayName, BatchId, ExecutionResult);
                }

                logger.LogInformation("Execute job completed for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.",
                    SetExpiryDateJob.JOB_ACTION, DisplayName, BatchId);
            }
            catch (Exception ex)
            {
                ExecutionResult = ex.Message;
                logger.LogError("Execute job failed for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.\n Error: {Error}",
                    SetExpiryDateJob.JOB_ACTION, DisplayName, BatchId, ex.ToString());
            }
            finally
            {
                IsExecuting = false;
                IsExecutingComplete = true;
            }
        }

        public DelegateCommand CloseExecutionCommand { get; }

        private void OnCloseExecutionCommand()
        {
            ExecutionResult = string.Empty;
            IsExecutingComplete = false;
        }

        private BatchExpiryModel BuildBatchExpiryModel()
        {
            return new BatchExpiryModel
            {
                ExpiryDate = ExpiryDate
            };
        }
    }
}