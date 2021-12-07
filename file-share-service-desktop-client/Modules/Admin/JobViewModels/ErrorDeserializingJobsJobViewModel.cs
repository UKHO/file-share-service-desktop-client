using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class ErrorDeserializingJobsJobViewModel : BaseBatchJobViewModel
    {
        private readonly ErrorDeserializingJobsJob job;
        private readonly ILogger<ErrorDeserializingJobsJobViewModel> eLogger;
        public ErrorDeserializingJobsJobViewModel(ErrorDeserializingJobsJob job, ILogger<ErrorDeserializingJobsJobViewModel> eLogger) : base(job, eLogger)
        {
            this.job = job;
            this.eLogger = eLogger;
            _ = CanExecute();
        }

        protected internal override Task OnExecuteCommand()
        {
            throw new NotImplementedException();
        }
    }
}