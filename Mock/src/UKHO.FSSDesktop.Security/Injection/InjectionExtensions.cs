namespace UKHO.FSSDesktop.Security.Injection
{
    using Microsoft.Extensions.DependencyInjection;

    public static class InjectionExtensions
    {
        public static IServiceCollection AddSecurityModule(this IServiceCollection collection)
        {

            return collection;
        }
    }
}