using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class NewBatchWithUserInputJob : IJob
    {
        public static string JOB_ACTION;
        public string DisplayName { get; set; }
        public string Action { get; set; }
        public List<string> ErrorMessages { get; set; }
        public void Validate(JToken jsonToken)
        {
            throw new NotImplementedException();
        }
    }
}
