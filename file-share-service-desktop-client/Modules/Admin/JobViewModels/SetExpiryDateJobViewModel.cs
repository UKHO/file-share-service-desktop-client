using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prism.Commands;
using System;
using System.Globalization;
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
        private readonly MacroTransformer macroTransformer;

        public SetExpiryDateJobViewModel(SetExpiryDateJob job,
            ILogger<SetExpiryDateJobViewModel> logger,
            Func<IFileShareApiAdminClient> fileShareClientFactory,
            MacroTransformer macroTransformer) : base(job)
        {
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
            this.job = job;
            this.logger = logger;
            this.fileShareClientFactory = fileShareClientFactory;
            this.macroTransformer = macroTransformer;
        }
        
        public string BatchId => job.ActionParams.BatchId;
        public bool IsExpiryDateKeyExist => job.IsExpiryDateKeyExist;

        public string RawExpiryDate
        {
            get => job.ActionParams.ExpiryDate;
            set {  }
        }

        public DateTime? ExpiryDate { get; private set; }

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

                if(response.IsSuccessStatusCode)
                {
                    ExecutionResult = $"Job successfully completed for batch ID: {BatchId}";
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var errorMessage = JsonConvert.DeserializeObject<ErrorDescriptionModel>(content);

                    ExecutionResult = errorMessage != null ? string.Join(Environment.NewLine, errorMessage.Errors.Select(e => e.Description)) :
                        $"Job failed for batch id: {BatchId} with {(int)response.StatusCode} - {response.ReasonPhrase}";

                    logger.LogError("File Share Service set expiry date job failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.", 
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

        protected override bool CanExecute()
        {
            ValidationErrors.Clear();
            
            //Validate view model
            ValidateViewModel();

            ValidationErrors = job.ErrorMessages;

            for (int i = 0; i < ValidationErrors.Count; i++)
            {
                logger.LogError("Configuration Error : {ValidationErrors}  for Action : {Action}, displayName:{displayName} and BatchId: {BatchId}. ", 
                    ValidationErrors[i].ToString(), SetExpiryDateJob.JOB_ACTION, DisplayName, BatchId);
            }

            return !ValidationErrors.Any();
        }

        private void ValidateViewModel()
        {
            if(IsExpiryDateKeyExist && RawExpiryDate is not null)
            {
                DateTime dateTime;
                //Parse if date is valid RFC 3339 format
                if (DateTime.TryParseExact(RawExpiryDate, RFC3339_FORMATS, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                {
                    ExpiryDate = dateTime;
                }
                else
                {
                    //Get expand macro data
                    var expandedDateTime = macroTransformer.ExpandMacros(RawExpiryDate);

                    if(RawExpiryDate.Equals(expandedDateTime))
                    {
                        job.ErrorMessages.Add("Expiry date is either invalid or in an invalid format.");
                    }
                    else
                    {
                        if (DateTime.TryParse(expandedDateTime,CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateTime))
                        {
                            ExpiryDate = dateTime;
                        }
                        else
                        {
                            job.ErrorMessages.Add($"Unable to parse the date {expandedDateTime}");
                        }
                    }
                }
            }
            else
            {
                RawExpiryDate = RawExpiryDate is null ? "null" : RawExpiryDate;
                ExpiryDate = null;
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
                ExpiryDate = RawExpiryDate
            };
        }
    }
}