using FakeItEasy;
using NUnit.Framework;
using Prism.Regions;
using UKHO.FileShareService.DesktopClient;

namespace FileShareService.DesktopClientTests
{
    public class NavigationManagerTests
    {
        [Test]
        public void TestNavigate()
        {
            var fakeRegionManager = A.Fake<IRegionManager>();
            new NavigationManager(fakeRegionManager).RequestNavigate("AView");

            A.CallTo(() => fakeRegionManager.RequestNavigate(RegionNames.MainRegion, "AView")).MustHaveHappened();
        }
    }
}