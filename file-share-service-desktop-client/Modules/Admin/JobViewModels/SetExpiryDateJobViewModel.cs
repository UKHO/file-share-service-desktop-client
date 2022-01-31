using Microsoft.Extensions.Logging;
using Prism.Commands;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
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
        public bool ExpiryDateKeyExists => job.ExpiryDateKeyExists;

        public string RawExpiryDate => job.ActionParams.ExpiryDate;

        private DateTime? expiryDate = null;

        public DateTime? ExpiryDate
        {
            get
            {
                if (!expiryDate.HasValue)
                {
                    expiryDate = dateTimeValidator.ValidateExpiryDate(ExpiryDateKeyExists, RFC3339_FORMATS, RawExpiryDate, job.ErrorMessages);
                }

                return expiryDate.HasValue ?
                    expiryDate.Value.ToUniversalTime() : null;
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

                var result = await fileShareClient.SetExpiryDateAsync(BatchId, batchExpiryModel, CancellationToken.None);

                if(result.IsSuccess)
                {
                    ExecutionResult = $"File Share Service set expiry date completed for batch ID: {BatchId}";
                }
                else
                {
                    ExecutionResult = (result.Errors != null && result.Errors.Any()) ? 
                        string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)) :
                        $"File Share Service set expiry date failed for batch ID:{BatchId} with status: {result.StatusCode}.";

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

        private BatchExpiryModel BuildBatchExpiryModel()
        {
            return new BatchExpiryModel
            {
                ExpiryDate = ExpiryDate
            };
        }
    }
}