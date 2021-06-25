using System;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class SetExpiryDateJob : IJob
    {
        public string DisplayName { get; set; }

        public SetExpiryDateJobParams ActionParams { get; set; } = new SetExpiryDateJobParams();
    }

    public class SetExpiryDateJobParams
    {
        public string BatchId { get; set; }

        public string ExpiryDate { get; set; }
    }
}