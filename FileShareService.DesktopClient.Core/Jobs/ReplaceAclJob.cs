using Prism.Mvvm;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UKHO.FileShareAdminClient.Models;
using System.Linq;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class ReplaceAclJob : BindableBase, IJob
    {
        public const string JOB_ACTION = "replaceAcl";
        public string DisplayName { get; set; }
        public ReplaceAclJobParams ActionParams { get; set; }
        public List<string> ErrorMessages { get; private set; } = new List<string>();

        public void Validate(JToken jsonToken)
        {
            if (string.IsNullOrWhiteSpace(ActionParams.BatchId))
            {
                ErrorMessages.Add("Batch ID is missing.");
            }
            else if (!Guid.TryParse(ActionParams.BatchId, out _))
            {
                ErrorMessages.Add("Batch ID is not in the correct format/GUID.");
            }

            if (!ActionParams.ReadGroups.Any() && !ActionParams.ReadUsers.Any())
            {
                ErrorMessages.Add("Either ReadUsers or ReadGroups should be specified.");
            }

        }
    }
    public class ReplaceAclJobParams : Acl
    {
        public string BatchId { get; set; }
    }

}
