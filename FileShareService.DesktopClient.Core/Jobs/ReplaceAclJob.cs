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
            #region Predeserialize validations

            //Check for batchId
            if (jsonToken.SelectToken("actionParams.batchId")?.Type != JTokenType.String)
            {
                ErrorMessages.Add($"Invalid batch Id.");
            }


            //Check for read users
            if (jsonToken.SelectToken("actionParams.readUsers")?.Type != JTokenType.Array)
            {
                ErrorMessages.Add($"Invalid user groups.");
            }

            //Check for read groups
            if (jsonToken.SelectToken("actionParams.readGroups")?.Type != JTokenType.Array)
            {
                ErrorMessages.Add($"Invalid read groups.");
            }

            #endregion

            #region Post deserialize validations

            if (string.IsNullOrWhiteSpace(ActionParams.BatchId))
            {
                ErrorMessages.Add("Batch id is missing.");
            }
           
            if (!string.IsNullOrWhiteSpace(ActionParams.BatchId) && !Guid.TryParse(ActionParams.BatchId, out Guid guid))
            {
              ErrorMessages.Add("Batch id should be GUID.");
            }

            if (!ActionParams.ReadGroups.Any() && !ActionParams.ReadUsers.Any())
            {
                ErrorMessages.Add("ReadGroups/ReadUsers are not specified.");
            }

            #endregion
        }
    }
    public class ReplaceAclJobParams : Acl
    {
        public string BatchId { get; set; }
    }

}
