using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class SetExpiryDateJob : IJob
    {
        public string DisplayName { get; set; }

        public SetExpiryDateJobParams ActionParams { get; set; } = new SetExpiryDateJobParams();
        public List<string> ErrorMessages { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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