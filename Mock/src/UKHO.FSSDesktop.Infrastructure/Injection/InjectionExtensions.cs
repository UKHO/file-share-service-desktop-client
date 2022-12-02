namespace UKHO.FSSDesktop.Infrastructure.Injection
{
    using FSSDesktop.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Services;

    public static class InjectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection collection)
        {
            collection.AddSingleton<IAuthenticationService, AuthenticationService>();
            collection.AddSingleton<ISearchService, SearchService>();

            return collection;
        }
    }
}