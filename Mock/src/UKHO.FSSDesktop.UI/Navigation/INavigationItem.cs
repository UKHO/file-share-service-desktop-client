using ModernWpf.Controls;
using System;

namespace UKHO.FSSDesktop.UI.Navigation
{
    using System.Collections.Generic;

    public interface INavigationItem
    {
        Type ModelType { get; }

        string Title { get; }

        IconElement Icon { get; }

        IEnumerable<string> Claims { get; }

        INavigationItem AddNavigation(Type modelType, string title, IconElement icon, IEnumerable<string> claims, bool selectOnInvoke = true);
    }
}