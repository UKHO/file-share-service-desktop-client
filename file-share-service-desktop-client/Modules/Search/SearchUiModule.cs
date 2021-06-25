using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    public class SearchUiModule : IModule
    {
        private readonly IRegionManager regionManager;

        public SearchUiModule(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<SearchView>(NavigationTargets.Search);
            containerRegistry.Register<IPageButton, SearchPageButton>(NavigationTargets.Search);
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // regionManager.RequestNavigate(RegionNames.MainRegion, NavigationTargets.Search);
        }
    }
}