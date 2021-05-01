using Prism.Regions;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient
{
    public class NavigationManager : INavigation
    {
        private readonly IRegionManager regionManager;

        public NavigationManager(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
        }

        public void RequestNavigate(string viewName)
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, viewName);
        }
    }
}