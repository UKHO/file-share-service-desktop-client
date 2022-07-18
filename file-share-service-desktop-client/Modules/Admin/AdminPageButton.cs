using System.Linq;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin
{
    public class AdminPageButton : BindableBase, IPageButton
    {
        private const string BatchCreateRole = "BatchCreate";

        private bool enabled;

        public AdminPageButton(IAuthProvider authProvider)
        {
            authProvider.PropertyChanged += (sender, args) =>
                SetEnabled(authProvider);

            SetEnabled(authProvider);
        }

        private void SetEnabled(IAuthProvider authProvider)
        {
            //the API itself performs case (& culture) sensitive comparison, so we do the same here
            Enabled = authProvider.IsLoggedIn && authProvider.Roles.Any(role => role == BatchCreateRole || role.StartsWith($"{BatchCreateRole}_"));
        }

        public string DisplayName => "Admin";

        public bool Enabled
        {
            get => enabled;
            private set
            {
                if (enabled != value)
                {
                    enabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string NavigationTarget => NavigationTargets.Admin;
    }
}