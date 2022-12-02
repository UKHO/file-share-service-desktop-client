using UKHO.FSSDesktop.UI.Navigation;

namespace UKHO.FSSDesktop.UI.Modules
{
    using Services;

    public interface IModule
    {
        int Ordinal { get; }

        INavigationItem ConfigureNavigation(INavigationService collection);
    }
}