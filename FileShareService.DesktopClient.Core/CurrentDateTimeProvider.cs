using System;

namespace UKHO.FileShareService.DesktopClient.Core
{
    public interface ICurrentDateTimeProvider
    {
        DateTime CurrentDateTime { get; }
    }

    public class CurrentDateTimeProvider : ICurrentDateTimeProvider
    {
        public DateTime CurrentDateTime => DateTime.UtcNow;
    }
}