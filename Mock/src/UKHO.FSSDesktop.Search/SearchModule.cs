namespace UKHO.FSSDesktop.Search
{
    using ModernWpf.Controls;
    using Security;
    using UI.Modules;
    using UI.Navigation;
    using UI.Services;
    using Views;

    internal class SearchModule : IModule
    {
        public int Ordinal => int.MinValue;

        public INavigationItem ConfigureNavigation(INavigationService collection)
        {
            var view = collection.AddNavigation(typeof(ChartSearch), "Search", new SymbolIcon(Symbol.Find), new[] {WellKnownClaim.Access});

            return view;
        }
    }
}