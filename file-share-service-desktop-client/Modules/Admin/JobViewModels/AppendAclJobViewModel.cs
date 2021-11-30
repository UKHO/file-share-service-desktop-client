using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using System;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using System.Threading;
using UKHO.FileShareService.DesktopClient.Core.Models;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class AppendAclJobViewModel : BaseBatchJobViewModel
    {
        private readonly AppendAclJob job;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;
        private string executionResult = string.Empty;
        string responseMessage = string.Empty;
        private bool isExecutingComplete;
        private readonly ILogger<AppendAclJobViewModel> logger;

        public AppendAclJobViewModel(AppendAclJob job, Func<IFileShareApiAdminClient> fileShareClientFactory, ILogger<AppendAclJobViewModel> logger) : base(job, logger)
        {
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
            this.job = job;
            this.fileShareClientFactory = fileShareClientFactory;
            this.logger = logger;
        }

        public string BatchId => job.ActionParams.BatchId;
        public List<string> ReadUsers => job.ActionParams.ReadUsers;
        public List<string> ReadGroups => job.ActionParams.ReadGroups;

        public DelegateCommand CloseExecutionCommand { get; }

        private void OnCloseExecutionCommand()
        {
            ExecutionResult = string.Empty;
            IsExecutingComplete = false;
        }
        protected internal override async Task OnExecuteCommand()
        {
            IsExecuting = true;
            try
            {
                logger.LogInformation("Execute job started for Action :{Action}  and displayName :{displayName} and batchId:{BatchId} .", Action, DisplayName, BatchId);
                var fileShareClient = fileShareClientFactory();
                var buildBatchModel = BuildBatchModel();                
                var response = await fileShareClient.AppendAclAsync(BatchId, buildBatchModel.Acl, CancellationToken.None);

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        responseMessage = $"File Share Service append Access Control List failed for batch ID:{BatchId} with status: {(int)response.StatusCode} {response.StatusCode}.";
                    }
                    else
                    {
                        var errorMessage = JsonConvert.DeserializeObject<ErrorDescriptionModel>(content);
                        responseMessage = string.Join(Environment.NewLine, errorMessage.Errors.Select(e => e.Description));
                    }
                    logger.LogError("File Share Service append acl failed for Action:{Action}, displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.", Action, DisplayName, BatchId, responseMessage);
                }
                ExecutionResult = response.StatusCode != HttpStatusCode.NoContent
                       ? responseMessage!
                       : $"File Share Service append Access Control List completed for batch ID: {BatchId}";

                logger.LogInformation("Execute job completed for Action : {Action}, displayName:{DisplayName} and batch ID:{BatchId}.", Action, DisplayName, BatchId);
            }
            catch (Exception e)
            {
                ExecutionResult = e.Message;
                logger.LogError("File Share Service append Access Control List failed for batch ID:{BatchId} with error:{ExecutionResult}", BatchId, ExecutionResult);
                logger.LogError(e.ToString());
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
    }
}