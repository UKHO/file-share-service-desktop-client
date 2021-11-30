using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class ErrorDeserializingJobsJobViewModel : BaseBatchJobViewModel
    {
        private readonly ErrorDeserializingJobsJob job;
        private readonly ILogger<ErrorDeserializingJobsJobViewModel> ELogger;

        public ErrorDeserializingJobsJobViewModel(ErrorDeserializingJobsJob job, ILogger<ErrorDeserializingJobsJobViewModel> ELogger) : base(job, ELogger)
        {
            this.job = job;
            this.ELogger = ELogger;
            _ = CanExecute();
        }

        protected internal override Task OnExecuteCommand()
        {
            throw new NotImplementedException();
        }
    }
}