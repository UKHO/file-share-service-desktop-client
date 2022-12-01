using Microsoft.Extensions.DependencyInjection;
using UKHO.FSSDesktop.UI.Services.Implementation;
using UKHO.FSSDesktop.UI.Services;
using UKHO.FSSDesktop.UI.Windows;

namespace UKHO.FSSDesktop.UI.Injection
{
    public static class InjectionExtensions
    {

        public static IServiceCollection AddFrameworkUI(this IServiceCollection collection)
        {
            collection.AddSingleton<FrameworkWindow>();
            collection.AddSingleton<FrameworkWindowView>();

            collection.AddSingleton<IViewService, ViewService>();

            return collection;
        }
    }
}