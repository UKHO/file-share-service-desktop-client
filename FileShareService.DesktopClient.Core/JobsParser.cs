using System;
using System.Collections.Generic;
using System.IO;
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
            new List<string>() { NewBatchJob.JOB_ACTION, AppendAclJob.JOB_ACTION, SetExpiryDateJob.JOB_ACTION };

        private List<string> jobIdCollection = new List<string>();

        public Jobs.Jobs Parse(string jobs)
        {
            try
            {
                if (string.IsNullOrEmpty(jobs))
                {
                    throw new JsonReaderException("Configuration file formatted incorrectly. Unable to find a job to process.");
                }

                var jsonSerializerSettings = new JsonSerializerSettings { DateParseHandling = DateParseHandling.None};
                jsonSerializerSettings.Converters.Add(JsonSubtypesConverterBuilder.Of<IJob>("action")
                    .RegisterSubtype<NewBatchJob>(NewBatchJob.JOB_ACTION)
                    .RegisterSubtype<AppendAclJob>(AppendAclJob.JOB_ACTION)
                    .RegisterSubtype<SetExpiryDateJob>(SetExpiryDateJob.JOB_ACTION)
                    .SerializeDiscriminatorProperty(true)
                    .Build()
                );

                JToken? jobsToken = null;

                using (var stringReader = new StringReader(jobs))
                using (var jsonTextReader = new JsonTextReader(stringReader) 
                { DateParseHandling = DateParseHandling.None})
                {
                    jobsToken = JToken.ReadFrom(jsonTextReader);
                }

                var batchJobs = jobsToken.SelectToken("jobs");

                if (batchJobs == null || batchJobs.Type != JTokenType.Array || !batchJobs.HasValues)
                {
                    throw new JsonReaderException("Configuration file formatted incorrectly. Unable to find a job to process.");
                }

                List<IJob> jobCollection = new List<IJob>();

                ErrorDeserializingJobsJob? errorJob = null;

                // Deserialize each job and validate
                foreach (JToken batchJob in batchJobs)
                {
                    string jobAction = string.Empty;

                    List<string> errors = ValidateJobActionAndDisplayName(batchJob, out jobAction);

                    if(errors.Any())
                    {
                        if(errorJob == null)
                        {
                            errorJob = new ErrorDeserializingJobsJob();
                        }
                        errorJob.ErrorMessages.AddRange(errors);

                        continue;
                    }

                    string jsonString = Convert.ToString(batchJob);

                    IJob? job = jobAction switch
                    {
                        NewBatchJob.JOB_ACTION => JsonConvert.DeserializeObject<NewBatchJob>(jsonString, jsonSerializerSettings),
                        AppendAclJob.JOB_ACTION => JsonConvert.DeserializeObject<AppendAclJob>(jsonString, jsonSerializerSettings),
                        SetExpiryDateJob.JOB_ACTION => JsonConvert.DeserializeObject<SetExpiryDateJob>(jsonString, jsonSerializerSettings),
                        _ => null
                    };


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

                jobIdCollection.Clear();

                return  new Jobs.Jobs() { jobs = jobCollection};
            }
            catch (Exception e)
            {
                return new Jobs.Jobs() { jobs = new[] { new ErrorDeserializingJobsJob(e) { ErrorMessages = new List<string>() { e.Message } } } };
            }
        }

        private List<string> ValidateJobActionAndDisplayName(JToken job, out string jobAction)
        {
            List<string> errors = new List<string>();

            //Retrieve job action
            JToken? jobActionToken = job.SelectToken("action");

            jobAction = jobActionToken?.Type == JTokenType.String ? 
                Convert.ToString(jobActionToken) : string.Empty;

            if (string.IsNullOrWhiteSpace(jobAction))
            {
                errors.Add(AddLineInfo(job, "Job action is not specified or is invalid."));
                return errors;
            }

            //Check whether job action specified in config is valid or not
            if (!ValidJobActions.Contains(jobAction))
            {
                errors.Add(AddLineInfo(job, $"Specified job action '{jobAction}' is invalid."));
            }
            
            //Retrieve display name
            JToken? displayNameTokne = job.SelectToken("displayName");
            string displayName = jobActionToken?.Type == JTokenType.String ? 
                Convert.ToString(displayNameTokne) : string.Empty; 

            if (string.IsNullOrWhiteSpace(displayName))
            {
                errors.Add(AddLineInfo(job, "Job display name is not specified or is invalid."));
                return errors;
            }

            string jobId = $"{jobAction}-{displayName.Replace(" ", string.Empty).ToLower()}";

            //Check whether the job id already exists
            if (jobIdCollection.Any(s => s.Equals(jobId)))
            {
                errors.Add(AddLineInfo(job, $"Duplicate job '{jobAction} - {displayName}' found in config file."));
            }
            //Add job-id in the collection
            jobIdCollection.Add(jobId);

            JToken? actionParamsToken = job.SelectToken("actionParams");

            //Check whether job actionParams is exist or not
            if (actionParamsToken == null || actionParamsToken?.Type != JTokenType.Object)
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