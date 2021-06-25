using Prism.Ioc;
using Prism.Modularity;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin
{
    public class AdminUiModule : IModule
    {
        private readonly INavigation navigation;

        public AdminUiModule(INavigation navigation)
        {
            this.navigation = navigation;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<AdminView>(NavigationTargets.Admin);
            containerRegistry.Register<IPageButton, AdminPageButton>(NavigationTargets.Admin);
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        //    navigation.RequestNavigate(NavigationTargets.Admin);
        }
    }
}