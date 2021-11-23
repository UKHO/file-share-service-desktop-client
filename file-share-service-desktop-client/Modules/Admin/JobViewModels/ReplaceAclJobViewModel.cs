using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Core.Models;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class ReplaceAclJobViewModel : BaseBatchJobViewModel
    {
        private readonly ReplaceAclJob job;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;
        private readonly ILogger<ReplaceAclJobViewModel> logger;
        private bool isExecutingComplete;
        private string executionResult = string.Empty;
        private string responseMessage = string.Empty;

        public ReplaceAclJobViewModel(ReplaceAclJob job, Func<IFileShareApiAdminClient> fileShareClientFactory, ILogger<ReplaceAclJobViewModel> logger) : base(job)
        {
            this.job = job;
            this.fileShareClientFactory = fileShareClientFactory;
            this.logger = logger;
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
        }

        public string BatchId => job.ActionParams.BatchId;
        public List<string> ReadUsers => (List<string>)job.ActionParams.ReadUsers;
        public List<string> ReadGroups => (List<string>)job.ActionParams.ReadGroups;


        protected internal override async Task OnExecuteCommand()
        {
            IsExecuting = true;
        
            try
            {
                logger.LogInformation("Execute job started for displayName :{displayName} .", DisplayName);
                var fileShareClient = fileShareClientFactory();
                var buildBatchModel = BuildBatchModel();
                var response = await fileShareClient.ReplaceAclAsync(BatchId, buildBatchModel.Acl);
                if(response.StatusCode != HttpStatusCode.NoContent)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    var errorMessage = JsonConvert.DeserializeObject<ErrorDescriptionModel>(content);
                    responseMessage = string.Join(Environment.NewLine, errorMessage.Errors.Select(e => e.Description)) ;
                    logger.LogInformation("File Share Service replace acl in-complete for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.", DisplayName, BatchId, responseMessage);
                }
                ExecutionResult = response.StatusCode != HttpStatusCode.NoContent
                        ? responseMessage!
                        : $"File Share Service replace acl completed for batch ID: {BatchId}";

                logger.LogInformation("Execute job completed for displayName:{DisplayName} and batch ID:{BatchId}.", DisplayName, BatchId);
            }
            catch (Exception e)
            {
                ExecutionResult = e.ToString();
                logger.LogError(e.Message);
                logger.LogInformation("File Share Service replace acl for batch ID:{BatchId} and error:{ExecutionResult}", BatchId,ExecutionResult);
                throw;
            }
            finally
            {
                IsExecuting = false;
                IsExecutingComplete = true;
            }

        }

        public BatchModel BuildBatchModel()
        {
            return new()
            {
                Acl = new FileShareAdminClient.Models.Acl
                {
                    ReadGroups = ReadGroups,
                    ReadUsers = ReadUsers
                },
            };
        }


        protected override bool CanExecute()
        {
            ValidationErrors.Clear();
            ValidationErrors = job.ErrorMessages;
            return !ValidationErrors.Any();
        }


        public bool IsExecutingComplete
        {
            get => isExecutingComplete;
            set
            {
                if (isExecutingComplete != value)
                {
                    isExecutingComplete = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ExecutionResult
        {
            get => executionResult;
            set
            {
                if (executionResult != value)
                {
                    executionResult = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DelegateCommand CloseExecutionCommand { get; }

        private void OnCloseExecutionCommand()
        {
            ExecutionResult = string.Empty;
            IsExecutingComplete = false;
        }
    }
}
