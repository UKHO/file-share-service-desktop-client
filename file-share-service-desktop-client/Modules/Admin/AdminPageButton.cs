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
            Enabled = authProvider.IsLoggedIn && authProvider.Roles.Contains(BatchCreateRole);
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