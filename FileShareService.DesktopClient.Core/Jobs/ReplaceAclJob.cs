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
        public const string JobAction = "replaceAcl";
        public string DisplayName { get; set; }
        public ReplaceAclJobParams ActionParams { get; set; }
        public List<string> ErrorMessages { get; set; }
        public List<string> Validate(JToken jsonToken)
        {
            ErrorMessages = new List<string>();

            #region Predeserialize validations

            if (jsonToken.SelectToken("actionParams.readUsers")?.Type != JTokenType.Array)
            {
                ErrorMessages.Add("Invalid user groups.");
            }

            if (jsonToken.SelectToken("actionParams.readGroups")?.Type != JTokenType.Array)
            {
                ErrorMessages.Add("Invalid read groups.");
            }

            #endregion

            #region Post deserialize validations

            if (string.IsNullOrWhiteSpace(ActionParams.BatchId))
            {
                ErrorMessages.Add("Batch id is missing.");
            }

            if (!ActionParams.ReadGroups.Any() && !ActionParams.ReadUsers.Any())
            {
                ErrorMessages.Add("ReadGroups/ReadUsers are not specified.");
            }

            #endregion

            return ErrorMessages;
        }
    }
    public class ReplaceAclJobParams : Acl
    {
        public string BatchId { get; set; }
    }

}
