using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class ErrorDeserializingJobsJob : IJob
    {
        public string Action { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Exception? Exception { get; } 
        public List<string> ErrorMessages { get; set; } = new List<string>();

        public ErrorDeserializingJobsJob()
        {

        }

        public ErrorDeserializingJobsJob(Exception exception)
        {
            Exception = exception;
        }

        [ExcludeFromCodeCoverage]
        public void Validate(JToken jsonToken)
        {
            // Method intentionally left empty. 
        }
    }
}