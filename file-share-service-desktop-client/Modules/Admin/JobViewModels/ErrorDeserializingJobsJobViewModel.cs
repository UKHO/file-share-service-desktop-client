using System;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class ErrorDeserializingJobsJobViewModel : BaseBatchJobViewModel
    {
        private readonly ErrorDeserializingJobsJob job;

        public ErrorDeserializingJobsJobViewModel(ErrorDeserializingJobsJob job) : base(job)
        {
            this.job = job;
        }

        protected internal override Task OnExecuteCommand()
        {
            throw new NotImplementedException();
        }

        protected override bool CanExecute()
        {
            ValidationErrors.Clear();

            if (JobValidationErrors.ValidationErrors.ContainsKey("-1"))
            {
                ValidationErrors.AddRange(
                    JobValidationErrors.ValidationErrors["-1"]);
            }

            return ValidationErrors.Count == 0;
        }

        public string ErrorDetails => job.Exception.ToString();
    }
}