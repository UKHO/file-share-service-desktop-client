using Microsoft.Extensions.DependencyInjection;
using UKHO.FSSDesktop.UI.Services.Implementation;
using UKHO.FSSDesktop.UI.Services;
using UKHO.FSSDesktop.UI.Windows;

namespace UKHO.FSSDesktop.UI.Injection
{
    using Navigation;
    using Views;

    public static class InjectionExtensions
    {

        public static IServiceCollection AddFrameworkUI(this IServiceCollection collection)
        {
            collection.AddSingleton<IViewService, ViewService>();

            collection.AddSingleton<FrameworkWindow>();
            collection.AddSingleton<FrameworkWindowView>();

            collection.AddSingleton<IFrameworkViewProvider, FrameworkViewProvider>();

            collection.AddSingleton<FrameworkError>();
            collection.AddSingleton<FrameworkErrorView>();
            collection.AddSingleton<FrameworkLogin>();
            collection.AddSingleton<FrameworkLoginView>();
            collection.AddSingleton<FrameworkNavigation>();
            collection.AddSingleton<FrameworkNavigationView>();
            collection.AddSingleton<FrameworkUnauthorized>();
            collection.AddSingleton<FrameworkUnauthorizedView>();
            collection.AddSingleton<FrameworkSettings>();
            collection.AddSingleton<FrameworkSettingsView>();

            return collection;
        }
    }
}