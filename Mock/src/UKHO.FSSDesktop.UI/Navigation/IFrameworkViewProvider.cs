namespace UKHO.FSSDesktop.UI.Navigation
{
    using System.Windows;
    using Services;

    public interface IFrameworkViewProvider : INavigationService
    {
        FrameworkElement CurrentView { get; }
    }
}