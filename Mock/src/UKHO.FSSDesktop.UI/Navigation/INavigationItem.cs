using ModernWpf.Controls;
using System;

namespace UKHO.FSSDesktop.UI.Navigation
{
    public interface INavigationItem
    {
        Type ModelType { get; }

        string Title { get; }

        IconElement Icon { get; }

        INavigationItem AddNavigation(Type modelType, string title, IconElement icon, bool selectOnInvoke = true);
    }
}