namespace UKHO.FSSDesktop.Search.Injection
{
    using Microsoft.Extensions.DependencyInjection;

    public static class InjectionExtensions
    {
        public static IServiceCollection AddSearchModule(this IServiceCollection collection)
        {

            return collection;
        }
    }
}