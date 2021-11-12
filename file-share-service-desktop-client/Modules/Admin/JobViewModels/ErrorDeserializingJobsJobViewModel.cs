using System;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class ErrorDeserializingJobsJobViewModel : BaseBatchJobViewModel
    {
        private readonly ErrorDeserializingJobsJob job;

        public ErrorDeserializingJobsJobViewModel(ErrorDeserializingJobsJob job) : base(job)
        {
            this.job = job;
            _ = CanExecute();
        }

        protected internal override Task OnExecuteCommand()
        {
            throw new NotImplementedException();
        }
    }
}