using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class SetExpiryDateJob : IJob
    {
        public const string JobAction = "setExpiryDate";
        public string DisplayName { get; set; }

        public SetExpiryDateJobParams ActionParams { get; set; } = new SetExpiryDateJobParams();
        public List<string> ErrorMessages { get ; set ; }

        public List<string> Validate(JToken jsonToken)
        {
            throw new NotImplementedException();
        }
    }

    public class SetExpiryDateJobParams
    {
        public string BatchId { get; set; }

        public string ExpiryDate { get; set; }
    }
}