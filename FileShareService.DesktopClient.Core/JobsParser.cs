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

        //To hold all the error jobs
        List<IJob>? ErrorJobs { get; }
    }

    public class JobsParser : IJobsParser
    {
        private static readonly List<string> ValidJobActions =
            new List<string>() { NewBatchJob.JOB_ACTION, AppendAclJob.JOB_ACTION,
                                 SetExpiryDateJob.JOB_ACTION, ReplaceAclJob.JOB_ACTION };

        private List<string> jobIdCollection = null!;

        //This ErrorJobs collection will be used in error job view model to display errors and add details in logger. 
        public List<IJob>? ErrorJobs { get; } = new List<IJob>();

        public Jobs.Jobs Parse(string jobs)
        {
            try
            {
                //Clear error job collection
                ErrorJobs?.Clear();

                jobIdCollection = new List<string>();

                if (string.IsNullOrEmpty(jobs))
                {
                    throw new JsonReaderException("Configuration file formatted incorrectly. Unable to find a job to process.");
                }

                var jsonSerializerSettings = new JsonSerializerSettings();
                jsonSerializerSettings.Converters.Add(JsonSubtypesConverterBuilder.Of<IJob>("action")
                    .RegisterSubtype<NewBatchJob>(NewBatchJob.JOB_ACTION)
                    .RegisterSubtype<AppendAclJob>(AppendAclJob.JOB_ACTION)
                    .RegisterSubtype<SetExpiryDateJob>(SetExpiryDateJob.JOB_ACTION)
                    .RegisterSubtype<ReplaceAclJob>(ReplaceAclJob.JOB_ACTION)
                    .SerializeDiscriminatorProperty(true)
                    .Build()
                );

                JToken? jobsToken = null;

                using (var stringReader = new StringReader(jobs))
                using (var jsonTextReader = new JsonTextReader(stringReader)
                { DateParseHandling = DateParseHandling.None })
                {
                    jobsToken = JToken.ReadFrom(jsonTextReader);

                    var batchJobs = jobsToken.SelectToken("jobs");

                    if (batchJobs == null || batchJobs.Type != JTokenType.Array || !batchJobs.HasValues)
                    {
                        throw new JsonReaderException("Configuration file formatted incorrectly. Unable to find a job to process.");
                    }

                    List<IJob> jobCollection = new List<IJob>();

                    // Deserialize each job and validate
                    foreach (JToken batchJob in batchJobs)
                    {
                        string jobAction = string.Empty;
                        string jobDispalyName = string.Empty;

                        List<string> errors = ValidateJobActionAndDisplayName(batchJob, out jobAction, out jobDispalyName);

                        string jsonString = Convert.ToString(batchJob);

                        //If any error in action, displayName and actionParams, create a job instance and add it in error job collection.
                        if (errors.Any())
                        {
                            IJob? errorJob = null;
                            try
                            {
                                errorJob = JsonConvert.DeserializeObject<IJob>(jsonString, jsonSerializerSettings);
                                errorJob?.ErrorMessages.AddRange(errors);
                            }
                            catch (Exception)
                            {
                                //When invalid action is specified in config, unable to deserilize that job. 
                                //Create a new error job instance and specify values.
                                errorJob = new ErrorDeserializingJobsJob
                                {
                                    Action = jobAction,
                                    DisplayName = jobDispalyName,
                                    ErrorMessages = errors
                                };
                            }
                            ErrorJobs?.Add(errorJob!);

                            continue;
                        }

                        IJob? job = jobAction switch
                        {
                            NewBatchJob.JOB_ACTION => JsonConvert.DeserializeObject<NewBatchJob>(jsonString, jsonSerializerSettings),
                            AppendAclJob.JOB_ACTION => JsonConvert.DeserializeObject<AppendAclJob>(jsonString, jsonSerializerSettings),
                            SetExpiryDateJob.JOB_ACTION => JsonConvert.DeserializeObject<SetExpiryDateJob>(jsonString, jsonSerializerSettings),
                            ReplaceAclJob.JOB_ACTION => JsonConvert.DeserializeObject<ReplaceAclJob>(jsonString, jsonSerializerSettings),
                            _ => null
                        };

                        if (job != null)
                        {
                            job.Validate(batchJob);
                            jobCollection.Add(job);
                        }
                    }

                    //Add error job at begining of the collection, if any error job exists.
                    if (ErrorJobs.Any())
                    {
                        jobCollection.Insert(0, new ErrorDeserializingJobsJob());
                    }

                    jobIdCollection.Clear();

                    return new Jobs.Jobs() { jobs = jobCollection };
                }
            }
            catch (Exception e)
            {
                IJob errorJob =  new ErrorDeserializingJobsJob(e) { ErrorMessages = new List<string>() { e.Message } } ;
                ErrorJobs?.Add(errorJob!);
                return new Jobs.Jobs() { jobs = ErrorJobs! };
            }
        }

        private List<string> ValidateJobActionAndDisplayName(JToken job, out string jobAction, out string jobDisplayName)
        {
            List<string> errors = new List<string>();

            //Retrieve job action
            JToken? jobActionToken = job.SelectToken("action");

            jobAction = jobActionToken?.Type == JTokenType.String ?
                Convert.ToString(jobActionToken) : string.Empty;

            //Retrieve display name
            JToken? displayNameTokne = job.SelectToken("displayName");

            jobDisplayName = displayNameTokne?.Type == JTokenType.String ?
                Convert.ToString(displayNameTokne) : string.Empty;

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

            if (string.IsNullOrWhiteSpace(jobDisplayName))
            {
                errors.Add(AddLineInfo(job, "Job display name is not specified or is invalid."));
                return errors;
            }

            string jobId = $"{jobAction}-{jobDisplayName.Replace(" ", string.Empty).ToLower()}";

            //Check whether the job id already exists
            if (jobIdCollection.Any(s => s.Equals(jobId)))
            {
                errors.Add(AddLineInfo(job, $"Duplicate job '{jobAction} - {jobDisplayName}' found in config file."));
            }
            //Add job-id in the collection
            jobIdCollection.Add(jobId);

            JToken? actionParamsToken = job.SelectToken("actionParams");

            //Check whether job actionParams is exist or not
            if (actionParamsToken == null || actionParamsToken?.Type != JTokenType.Object)
            {
                errors.Add(AddLineInfo(job, $"ActionParams attribute is invalid  or not specified for job '{jobAction} - {jobDisplayName}'."));
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