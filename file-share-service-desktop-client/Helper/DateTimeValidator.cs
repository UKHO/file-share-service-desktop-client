using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace UKHO.FileShareService.DesktopClient.Helper
{
    public class DateTimeValidator : IDateTimeValidator
    {
        private readonly IMacroTransformer macroTransformer;

        public DateTimeValidator(IMacroTransformer macroTransformer)
        {
            this.macroTransformer = macroTransformer;
        }

        public DateTime? ValidateExpiryDate(bool isExpiryDateKeyExist, string[] validFormats, string rawDateTime, List<string> errorMessages)
        {
            if (isExpiryDateKeyExist && rawDateTime is not null)
            {
                //This check is for empty string or white space.                
                if(rawDateTime.All(char.IsWhiteSpace))
                {
                    errorMessages.Add("The expiry date is missing.");
                    return null;
                }

                DateTime dateTime;
                //Parse if date is valid RFC 3339 format
                if (DateTime.TryParseExact(rawDateTime, validFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                {
                    return dateTime;
                }
                //Get expand macro data
                var expandedDateTime = macroTransformer.ExpandMacros(rawDateTime);

                //This check is for any invalid format is passed to macro.
                if (!rawDateTime.Equals(expandedDateTime) && 
                    DateTime.TryParse(expandedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateTime))
                {
                    return dateTime;
                }

                errorMessages.Add("The expiry date format is invalid.");
                return null;
            }
            return null;
        }
    }
}
