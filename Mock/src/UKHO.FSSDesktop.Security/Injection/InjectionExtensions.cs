namespace UKHO.FSSDesktop.Security.Injection
{
    using Microsoft.Extensions.DependencyInjection;
    using UI.Modules;
    using Views;

    public static class InjectionExtensions
    {
        public static IServiceCollection AddSecurityModule(this IServiceCollection collection)
        {
            collection.AddSingleton<IModule, SecurityModule>();

            collection.AddSingleton<ChartSecurity>();
            collection.AddSingleton<ChartSecurityView>();

            return collection;
        }
    }
}