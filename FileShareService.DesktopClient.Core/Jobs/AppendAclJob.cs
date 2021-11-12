using Newtonsoft.Json.Linq;
using Prism.Mvvm;
using System.Collections.Generic;
using System.Linq;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class AppendAclJob : BindableBase, IJob
    {
        public const string JOB_ACTION = "appendAcl";
        public string DisplayName { get; set; }

        public AppendAclJobParams ActionParams { get; set; } = new AppendAclJobParams();

        public List<string> ErrorMessages { get; private set; } = new List<string>();

        public void Validate(JToken jsonToken)
        {
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
        }
    }

    public class AppendAclJobParams : Acl
    {
        public string BatchId { get; set; }
    }
}