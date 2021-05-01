using System.Linq;
using FileShareService.DesktopClientTests;
using NUnit.Framework;
using UKHO.FileShareService.DesktopClient.Core;

namespace FileShareService.DesktopClient.CoreTests
{
    public class EnvironmentLoaderTests
    {
        [Test]
        public void TestCurrentEnvironmentProperty()
        {
            var environments = new EnvironmentLoader();
            Assert.AreEqual(2, environments.Environments.Count);
            Assert.AreSame(environments.Environments[0], environments.CurrentEnvironment);

            environments.AssertPropertyChanged(nameof(environments.CurrentEnvironment),
                () => environments.CurrentEnvironment = environments.Environments[1]);
            environments.AssertPropertyChangedNotFired(nameof(environments.CurrentEnvironment),
                () => environments.CurrentEnvironment = environments.Environments[1]);
        }
    }
}