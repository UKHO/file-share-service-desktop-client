using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class SetExpiryDateJobViewModel : BaseBatchJobViewModel
    {
        private readonly SetExpiryDateJob job;
        private readonly ILogger<SetExpiryDateJobViewModel> logger;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;

        public SetExpiryDateJobViewModel(SetExpiryDateJob job,
            ILogger<SetExpiryDateJobViewModel> logger,
            Func<IFileShareApiAdminClient> fileShareClientFactory) : base(job)
        {
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
            this.job = job;
            this.logger = logger;
            this.fileShareClientFactory = fileShareClientFactory;
        }
        
        public string BatchId => job.ActionParams.BatchId;
        public string ExpiryDate => job.ActionParams.ExpiryDate;
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
                    var errorMessage = JsonConvert.DeserializeObject<ErrorDescriptionModel>(content);
                    ExecutionResult = string.Join(Environment.NewLine, errorMessage?.Errors.Select(e => e.Description));
                }
            }
            catch(Exception ex)
            {
                ExecutionResult = ex.ToString();
            }
            finally
            {
                IsCommitting = false;
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

    public class ErrorDescriptionModel
    {
        public IEnumerable<Error> Errors { get; set; }
    }

    public class ErrorDescription
    {
        public string CorrelationId { get; set; }
        public List<Error> Errors { get; set; }
    }
    public class Error
    {
        public string Source { get; set; }
        public string Description { get; set; }
    }
}