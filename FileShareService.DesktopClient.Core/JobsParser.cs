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
        private static readonly List<string> ValidJobActions =
            new List<string>() { "newBatch", "appendAcl", "setExpiryDate", "replaceAcl" };

        private List<string> jobIdCollection;

        public Jobs.Jobs Parse(string jobs)
        {
            if (string.IsNullOrEmpty(jobs))
                return new Jobs.Jobs();

            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.Converters.Add(JsonSubtypesConverterBuilder.Of<IJob>("action")
                .RegisterSubtype<NewBatchJob>("newBatch")
                .RegisterSubtype<AppendAclJob>("appendAcl")
                .RegisterSubtype<SetExpiryDateJob>("setExpiryDate")
                .RegisterSubtype<ReplaceAclJob>("replaceAcl")
                .SerializeDiscriminatorProperty(true)
                .Build()
            );
            try
            {
                JToken jobsToken = JToken.Parse(jobs);

                var batchJobs = jobsToken.SelectToken("jobs");

                if (batchJobs == null || batchJobs.Type != JTokenType.Array || !batchJobs.HasValues)
                {
                    throw new JsonReaderException("Configuration file formatted incorrectly. Unable to find a job to process.");
                }

                List<IJob> jobCollection = new List<IJob>();

                jobIdCollection = new List<string>();

                ErrorDeserializingJobsJob? errorJob = null;

                // Deserialize each job and validate
                foreach (JToken batchJob in batchJobs)
                {
                    string jobAction = string.Empty;

                    List<string> errors = ValidateJobActionAndDisplayName(batchJob, out jobAction);

                    if (errors.Any())
                    {
                        if (errorJob == null)
                        {
                            errorJob = new ErrorDeserializingJobsJob();
                        }
                        errorJob.ErrorMessages.AddRange(errors);

                        continue;
                    }

                    IJob? job = null;
                    string jsonString = Convert.ToString(batchJob);

                    switch (jobAction)
                    {
                        ////case NewBatchJob.JobAction:
                        ////    job = JsonConvert.DeserializeObject<NewBatchJob>(jsonString, jsonSerializerSettings);
                        ////    break;

                        ////case AppendAclJob.JobAction:
                        ////    job = JsonConvert.DeserializeObject<AppendAclJob>(jsonString, jsonSerializerSettings);
                        ////    break;

                        ////case SetExpiryDateJob.JobAction:
                        ////    job = JsonConvert.DeserializeObject<SetExpiryDateJob>(jsonString, jsonSerializerSettings);
                        ////    break;

                        case ReplaceAclJob.JobAction:
                            job = JsonConvert.DeserializeObject<ReplaceAclJob>(jsonString, jsonSerializerSettings);
                            break;
                    }

                    if (job != null)
                    {
                        job.Validate(batchJob);
                        jobCollection.Add(job);
                    }
                }

                //Add error job at begining of the collection, if exists.
                if (errorJob != null)
                {
                    jobCollection.Insert(0, errorJob);
                }

                return new Jobs.Jobs() { jobs = jobCollection };
            }
            catch (Exception e)
            {
                return new Jobs.Jobs() { jobs = new[] { new ErrorDeserializingJobsJob(e) { ErrorMessages = new List<string>() { e.Message } } } };
            }
        }

        private List<string> ValidateJobActionAndDisplayName(JToken job, out string jobAction)
        {
            jobAction = string.Empty;

            List<string> errors = new List<string>();

            //Retrieve job action
            JToken? jobActionToken = job.SelectToken("action");

            if (jobActionToken == null ||
                string.IsNullOrWhiteSpace(Convert.ToString(jobActionToken)))
            {
                errors.Add(AddLineInfo(job, "Job action is not specified or is invalid."));
                return errors;
            }

            jobAction = $"{Convert.ToString(jobActionToken)}";

            //Check whether job action specified in config is valid or not
            if (!ValidJobActions.Contains(jobAction))
            {
                errors.Add(AddLineInfo(job, $"Specified job action '{jobAction}' is invalid."));
                return errors;
            }

            string displayName = string.Empty;
            //Retrieve display name
            JToken? displayNameToken = job.SelectToken("displayName");
            if (displayNameToken == null || string.IsNullOrWhiteSpace(Convert.ToString(displayNameToken)))
            {
                errors.Add(AddLineInfo(job, "Job display name is not specified or is invalid."));
                return errors;
            }

            displayName = displayNameToken.ToString();

            string jobId = $"{jobAction}-{displayName.Replace(" ", string.Empty).ToLower()}";

            //Check whether the job id already exists
            if (jobIdCollection.Any(s => s.Equals(jobId)))
            {
                errors.Add(AddLineInfo(job, $"Duplicate job '{jobAction} - {displayName}' found in config file."));
                return errors;
            }
            //Add job-id in the collection
            jobIdCollection.Add(jobId);

            //Check whether job actionParams is exist or not
            if (job.SelectToken("actionParams") == null)
            {
                errors.Add(AddLineInfo(job, $"ActionParams attribute is invalid  or not specified for job '{jobAction} - {displayName}'."));
            }

            return errors;
        }

        private string AddLineInfo(JToken job, string errorMessage)
        {
            var lineInfo = job as IJsonLineInfo;

            if (lineInfo.HasLineInfo())
            {
                errorMessage = $"{errorMessage} Line: {lineInfo.LineNumber}, Position: {lineInfo.LinePosition}.";
            }

            return errorMessage;
        }
    }
}