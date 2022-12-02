namespace UKHO.FSSDesktop.UI.Services
{
    using System;
    using System.Collections.Generic;
    using ModernWpf.Controls;
    using Navigation;

    public interface INavigationService
    {
        INavigationItem AddNavigation(Type modelType, string title, IconElement icon, IEnumerable<string> claims, bool selectOnInvoke = true);

        void ShowError(IEnumerable<(string message, string details)> errors);

        void ShowLogin();

        void ShowUnauthorized();

        void ShowNavigation();
    }
}