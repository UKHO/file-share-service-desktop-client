using NUnit.Framework;
using UKHO.FileShareService.DesktopClient;

namespace FileShareService.DesktopClientTests
{
    public class VersionProviderTests
    {
        [Test]
        public void TestGetVersion()
        {
            StringAssert.IsMatch(@"\d{1,5}\.\d{1,5}\.\d{1,5}\.\d{1,5}", new VersionProvider().Version);
        }
    }
}