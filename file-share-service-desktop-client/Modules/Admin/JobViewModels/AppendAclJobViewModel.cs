using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using System.Threading;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class AppendAclJobViewModel : BaseBatchJobViewModel
    {
        private readonly AppendAclJob job;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;
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

        protected internal override async Task OnExecuteCommand()
        {
            IsExecuting = true;
            try
            {
                logger.LogInformation("Execute job started for Action :{Action}  and displayName :{displayName} and batchId:{BatchId} .", Action, DisplayName, BatchId);
                var fileShareClient = fileShareClientFactory();
                var buildBatchModel = BuildBatchModel();                
                var result = await fileShareClient.AppendAclAsync(BatchId, buildBatchModel.Acl, CancellationToken.None);

                if (result.IsSuccess)
                {
                    ExecutionResult = $"File Share Service append Access Control List completed for batch ID: {BatchId}";
                }
                else
                {
                    ExecutionResult = (result.Errors != null && result.Errors.Any()) ? 
                        string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)) :
                        $"File Share Service append Access Control List failed for batch ID:{BatchId} with status: {result.StatusCode}.";

                    logger.LogError("File Share Service append Access Control List failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                        DisplayName, BatchId, ExecutionResult);
                }

                logger.LogInformation("Execute job completed for Action : {Action}, displayName:{DisplayName} and batch ID:{BatchId}.", 
                    Action, DisplayName, BatchId);
            }
            catch (Exception e)
            {
                ExecutionResult = e.Message;
                logger.LogError("File Share Service append Access Control List failed for batch ID:{BatchId} with error:{ExecutionResult}", 
                    BatchId, ExecutionResult);
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
    }
}