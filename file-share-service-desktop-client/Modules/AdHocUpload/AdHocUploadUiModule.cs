using Prism.Ioc;
using Prism.Modularity;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient.Modules.AdHocUpload
{
    internal class AdHocUploadUiModule : IModule
    {
        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<AdHocUploadView>(NavigationTargets.AdHocUpload);
            containerRegistry.Register<IPageButton, AdHocUploadPageButton>(NavigationTargets.AdHocUpload);
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
        }
    }
}