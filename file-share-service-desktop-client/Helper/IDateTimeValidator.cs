using System;
using System.Collections.Generic;

namespace UKHO.FileShareService.DesktopClient.Helper
{
    public interface IDateTimeValidator
    {
        DateTime? ValidateExpiryDate(bool isExpiryDateKeyExist, string[] validFormats, string rawDateTime, List<string> errorMessages);
    }
}
