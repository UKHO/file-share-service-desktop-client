using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly List<string> ValidJobActions = 
            new List<string>() { "newBatch", "appendAcl", "setExpiryDate" };
        

        public Jobs.Jobs Parse(string jobs)
        {
            if (string.IsNullOrEmpty(jobs))
                return new Jobs.Jobs();

            try
            {
                Validate(jobs);

                if(JobValidationErrors.ValidationErrors.ContainsKey(JobValidationErrors.CONFLICT_ERROR_CODE))
                {
                    string errorMessage = string.Join(Environment.NewLine, JobValidationErrors.ValidationErrors[JobValidationErrors.CONFLICT_ERROR_CODE]);
                    throw new JsonException(errorMessage);
                }

                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    Error = (se, ev) =>
                    {
                        ev.ErrorContext.Handled = true;
                        //JobValidationErrors.AddValidationErrors(UNKNOWN_JOB_ERROR_CODE, new List<string>() { ev.ErrorContext.Error.Message });
                    }
                };

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
                JobValidationErrors.AddValidationErrors(JobValidationErrors.UNKNOWN_JOB_ERROR_CODE, new List<string>() { e.Message });
                return new Jobs.Jobs() {jobs = new[] {new ErrorDeserializingJobsJob(e)}};
            }
        }

        private void Validate(string jobs)
        {
            List<string> errorMessages;
            List<string> jobIdCollection = new List<string>();

            JobValidationErrors.ValidationErrors.Clear();

            JToken jobsToken = JToken.Parse(jobs);

            JArray? batchJobs = (JArray?)jobsToken?.SelectToken("jobs");

            if (batchJobs == null || !batchJobs.HasValues)
            {
                JobValidationErrors.AddValidationErrors(JobValidationErrors.UNKNOWN_JOB_ERROR_CODE, new List<string>() { "Invalid format of configuration file. Unable to get a job for process."});
            }
            else
            {
                foreach (JToken job in batchJobs)
                {
                    errorMessages = new List<string>();

                    //Retrieve job action
                    JToken? jobActionToken = job.SelectToken("action");

                    if (jobActionToken == null ||
                        string.IsNullOrWhiteSpace(Convert.ToString(jobActionToken)))
                    {
                        AddValidationMessage(job, "Job action is not specified or invalid.");
                        continue;
                    }

                    string jobAction = $"{Convert.ToString(jobActionToken)}";

                    //Check whether job action specified in config is valid or not
                    if (!ValidJobActions.Contains(jobAction))
                    {
                        AddValidationMessage(job, $"Specified job action '{jobAction}' is invalid.");
                        continue;
                    }

                    string displayName = string.Empty;
                    //Retrieve display name
                    JToken? displayNameTokne = job.SelectToken("displayName");
                    if (displayNameTokne == null || string.IsNullOrWhiteSpace(Convert.ToString(displayNameTokne)))
                    {
                        AddValidationMessage(job, "Job display name is not specified or invalid.");
                        continue;
                    }

                    displayName = displayNameTokne.ToString();

                    string jobId = $"{jobAction}-{displayName.Replace(" ",string.Empty).ToLower()}";

                    //Check whether the job id already exists
                    if(jobIdCollection.Any(s => s.Equals(jobId)))
                    {
                        AddValidationMessage(job, $"Duplicate job '{jobAction} - {displayName}' found in config file.", JobValidationErrors.CONFLICT_ERROR_CODE);
                        continue;
                    }

                    //Add job-id in the collection
                    jobIdCollection.Add(jobId);
                    
                    //Check whether job actionParams is exist or not
                    if (job.SelectToken("actionParams") == null)
                    {
                        errorMessages.Add($"ActionParams attribute is invalid  or not specified for job '{jobAction} - {displayName}'.");
                        JobValidationErrors.AddValidationErrors(jobId, errorMessages);
                        continue;
                    }
                   
                    if (jobAction == "newBatch")
                    {
                        JToken? batchAttributeToken = job.SelectToken("actionParams.attributes");

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

        private void AddValidationMessage(JToken job, string errorMessage, string jobId= JobValidationErrors.UNKNOWN_JOB_ERROR_CODE)
        {
            var lineInfo = job as IJsonLineInfo;

            if(lineInfo.HasLineInfo())
            {
                errorMessage = $"{errorMessage} Line: {lineInfo.LineNumber}, Position: {lineInfo.LinePosition}.";
            }
            JobValidationErrors.AddValidationErrors(jobId, new List<string>() { errorMessage });
        }
    }
}