namespace UKHO.FSSDesktop.Security
{
    using ModernWpf.Controls;
    using UI.Modules;
    using UI.Navigation;
    using UI.Services;
    using Views;

    internal class SecurityModule : IModule
    {
        public int Ordinal => int.MinValue;

        public INavigationItem ConfigureNavigation(INavigationService collection)
        {
            var view = collection.AddNavigation(typeof(ChartSecurity), "Security", new SymbolIcon(Symbol.Admin), new[] {WellKnownClaim.AdministerSecurity});

            return view;
        }
    }
}