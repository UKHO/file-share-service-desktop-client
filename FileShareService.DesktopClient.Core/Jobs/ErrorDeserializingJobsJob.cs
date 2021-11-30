using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class ErrorDeserializingJobsJob : IJob
    {
        private const string Display_Name = "Error deserializing jobs from file:";

        public string Action { get; set; }
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
            //
            get => Display_Name;
            [ExcludeFromCodeCoverage]
            set { }
        }

        [ExcludeFromCodeCoverage]
        public void Validate(JToken jsonToken)
        {
            // Method intentionally left empty. 
        }
    }
}