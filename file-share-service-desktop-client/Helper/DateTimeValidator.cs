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
                    return dateTime.ToUniversalTime();
                }
                //Get expand macro data
                var expandedDateTime = macroTransformer.ExpandMacros(rawDateTime);

                if (rawDateTime.Equals(expandedDateTime))
                {
                    errorMessages.Add("Expiry date is either invalid or in an invalid format.");
                    return null;
                }

                if (DateTime.TryParse(expandedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateTime))
                {
                    return dateTime.ToUniversalTime();
                }

                errorMessages.Add($"Unable to parse the date {expandedDateTime}");
                return null;
            }
            return null;
        }
    }
}
