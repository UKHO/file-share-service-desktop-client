namespace UKHO.FSSDesktop.Import.Injection
{
    using Microsoft.Extensions.DependencyInjection;

    public static class InjectionExtensions
    {
        public static IServiceCollection AddImportModule(this IServiceCollection collection)
        {
            collection.AddSingleton<ImportModule>();
            collection.AddSingleton<ImportModuleView>();

            return collection;
        }
    }
}