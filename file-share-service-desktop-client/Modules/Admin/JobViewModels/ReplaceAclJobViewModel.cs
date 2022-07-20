using Microsoft.Extensions.Logging;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class ReplaceAclJobViewModel : BaseBatchJobViewModel
    {
        private readonly ReplaceAclJob job;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;
        private readonly ILogger<ReplaceAclJobViewModel> logger;

        public ReplaceAclJobViewModel(ReplaceAclJob job, Func<IFileShareApiAdminClient> fileShareClientFactory, ILogger<ReplaceAclJobViewModel> logger) : base(job, logger)
        {
            this.job = job;
            this.fileShareClientFactory = fileShareClientFactory;
            this.logger = logger;
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
        }

        public string BatchId => job.ActionParams.BatchId;
        public List<string> ReadUsers => job.ActionParams.ReadUsers;
        public List<string> ReadGroups => job.ActionParams.ReadGroups;


        protected internal override async Task OnExecuteCommand()
        {
            IsExecuting = true;
            
            try
            {
                logger.LogInformation("Execute job started for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.", ReplaceAclJob.JOB_ACTION, DisplayName, BatchId);
                var fileShareClient = fileShareClientFactory();
                var buildBatchModel = BuildBatchModel();
                var result = await fileShareClient.ReplaceAclAsync(BatchId, buildBatchModel.Acl, CancellationToken.None);

                if (result.IsSuccess)
                {
                    ExecutionResult = $"File Share Service replace Access Control List completed for batch ID: {BatchId}";
                }
                else
                {
                    ExecutionResult = (result.Errors != null && result.Errors.Any()) ? 
                        string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)) :
                        $"File Share Service replace Access Control List failed for batch ID:{BatchId} with status: {result.StatusCode}.";

                    if (result.StatusCode == (int)HttpStatusCode.Forbidden)
                    {
                        ExecutionResult += $"{Environment.NewLine}{BusinessUnitPermissionHint()}";
                    }

                    logger.LogError("File Share Service replace Access Control List failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                        DisplayName, BatchId, ExecutionResult);
                }
                logger.LogInformation("Execute job completed for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.", 
                    ReplaceAclJob.JOB_ACTION, DisplayName, BatchId);
            }
            catch (Exception e)
            {
                ExecutionResult = e.Message;
                logger.LogError("File Share Service replace Access Control List failed for batch ID:{BatchId} with error:{ExecutionResult}", BatchId,ExecutionResult);
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
