using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class ErrorDeserializingJobsJob : IJob
    {
        public Exception Exception { get; }

        public List<string> ErrorMessages { get; set; }

        public ErrorDeserializingJobsJob(Exception exception)
        {
            Exception = exception;
        }

        public ErrorDeserializingJobsJob(List<string> errors)
        {
            ErrorMessages = errors;
        }

        public string DisplayName
        {
            get => Exception.Message;
            [ExcludeFromCodeCoverage]
            set { }
        }
    }
}