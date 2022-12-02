namespace UKHO.FSSDesktop.Import.Injection
{
    using Microsoft.Extensions.DependencyInjection;
    using UI.Modules;
    using Views;

    public static class InjectionExtensions
    {
        public static IServiceCollection AddImportModule(this IServiceCollection collection)
        {
            collection.AddSingleton<IModule, ImportModule>();

            collection.AddSingleton<ChartImport>();
            collection.AddSingleton<ChartImportView>();

            return collection;
        }
    }
}