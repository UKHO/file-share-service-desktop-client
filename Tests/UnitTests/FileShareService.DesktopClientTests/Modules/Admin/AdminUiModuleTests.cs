using FakeItEasy;
using NUnit.Framework;
using Prism.Ioc;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Admin;

namespace FileShareService.DesktopClientTests.Modules.Admin
{
    public class AdminUiModuleTests
    {
        private AdminUiModule adminUiModule = null!;

        [SetUp]
        public void Setup()
        {
            adminUiModule = new AdminUiModule(A.Fake<INavigation>());
        }

        [Test]
        public void TestRegistersView()
        {
            var fakeContainerRegistry = A.Fake<IContainerRegistry>();
            adminUiModule.RegisterTypes(fakeContainerRegistry);

            A.CallTo(() => fakeContainerRegistry.Register(typeof(object), typeof(AdminView), NavigationTargets.Admin))
                .MustHaveHappened();
        }

        [Test]
        public void TestRegistersAdminButton()
        {
            var fakeContainerRegistry = A.Fake<IContainerRegistry>();
            adminUiModule.RegisterTypes(fakeContainerRegistry);

            A.CallTo(() =>
                    fakeContainerRegistry.Register(typeof(IPageButton), typeof(AdminPageButton),
                        NavigationTargets.Admin))
                .MustHaveHappened();
        }
    }
}