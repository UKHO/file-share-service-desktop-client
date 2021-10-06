using System;
using System.Collections.Generic;
using JsonSubTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Core
{
    public interface IJobsParser
    {
        Jobs.Jobs Parse(string jobs);
    }

    public class JobsParser : IJobsParser
    {
        private readonly List<string> ValidJobActions = new List<string>() { "newBatch", "appendAcl", "setExpiryDate" };
        public Jobs.Jobs Parse(string jobs)
        {
            if (string.IsNullOrEmpty(jobs))
                return new Jobs.Jobs();

            try
            {
                Validate(jobs);

                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    Error = (se, ev) =>
                    {
                        ev.ErrorContext.Handled = true;
                        JobValidationErrors.AddValidationErrors("-1", new List<string>() { ev.ErrorContext.Error.Message });
                    }
                };

                //var jsonSerializerSettings = new JsonSerializerSettings
                //{
                //    Error = (se, ev) =>
                //    {
                //        ev.ErrorContext.Handled = true;
                //    }
                //};
                //var jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.Converters.Add(JsonSubtypesConverterBuilder.Of<IJob>("action")
                    .RegisterSubtype<NewBatchJob>("newBatch")
                    .RegisterSubtype<AppendAclJob>("appendAcl")
                    .RegisterSubtype<SetExpiryDateJob>("setExpiryDate")
                    .SerializeDiscriminatorProperty(true)
                    .Build()
                );
            
                return JsonConvert.DeserializeObject<Jobs.Jobs>(jobs, jsonSerializerSettings);
            }
            catch (Exception e)
            {
                return new Jobs.Jobs() {jobs = new[] {new ErrorDeserializingJobsJob(e)}};
            }
        }

        private void Validate(string jobs)
        {
            List<string> errorMessages;

            JobValidationErrors.ValidationErrors.Clear();
            // create jObect for given json
            JObject jObject = JObject.Parse(jobs);

            JArray batchJobs = (JArray)jObject.SelectToken("jobs");
            
            if (batchJobs == null)
            {
                JobValidationErrors.AddValidationErrors("-1", new List<string>() { "Invalid format of configuration file.Unable to get a job for process."});
            }
            else
            {
                foreach (JToken job in batchJobs)
                {
                    errorMessages = new List<string>();
                    //Retrieve job action
                    JToken jobActionToken = job.SelectToken("action");

                    if (jobActionToken == null ||
                        string.IsNullOrWhiteSpace(jobActionToken.ToString()))
                    {
                        errorMessages.Add("Job action is not specified or invalid.");
                        JobValidationErrors.AddValidationErrors("-1", errorMessages);
                        continue;
                    }

                    string jobAction = $"{jobActionToken.ToString()}";

                    string displayName = string.Empty;
                    //Retrieve display name
                    JToken displayNameTokne = job.SelectToken("displayName");
                    if (displayNameTokne == null || string.IsNullOrWhiteSpace(displayNameTokne.ToString()))
                    {
                        errorMessages.Add("Job display name is not specified or invalid.");
                        JobValidationErrors.AddValidationErrors("-1", errorMessages);
                        continue;
                    }
                    else
                    {
                        displayName = displayNameTokne.ToString();
                    }

                    //Check whether job action specified in config is valid or not
                    if (!ValidJobActions.Contains(jobAction))
                    {
                        errorMessages.Add($"Specified job action '{jobAction}' is invalid.");
                        JobValidationErrors.AddValidationErrors($"-1", errorMessages);
                        continue;
                    }

                    string jobId = $"{jobAction}-{displayName.Replace(" ",string.Empty).ToLower()}";
                    //Check whether the job id already exists
                    if(JobValidationErrors.ValidationErrors.ContainsKey(jobId))
                    {
                        errorMessages.Add($"Specified job '{jobAction} - {displayName}' already exists.");
                        JobValidationErrors.AddValidationErrors($"-1", errorMessages);
                        continue;
                    }
                    
                    //Check whether job actionParams is exist or not
                    if (job.SelectToken("actionParams") == null)
                    {
                        errorMessages.Add($"ActionParams attribute is invalid  or not specified for job '{jobAction} - {displayName}'.");
                        JobValidationErrors.AddValidationErrors(jobId, errorMessages);
                        continue;
                    }
                   
                    if (jobAction == "newBatch")
                    {
                        JToken batchAttributeToken = job.SelectToken("actionParams.attributes");
                        if (batchAttributeToken?.Type != JTokenType.Array)
                        {
                            errorMessages.Add("Invalid batch attributes.");
                        }
                        else if(batchAttributeToken.HasValues)
                        {
                            int counter = 1;
                            foreach(var batchAttribute in batchAttributeToken)
                            {
                                if(batchAttribute.SelectToken("key")?.Type != JTokenType.String)
                                {
                                    errorMessages.Add($"Either batch attribute key is missing or invalid for batch attribute - {counter}.");
                                }

                                if (batchAttribute.SelectToken("value")?.Type != JTokenType.String)
                                {
                                    errorMessages.Add($"Either batch attribute value is missing or invalid for batch attribute - {counter}.");
                                }
                            }
                        }

                        if (job.SelectToken("actionParams.acl.readUsers")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid user groups.");
                        }

                        if (job.SelectToken("actionParams.acl.readGroups")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid read groups.");
                        }

                        if (job.SelectToken("actionParams.files")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid files object.");
                        }                 
                    }

                    else if(jobAction == "appendAcl")
                    {
                        if(job.SelectToken("actionParams.readUsers")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid user group.");
                        }

                        if (job.SelectToken("actionParams.readGroups")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid read groups.");
                        }
                    }

                    else if (jobAction == "setExpiryDate")
                    {
                        //Do validations
                    }

                    JobValidationErrors.AddValidationErrors(jobId, errorMessages);
                }
            }
        }
    }
}