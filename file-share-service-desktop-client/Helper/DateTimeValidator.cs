using System;
using System.Collections.Generic;
using System.Globalization;

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
                DateTime dateTime;
                //Parse if date is valid RFC 3339 format
                if (DateTime.TryParseExact(rawDateTime, validFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime))
                {
                    return dateTime;
                }
                //Get expand macro data
                var expandedDateTime = macroTransformer.ExpandMacros(rawDateTime);

                //This check is for any invalid format is passed to macro.
                if (rawDateTime.Equals(expandedDateTime))
                {
                    errorMessages.Add("Either expiry date is invalid or invalid format.");
                    return null;
                }

                if (DateTime.TryParse(expandedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateTime))
                {
                    return dateTime;
                }

                errorMessages.Add($"Unable to parse the date {expandedDateTime}");
                return null;
            }
            return null;
        }
    }
}
