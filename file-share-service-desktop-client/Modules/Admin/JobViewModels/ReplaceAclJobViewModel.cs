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

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class ReplaceAclJobViewModel : BaseBatchJobViewModel
    {
        private readonly ReplaceAclJob job;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;
        private readonly ILogger<ReplaceAclJobViewModel> logger;
        private bool isExecutingComplete;
        private string executionResult = string.Empty;

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
                var fileShareClient = fileShareClientFactory();
                var buildBatchModel = BuildBatchModel();
                var response = await fileShareClient.ReplaceAclAsync(BatchId, buildBatchModel.Acl);
                if (response == "")
                {
                    ExecutionResult = $"File Share Service replace acl completed for batch ID: {BatchId}";
                    logger.LogInformation("Execute job completed for displayName:{displayName} and batch ID:{BatchId}.", DisplayName, BatchId);
                }
                else
                {
                    ExecutionResult = response;
                }
            }

            catch (Exception e)
            {
                logger.LogError(e.Message);
                logger.LogInformation("File Share Service replace acl for batch ID:", BatchId);
                ExecutionResult = e.ToString();
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
