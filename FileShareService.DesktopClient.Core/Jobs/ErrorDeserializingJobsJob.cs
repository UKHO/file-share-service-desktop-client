using System;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class ErrorDeserializingJobsJob : IJob
    {
        public Exception Exception { get; }

        public ErrorDeserializingJobsJob(Exception exception)
        {
            Exception = exception;
        }

        public string DisplayName
        {
            get => Exception.Message;
            [ExcludeFromCodeCoverage]
            set { }
        }
    }
}