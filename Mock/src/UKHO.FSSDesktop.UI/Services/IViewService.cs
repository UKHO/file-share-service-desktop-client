using System.Windows;

namespace UKHO.FSSDesktop.UI.Services
{
    public interface IViewService
    {
        T GetViewFor<T>(object model) where T : FrameworkElement;
    }
}