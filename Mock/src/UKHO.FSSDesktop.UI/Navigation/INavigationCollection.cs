using ModernWpf.Controls;
using System.Collections.Generic;
using System;

namespace UKHO.FSSDesktop.UI.Navigation
{
    public interface INavigationCollection
    {
        INavigationItem AddNavigation(Type modelType, string title, IconElement icon, bool selectOnInvoke = true);

        void ShowError(IEnumerable<(string message, string details)> errors);

        void ShowLogin();

        void ShowUnauthorized();

        void ShowNavigation();
    }
}