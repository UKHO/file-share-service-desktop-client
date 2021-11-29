using Newtonsoft.Json.Linq;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class AppendAclJob : BindableBase, IJob
    {
        public const string JOB_ACTION = "appendAcl";
        public string DisplayName { get; set; }

        public string Action { get; set; }

        public AppendAclJobParams ActionParams { get; set; } = new AppendAclJobParams();

        public List<string> ErrorMessages { get; private set; } = new List<string>();
       

        public void Validate(JToken jsonToken)
        {
            
            if (string.IsNullOrWhiteSpace(ActionParams.BatchId))
            {
                ErrorMessages.Add("Batch id is missing.");
            }
            else if (!Guid.TryParse(ActionParams.BatchId, out _))
            {
                ErrorMessages.Add("Batch ID is not in the correct format/GUID.");
            }

            if (!ActionParams.ReadGroups.Any() && !ActionParams.ReadUsers.Any())
            {
                ErrorMessages.Add("ReadGroups/ReadUsers are not specified.");
            }          
        }
    }

    public class AppendAclJobParams : Acl
    {
        public string BatchId { get; set; }
    }
}