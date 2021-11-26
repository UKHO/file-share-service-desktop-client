using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class SetExpiryDateJob : IJob
    {
        public const string JOB_ACTION = "setExpiryDate";
        public string DisplayName { get; set; }

        public SetExpiryDateJobParams ActionParams { get; set; } = new SetExpiryDateJobParams();
        public List<string> ErrorMessages { get; private set; } = new List<string>();

        // To hold whether expiry date is specified as NULL in the config or not.
        public bool IsExpiryDateKeyExist { get; private set; }

        public void Validate(JToken jsonToken)
        {
            #region Predeserialize validations
            JToken? expiryDateToken = jsonToken.SelectToken("actionParams.expiryDate");

            //Set value if token exists and value is null (not empty or blank)
            IsExpiryDateKeyExist = expiryDateToken != null;

            #endregion

            #region Post deserialize validations

            if (expiryDateToken == null)
            {
                ErrorMessages.Add("Either expiry date key is missing or invalid.");
            }

            if (string.IsNullOrWhiteSpace(ActionParams.BatchId))
            {
                ErrorMessages.Add("Batch id is missing.");
            }
            else if (!Guid.TryParse(ActionParams.BatchId, out _))
            {
                ErrorMessages.Add("Batch id not in the correct format/GUID.");
            }
            #endregion
        }
    }

    public class SetExpiryDateJobParams
    {
        public string BatchId { get; set; }

        public string ExpiryDate { get; set; }
    }
}