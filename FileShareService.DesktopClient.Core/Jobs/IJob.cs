using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public interface IJob
    {
        string DisplayName { get; set; }
        
        List<string> ErrorMessages { get; }

        void Validate(JToken jsonToken);
    }
}