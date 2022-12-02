namespace UKHO.FSSDesktop.Search.Injection
{
    using Microsoft.Extensions.DependencyInjection;
    using UI.Modules;
    using Views;

    public static class InjectionExtensions
    {
        public static IServiceCollection AddSearchModule(this IServiceCollection collection)
        {
            collection.AddSingleton<IModule, SearchModule>();

            collection.AddSingleton<ChartSearch>();
            collection.AddSingleton<ChartSearchView>();

            return collection;
        }
    }
}