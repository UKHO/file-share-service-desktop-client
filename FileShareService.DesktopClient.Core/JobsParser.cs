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

            var batchJobs = jobsToken.SelectToken("jobs");

            if(batchJobs == null || batchJobs.Type != JTokenType.Array || !batchJobs.HasValues)
            {
               JobValidationErrors.AddValidationErrors(JobValidationErrors.UNKNOWN_JOB_ERROR_CODE, 
                   new List<string>() { "Configuration file formatted incorrectly. Unable to find a job to process." });
                return;
            }

            foreach (JToken job in batchJobs)
            {
                errorMessages = new List<string>();

                //Retrieve job action
                JToken? jobActionToken = job.SelectToken("action");

                if (jobActionToken == null ||
                    string.IsNullOrWhiteSpace(Convert.ToString(jobActionToken)))
                {
                    AddValidationMessage(job, "Job action is not specified or is invalid.");
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
                    AddValidationMessage(job, "Job display name is not specified or is invalid.", JobValidationErrors.CONFLICT_ERROR_CODE);
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

                switch (jobAction) 
                {
                    case "newBatch":
                        //Check for batch attributes
                        JToken? batchAttributeToken = job.SelectToken("actionParams.attributes");

                        if (batchAttributeToken?.Type != JTokenType.Array)
                        {
                            errorMessages.Add("Invalid batch attribute.");
                        }
                        else if (batchAttributeToken.HasValues)
                        {
                            //Check for batch attribute key and value
                            foreach (var batchAttribute in batchAttributeToken)
                            {
                                if (batchAttribute.SelectToken("key")?.Type != JTokenType.String)
                                {
                                    errorMessages.Add($"Batch attribute key is missing or is invalid for the batch.");
                                }

                                if (batchAttribute.SelectToken("value")?.Type != JTokenType.String)
                                {
                                    errorMessages.Add($"Batch attribute value is missing or is invalid for the batch.");
                                }
                            }
                        }

                        //Check for read users
                        if (job.SelectToken("actionParams.acl.readUsers")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid user groups.");
                        }

                        //Check for read groups
                        if (job.SelectToken("actionParams.acl.readGroups")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid read groups.");
                        }

                        //Check for files
                        if (job.SelectToken("actionParams.files")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid file object.");
                        }

                        break;

                    case "appendAcl":

                        if (job.SelectToken("actionParams.readUsers")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid user groups.");
                        }

                        if (job.SelectToken("actionParams.readGroups")?.Type != JTokenType.Array)
                        {
                            errorMessages.Add($"Invalid read groups.");
                        }
                        break;
                }
                JobValidationErrors.AddValidationErrors(jobId, errorMessages);
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