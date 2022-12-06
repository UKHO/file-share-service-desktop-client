namespace UKHO.FSSDesktop.Modules.ESS
{
    using FSS.Import;
    using FSSDesktop.Security;
    using ModernWpf.Controls;
    using Security;
    using UI.Modules;
    using UI.Navigation;
    using UI.Services;

    public class ESSModule : IModule
    {
        public int Ordinal => 1;

        public INavigationItem ConfigureNavigation(INavigationService collection)
        {
            var root = collection.AddNavigation(typeof(ChartImport), "Exchange Set Service", new SymbolIcon(Symbol.Document), new[] {WellKnownClaim.Access}, false);

            root.AddNavigation(typeof(ESSChartSecurity), "Security", new SymbolIcon(Symbol.Admin), new[] {WellKnownClaim.AdministerSecurity});

            return root;
        }
    }
}