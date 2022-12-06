namespace UKHO.FSSDesktop.Modules.FSS
{
    using FSSDesktop.Security;
    using Import;
    using ModernWpf.Controls;
    using Search;
    using Security;
    using UI.Modules;
    using UI.Navigation;
    using UI.Services;

    public class FSSModule : IModule
    {
        public int Ordinal => 0;

        public INavigationItem ConfigureNavigation(INavigationService collection)
        {
            var root = collection.AddNavigation(typeof(ChartImport), "File Share Service", new SymbolIcon(Symbol.Share), new[] {WellKnownClaim.Access}, false);

            root.AddNavigation(typeof(ChartImport), "Import", new SymbolIcon(Symbol.ImportAll), new[] {WellKnownClaim.AdministerImport});
            root.AddNavigation(typeof(ChartSearch), "Search", new SymbolIcon(Symbol.Find), new[] {WellKnownClaim.Access});
            root.AddNavigation(typeof(ChartSecurity), "Security", new SymbolIcon(Symbol.Admin), new[] {WellKnownClaim.AdministerSecurity});

            return root;
        }
    }
}