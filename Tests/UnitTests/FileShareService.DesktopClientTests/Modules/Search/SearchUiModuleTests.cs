using FakeItEasy;
using NUnit.Framework;
using Prism.Ioc;
using Prism.Regions;
using UKHO.FileShareService.DesktopClient;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Search;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    public class SearchUiModuleTests
    {
        private SearchUiModule searchUiModule = null!;

        [SetUp]
        public void Setup()
        {
            searchUiModule = new SearchUiModule(A.Fake<IRegionManager>());
        }

        [Test]
        public void TestRegistersView()
        {
            var fakeContainerRegistry = A.Fake<IContainerRegistry>();
            searchUiModule.RegisterTypes(fakeContainerRegistry);

            A.CallTo(() => fakeContainerRegistry.Register(typeof(object), typeof(SearchView), NavigationTargets.Search))
                .MustHaveHappened();
        }

        [Test]
        public void TestRegistersAdminButton()
        {
            var fakeContainerRegistry = A.Fake<IContainerRegistry>();
            searchUiModule.RegisterTypes(fakeContainerRegistry);

            A.CallTo(() =>
                    fakeContainerRegistry.Register(typeof(IPageButton), typeof(SearchPageButton),
                        NavigationTargets.Search))
                .MustHaveHappened();
        }
    }
}