using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class ErrorDeserializingJobsJob : IJob
    {
        public Exception Exception { get; }

        public List<string> ErrorMessages { get; set; } = new List<string>();

        public ErrorDeserializingJobsJob()
        {

        }

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

        public List<string> Validate(JToken jsonToken)
        {
            return new List<string>();
        }
    }
}