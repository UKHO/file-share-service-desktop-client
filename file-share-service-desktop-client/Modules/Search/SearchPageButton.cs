using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    public class SearchPageButton : BindableBase, IPageButton
    {
        private readonly IAuthProvider authProvider;

        public SearchPageButton(IAuthProvider authProvider)
        {
            this.authProvider = authProvider;
            authProvider.PropertyChanged += (sender, args) => { RaisePropertyChanged(nameof(Enabled)); };
        }

        public string DisplayName => "Search";

        public bool Enabled => authProvider.IsLoggedIn;
        public string NavigationTarget => NavigationTargets.Search;
    }
}