using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace UKHO.FileShareService.DesktopClient.Core
{
    [ExcludeFromCodeCoverage]
    public class JobValidationErrors
    {
        public const string CONFLICT_ERROR_CODE = "409";
        public const string UNKNOWN_JOB_ERROR_CODE = "-1";

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
