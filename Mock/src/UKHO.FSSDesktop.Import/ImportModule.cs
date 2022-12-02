namespace UKHO.FSSDesktop.Import
{
    using ModernWpf.Controls;
    using Security;
    using UI.Modules;
    using UI.Navigation;
    using UI.Services;
    using Views;

    internal class ImportModule : IModule
    {
        public int Ordinal => int.MaxValue;

        public INavigationItem ConfigureNavigation(INavigationService collection)
        {
            var view = collection.AddNavigation(typeof(ChartImport), "Import", new SymbolIcon(Symbol.ImportAll), new[] { WellKnownClaim.AdministerImport });

            return view;
        }
    }
}