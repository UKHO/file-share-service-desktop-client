using FakeItEasy;
using NUnit.Framework;
using Prism.Ioc;
using Prism.Regions;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Auth;

namespace FileShareService.DesktopClientTests.Modules.Auth
{
    public class AuthenticateUiModuleTests
    {
        [Test]
        public void TestAuthenticateUiModuleRegistersView()
        {
            var containerRegistry = A.Fake<IContainerRegistry>();
            new AuthenticateUiModule(A.Fake<IRegionManager>()).RegisterTypes(containerRegistry);

            A.CallTo(() => containerRegistry.Register(typeof(object), typeof(AuthenticateView),
                NavigationTargets.Authenticate
            )).MustHaveHappened();
        }
    }
}