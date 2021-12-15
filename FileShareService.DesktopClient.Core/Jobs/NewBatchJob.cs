using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class NewBatchJob : IJob
    {
        public const string JOB_ACTION = "newBatch";
        public string DisplayName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public NewBatchJobParams ActionParams { get; set; } = new NewBatchJobParams();
        public List<string> ErrorMessages { get; private set; } = new List<string>();
        
        // To hold whether expiry date is specified in config or not.
        public bool IsExpiryDateKeyExist { get; private set; }

        public void Validate(JToken jsonToken)
        {
            #region Predeserialize validations

            //Check for batch attributes
            JToken? batchAttributeToken = jsonToken.SelectToken("actionParams.attributes");

            if (batchAttributeToken?.Type != JTokenType.Array)
            {
                ErrorMessages.Add("Invalid batch attribute.");
            }
            else if (batchAttributeToken.HasValues)
            {
                //Check for batch attribute key and value
                foreach (var batchAttribute in batchAttributeToken)
                {
                    if (batchAttribute.SelectToken("key")?.Type != JTokenType.String)
                    {
                        ErrorMessages.Add($"Batch attribute key is missing or is invalid for the batch.");
                    }

                    if (batchAttribute.SelectToken("value")?.Type != JTokenType.String)
                    {
                        ErrorMessages.Add($"Batch attribute value is missing or is invalid for the batch.");
                    }
                }
            }

            //Check for read users
            if (jsonToken.SelectToken("actionParams.acl.readUsers")?.Type != JTokenType.Array)
            {
                ErrorMessages.Add($"Invalid user groups.");
            }

            //Check for read groups
            if (jsonToken.SelectToken("actionParams.acl.readGroups")?.Type != JTokenType.Array)
            {
                ErrorMessages.Add($"Invalid read groups.");
            }

            //Check for files
            if (jsonToken.SelectToken("actionParams.files") != null && 
                jsonToken.SelectToken("actionParams.files")?.Type != JTokenType.Array)
            {
                ErrorMessages.Add($"Invalid file object.");
            }

            JToken? expiryDateToken = jsonToken.SelectToken("actionParams.expiryDate");
            //Set value if key exists
            IsExpiryDateKeyExist = expiryDateToken != null;
            #endregion

            #region Post deserialize validations

            if (string.IsNullOrWhiteSpace(ActionParams.BusinessUnit))
            {
                ErrorMessages.Add("Business Unit is missing or is not specified.");
            }

            if (ActionParams.Files?.Count == 0)
            {
                ErrorMessages.Add("File is not specified for upload.");
            }

            #endregion

        }
    }

    public class NewBatchJobParams
    {
        public string BusinessUnit { get; set; } = string.Empty;

        public IEnumerable<KeyValuePair<string, string>> Attributes { get; set; } =
            new List<KeyValuePair<string, string>>();

        public Acl Acl { get; set; } = new Acl();
        public string ExpiryDate { get; set; } = string.Empty;

        public List<NewBatchFiles> Files { get; set; } = new List<NewBatchFiles>();
    }

    public class NewBatchFiles
    {
        public string SearchPath { get; set; } = string.Empty;
        public int ExpectedFileCount { get; set; }
        public string MimeType { get; set; } = string.Empty;
    }

    public class Acl
    {
        public List<string> ReadUsers { get; set; } = new List<string>();
        public List<string> ReadGroups { get; set; } = new List<string>();
    }
}