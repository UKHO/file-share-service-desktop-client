using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient.Modules.Auth
{
    public class AuthenticateUiModule : IModule
    {
        private readonly IRegionManager regionManager;

        public AuthenticateUiModule(IRegionManager regionManager)
        {
            this.regionManager = regionManager;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<AuthenticateView>(NavigationTargets.Authenticate);
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            regionManager.RequestNavigate(RegionNames.MainRegion, NavigationTargets.Authenticate);
        }
    }
}