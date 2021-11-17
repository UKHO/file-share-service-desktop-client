using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public abstract class BaseBatchJobViewModel : BindableBase, IBatchJobViewModel
    {
        private readonly IJob job;
        private bool isExecuting;

        protected BaseBatchJobViewModel(IJob job)
        {
            this.job = job;
            ExcecuteJobCommand = new DelegateCommand(async () => await OnExecuteCommand(), () => !IsExecuting && CanExecute());
        }

        public DelegateCommand ExcecuteJobCommand { get; }
        protected internal abstract Task OnExecuteCommand();

        protected virtual bool CanExecute()
        {
            ValidationErrors.Clear();

            ValidationErrors = job.ErrorMessages;

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

        public List<string> ValidationErrors { get; set; } = new List<string>();

        public bool IsVisibleValidationErrorsArea
        {
            get
            {
                return ValidationErrors.Any();
            }
        }
    }
}