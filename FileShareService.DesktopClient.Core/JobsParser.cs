using System;
using JsonSubTypes;
using Newtonsoft.Json;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Core
{
    public interface IJobsParser
    {
        Jobs.Jobs Parse(string jobs);
    }

    public class JobsParser : IJobsParser
    {
        public Jobs.Jobs Parse(string jobs)
        {
            if (string.IsNullOrEmpty(jobs))
                return new Jobs.Jobs();
            var jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.Converters.Add(JsonSubtypesConverterBuilder.Of<IJob>("action")
                .RegisterSubtype<NewBatchJob>("newBatch")
                .RegisterSubtype<AppendAclJob>("appendAcl")
                .RegisterSubtype<SetExpiryDateJob>("setExpiryDate")
                .SerializeDiscriminatorProperty(true)
                .Build()
            );
            try
            {
                return JsonConvert.DeserializeObject<Jobs.Jobs>(jobs, jsonSerializerSettings);
            }
            catch (Exception e)
            {
                return new Jobs.Jobs() {jobs = new[] {new ErrorDeserializingJobsJob(e)}};
            }
        }
    }
}