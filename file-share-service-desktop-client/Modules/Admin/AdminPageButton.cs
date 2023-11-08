using System;
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
            //the API itself performs case insensitive comparison, so we do the same here

            bool isBatchCreate(string role) => 0 == string.Compare(BatchCreateRole, role, StringComparison.OrdinalIgnoreCase);

            bool isBuBatchCreate(string role) =>
                role!.StartsWith($"{BatchCreateRole}_", StringComparison.OrdinalIgnoreCase)
                && $"{BatchCreateRole}_".Length < role.Length;

            var hasAdminRole = authProvider.Roles
                .Select(role => role?.Trim())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Any(role => isBatchCreate(role!) || isBuBatchCreate(role!));

            Enabled = authProvider.IsLoggedIn && hasAdminRole;
        }

        public virtual string DisplayName => "Admin";

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

        public virtual string NavigationTarget => NavigationTargets.Admin;
    }
}