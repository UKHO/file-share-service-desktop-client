﻿using System;
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
            PopulateValidationErrors(JobValidationErrors.UNKNOWN_JOB_ERROR_CODE);
        }

        protected internal override Task OnExecuteCommand()
        {
            throw new NotImplementedException();
        }

        protected override bool CanExecute()
        {
            return false;
        }
    }
}