namespace UKHO.FSSDesktop.Modules.Injection
{
    using ESS;
    using ESS.Security;
    using FSS;
    using FSS.Import;
    using FSS.Search;
    using FSS.Security;
    using Microsoft.Extensions.DependencyInjection;
    using UI.Modules;

    public static class InjectionExtensions
    {
        public static IServiceCollection AddModules(this IServiceCollection collection)
        {
            collection.AddSingleton<IModule, FSSModule>();
            collection.AddSingleton<IModule, ESSModule>();

            collection.AddSingleton<ChartSearch>();
            collection.AddSingleton<ChartSearchView>();
            collection.AddSingleton<ChartImport>();
            collection.AddSingleton<ChartImportView>();
            collection.AddSingleton<ChartSecurity>();
            collection.AddSingleton<ChartSecurityView>();

            collection.AddSingleton<ESSChartSecurity>();
            collection.AddSingleton<ESSChartSecurityView>();

            return collection;
        }
    }
}