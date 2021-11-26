using System.Collections.Generic;

namespace UKHO.FileShareService.DesktopClient.Core.Models
{
    public class ErrorDescriptionModel
    {
        public IEnumerable<Error> Errors { get; set; } = new List<Error>();
    }

    public class Error
    {
        public string Source { get; set; }
        public string Description { get; set; }
    }
}
