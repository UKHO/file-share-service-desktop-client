using System.Collections.Generic;
using System.Linq;

namespace UKHO.FileShareService.DesktopClient.Core
{
    public static class JobValidationErrors
    {
        public static Dictionary<string, List<string>> ValidationErrors { get; set; } 
            = new Dictionary<string, List<string>>();

        public static void AddValidationErrors(string key, List<string> values)
        {
            if (values != null && values.Any())
            {
                if (ValidationErrors.ContainsKey(key))
                {
                    ValidationErrors[key].AddRange(values);
                }
                else
                {
                    ValidationErrors.Add(key, values);
                }
            }
        }
    }
}
