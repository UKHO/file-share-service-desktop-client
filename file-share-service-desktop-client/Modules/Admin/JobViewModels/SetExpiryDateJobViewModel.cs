using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                var fileShareClient = fileShareClientFactory();
                var batchExpiryModel = BuildBatchExpiryModel();

                var response = await fileShareClient.SetExpiryDateAsync(BatchId, batchExpiryModel);

                if(response.IsSuccessStatusCode)
                {
                    ExecutionResult = $"File Share Service set expiry date completed for batch ID: {BatchId}";
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var errorMessage = string.IsNullOrWhiteSpace(content) ? new ErrorDescriptionModel() :
                        JsonConvert.DeserializeObject<ErrorDescriptionModel>(content);
                    ExecutionResult = string.Join(Environment.NewLine, errorMessage.Errors.Select(e => e.Description));
                }
            }
            catch(Exception ex)
            {
                ExecutionResult = ex.ToString();
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
                        job.ErrorMessages.Add("Expiry date is either invalid or in an invalid format - the valid formats are 'RFC3339 formats'.");
                    }
                    else
                    {
                        if (DateTime.TryParse(expandedDateTime,CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateTime))
                        {
                            ExpiryDate = dateTime;
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

    public class ErrorDescriptionModel
    {
        public IEnumerable<Error> Errors { get; set; } = new List<Error>();
    }

    public class Error
    {
        public string Source { get; set; }
        public string Description { get; set; }
    }
}