using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UKHO.FileShareService.DesktopClient.Core.Models;

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
                    JToken? batchKeyToken = batchAttribute.SelectToken("key");
                    string batchKey = batchKeyToken?.Type == JTokenType.String ?
                    Convert.ToString(batchKeyToken) : string.Empty;

                    if (batchAttribute.SelectToken("key")?.Type != JTokenType.String)
                    {
                        ErrorMessages.Add($"Batch attribute key is missing or is invalid for the batch.");
                    }
                    else if (string.IsNullOrWhiteSpace(batchKey))
                    {
                        ErrorMessages.Add($"Batch attribute key cannot be blank.");
                    }

                    JToken? batchValueToken = batchAttribute.SelectToken("value");
                    string batchValue = batchValueToken?.Type == JTokenType.String ?
                    Convert.ToString(batchValueToken) : string.Empty;

                    if (batchAttribute.SelectToken("value")?.Type != JTokenType.String)
                    {
                        ErrorMessages.Add($"Batch attribute value is missing or is invalid for the batch.");
                    }
                    else if (string.IsNullOrWhiteSpace(batchValue))
                    {
                        ErrorMessages.Add($"Batch attribute value cannot be blank.");
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

            JToken? expiryDateToken = jsonToken.SelectToken("actionParams.expiryDate");
            //Set value if key exists
            IsExpiryDateKeyExist = expiryDateToken != null;

            var filesToken = jsonToken.SelectToken("actionParams.files");

            if(filesToken != null)
            {
                //Check for files
                if (filesToken.Type != JTokenType.Array)
                {
                    ErrorMessages.Add($"Invalid file object.");
                }

                if (filesToken.Type == JTokenType.Array && filesToken.HasValues)
                {
                    //Check for file attributes
                    foreach (JToken fileObj in filesToken)
                    {
                        JToken? fileAttributeToken = fileObj.SelectToken("attributes");
                        var searchPath = fileObj.SelectToken("searchPath");

                        if (fileAttributeToken == null) continue;

                        if (fileAttributeToken?.Type != JTokenType.Array)
                        {
                            ErrorMessages.Add("Invalid file attribute. searchPath:" + searchPath);
                            continue;
                        }

                        if (!fileAttributeToken.HasValues) continue;

                        //Check for file attribute key and value
                        foreach (JToken? fileAttribute in fileAttributeToken)
                        {
                            JToken? fileKeyToken = fileAttribute.SelectToken("key");
                            string fileKey = fileKeyToken?.Type == JTokenType.String ?
                                Convert.ToString(fileKeyToken) : string.Empty;

                            if (fileAttribute.SelectToken("key")?.Type != JTokenType.String)
                            {
                                ErrorMessages.Add($"File attribute key is missing or is invalid for the file. searchPath:" + searchPath);
                            }
                            else if (string.IsNullOrWhiteSpace(fileKey))
                            {
                                ErrorMessages.Add($"File attribute key cannot be blank. searchPath : " + searchPath);
                            }


                            JToken? fileValueToken = fileAttribute.SelectToken("value");
                            string fileValue = fileValueToken?.Type == JTokenType.String ?
                                Convert.ToString(fileValueToken) : string.Empty;
                            if (fileAttribute.SelectToken("value")?.Type != JTokenType.String)
                            {
                                ErrorMessages.Add($"File attribute value is missing or is invalid for the file. searchPath : " + searchPath);
                            }
                            else if (string.IsNullOrWhiteSpace(fileValue))
                            {
                                ErrorMessages.Add($"File attribute value cannot be blank. searchPath : " + searchPath);
                            }
                        }
                    }
                }
            }
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

            var invalidFileCounts = ActionParams?.Files?
                .Where(file => file.ExpectedFileCount != "*")
                .Where(file => !(int.TryParse(file.ExpectedFileCount, out var fileCount) && 0 < fileCount));

            foreach (var invalidFile in invalidFileCounts ?? Enumerable.Empty<NewBatchFiles>())
            {
                ErrorMessages.Add($"Expected file count value '{invalidFile.ExpectedFileCount}' is invalid for file path '{invalidFile.SearchPath}'. '*' or positive integer value allowed");
            }

            #endregion

        }
    }

    public class NewBatchJobParams
    {
        public string BusinessUnit { get; set; } = string.Empty;

        public List<KeyValueAttribute> Attributes { get; set; } = new List<KeyValueAttribute>();

        public Acl Acl { get; set; } = new Acl();
        public string ExpiryDate { get; set; } = string.Empty;

        public List<NewBatchFiles> Files { get; set; } = new List<NewBatchFiles>();

    }

    public class NewBatchFiles
    {
        public string SearchPath { get; set; } = string.Empty;
        //ExpectedFileCount can be "*" or an integer number
        public string ExpectedFileCount { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public List<KeyValueAttribute> Attributes { get; set; } = new List<KeyValueAttribute>();
    }

    public class Acl
    {
        public List<string> ReadUsers { get; set; } = new List<string>();
        public List<string> ReadGroups { get; set; } = new List<string>();
    }
}