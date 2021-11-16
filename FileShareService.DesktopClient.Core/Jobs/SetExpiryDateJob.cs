
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class SetExpiryDateJob : IJob
    {
        public const string JOB_ACTION = "setExpiryDate";
        public string DisplayName { get; set; }

        public SetExpiryDateJobParams ActionParams { get; set; } = new SetExpiryDateJobParams();
        public List<string> ErrorMessages { get; private set; } = new List<string>();

        public void Validate(JToken jsonToken)
        {
            //This validation will be implemented when PBI SetExpiryDateJob is picked-up.
        }
    }

    public class SetExpiryDateJobParams
    {
        public string BatchId { get; set; }

        public string ExpiryDate { get; set; }
    }
}