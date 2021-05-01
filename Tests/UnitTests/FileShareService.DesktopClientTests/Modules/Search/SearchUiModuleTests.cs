using FakeItEasy;
using NUnit.Framework;
using Prism.Ioc;
using Prism.Regions;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Search;

namespace FileShareService.DesktopClientTests.Modules.Search
{
    public class SearchUiModuleTests
    {
        [Test]
        public void TestRegistersView()
        {
            var searchUiModule = new SearchUiModule(A.Fake<IRegionManager>());

            var fakeContainerRegistry = A.Fake<IContainerRegistry>();
            searchUiModule.RegisterTypes(fakeContainerRegistry);

            A.CallTo(() => fakeContainerRegistry.Register(typeof(object), typeof(SearchView), NavigationTargets.Search))
                .MustHaveHappened();
        }
    }
}