using System;
using System.Collections.Generic;
using System.Globalization;
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
        private string executionResult = string.Empty;
        private bool isExecutingComplete;
        private bool isCommitting;
        private readonly ILogger<BaseBatchJobViewModel> logger;

        protected readonly string[] RFC3339_FORMATS = new string[]
        {
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyy-MM-ddTHH:mm:ss.fK",
            "yyyy-MM-ddTHH:mm:ss.ffK",
            "yyyy-MM-ddTHH:mm:ss.fffK"
        };

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

        public virtual bool IsExecutingComplete
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

        public virtual bool IsCommitting
        {
            get => isCommitting;
            set
            {
                if (isCommitting != value)
                {
                    isCommitting = value;
                    RaisePropertyChanged();
                }
            }
        }

        public virtual string ExecutionResult
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

        public DelegateCommand? CloseExecutionCommand { get; set; }

        public virtual void OnCloseExecutionCommand()
        {
            ExecutionResult = string.Empty;
            IsExecutingComplete = false;
        }

        public string DisplayName => job.DisplayName;

        public string Action => job.Action;

        public List<string> ValidationErrors { get; set; } = new List<string>();
       
        public bool IsVisibleValidationErrorsArea => ValidationErrors.Any();

        protected string ConvertToRFC3339Format(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        protected string BusinessUnitPermissionHint(string businessUnit) =>
            $"Please ensure you have permission to manage batches for the '{businessUnit}' business unit";

        protected string BusinessUnitPermissionHint() =>
            $"Please ensure you have permission to manage batches for the business unit";
    }
}