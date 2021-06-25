using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JsonSubTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
            return JsonConvert.DeserializeObject<Jobs.Jobs>(jobs,
                jsonSerializerSettings);
        }
    }

    public class JobsTypeNameHandler : DefaultSerializationBinder
    {
        private readonly Dictionary<string, Type> nameToType;
        private readonly Dictionary<Type, string> typeToName;

        public JobsTypeNameHandler()
        {
            var customDisplayNameTypes =
                GetType()
                    .Assembly
                    //concat with references if desired
                    .GetTypes()
                    .Where(x => x.Namespace == typeof(IJob).Namespace &&
                                x
                                    .GetCustomAttributes(false)
                                    .Any(y => y is DisplayNameAttribute));

            nameToType = customDisplayNameTypes.ToDictionary(
                t => t.GetCustomAttributes(false).OfType<DisplayNameAttribute>().First().DisplayName,
                t => t);

            typeToName = nameToType.ToDictionary(
                t => t.Value,
                t => t.Key);
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (false == typeToName.ContainsKey(serializedType))
            {
                base.BindToName(serializedType, out assemblyName, out typeName);
                return;
            }

            var name = typeToName[serializedType];

            assemblyName = null;
            typeName = name;
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            if (nameToType.ContainsKey(typeName))
                return nameToType[typeName];

            return base.BindToType(assemblyName, typeName);
        }
    }
}