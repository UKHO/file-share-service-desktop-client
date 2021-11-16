using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class NewBatchJob : IJob
    {
        public string DisplayName { get; set; }

        public NewBatchJobParams ActionParams { get; set; } = new NewBatchJobParams();
        public List<string> ErrorMessages { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public List<string> Validate(JToken jsonToken)
        {
            throw new System.NotImplementedException();
        }
    }

    public class NewBatchJobParams
    {
        public string BusinessUnit { get; set; }

        public IEnumerable<KeyValuePair<string, string>> Attributes { get; set; } =
            new List<KeyValuePair<string, string>>();

        public Acl Acl { get; set; } = new Acl();
        public string ExpiryDate { get; set; }

        public List<NewBatchFiles> Files { get; set; } = new List<NewBatchFiles>();
    }

    public class NewBatchFiles
    {
        public string SearchPath { get; set; }
        public int ExpectedFileCount { get; set; }
        public string MimeType { get; set; }
    }

    public class Acl
    {
        public List<string> ReadUsers { get; set; } = new List<string>();
        public List<string> ReadGroups { get; set; } = new List<string>();
    }
}