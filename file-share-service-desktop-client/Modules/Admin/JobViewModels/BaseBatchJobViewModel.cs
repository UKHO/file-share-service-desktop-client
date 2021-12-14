using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public abstract class BaseBatchJobViewModel : BindableBase, IBatchJobViewModel
    {
        private readonly IJob job;
        private bool isExecuting;
        private readonly ILogger<BaseBatchJobViewModel> logger;

        protected BaseBatchJobViewModel(IJob job, ILogger<BaseBatchJobViewModel> logger)
        {
            this.job = job;
            this.logger = logger;
            ExcecuteJobCommand = new DelegateCommand(async () => await OnExecuteCommand(), () => !IsExecuting && CanExecute());
        }

        public DelegateCommand ExcecuteJobCommand { get; }
        protected internal abstract Task OnExecuteCommand();

        protected virtual bool CanExecute()
        {
            ValidationErrors.Clear();

            ValidationErrors = job.ErrorMessages;
            for (int i = 0; i < ValidationErrors.Count; i++)
            {
                logger.LogError("Configuration Error : {ValidationErrors} for Action : {Action}, displayName:{displayName}. ", ValidationErrors[i].ToString(), Action, DisplayName);
            }

            return !ValidationErrors.Any();

        }

        public bool IsExecuting
        {
            get => isExecuting;
            set
            {
                if (isExecuting != value)
                {
                    isExecuting = value;
                    RaisePropertyChanged();
                    ExcecuteJobCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string DisplayName => job.DisplayName;

        public string Action => job.Action;

        public List<string> ValidationErrors { get; set; } = new List<string>();

        public bool IsVisibleValidationErrorsArea => ValidationErrors.Any();
    }
}