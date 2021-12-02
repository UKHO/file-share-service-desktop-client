using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class ErrorDeserializingJobsJobViewModel : BaseBatchJobViewModel
    {
        private readonly ErrorDeserializingJobsJob job;
        private readonly ILogger<ErrorDeserializingJobsJobViewModel> logger;
        private readonly List<IJob>? jobs;

        public ErrorDeserializingJobsJobViewModel(List<IJob>? jobs, ILogger<ErrorDeserializingJobsJobViewModel> logger) 
            : base(new ErrorDeserializingJobsJob(), logger)
        {
            this.jobs = jobs;
            this.logger = logger;
            _ = CanExecute();
        }

        protected internal override Task OnExecuteCommand()
        {
            throw new NotImplementedException();
        }

        protected override bool CanExecute()
        {
            ValidationErrors.Clear();

            foreach(var errorJob in jobs!)
            {
                ValidationErrors.AddRange(errorJob.ErrorMessages);

                //For logger.
                foreach(string message in errorJob.ErrorMessages)
                {
                    logger.LogError("Configuration Error : {ValidationErrors} for Action : {Action}, displayName:{displayName}. ",
                    message, errorJob.Action, errorJob.DisplayName);
                }                
            }

            return !ValidationErrors.Any();
        }
    }
}