using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient.Modules.Auth
{
    public class AuthenticateViewModel : BindableBase
    {
        private readonly IAuthProvider authProvider;
        private readonly IEnvironmentsManager environmentsManager;
        private readonly INavigation navigation;
        private bool isIsAuthenticating;
        private string output = "";

        public AuthenticateViewModel(IAuthProvider authProvider, IEnvironmentsManager environmentsManager,
            INavigation navigation)
        {
            this.authProvider = authProvider;
            this.environmentsManager = environmentsManager;
            this.navigation = navigation;
            LoginCommand = new DelegateCommand(async () => await OnLogin(),
                () => !IsAuthenticating && !IsAuthenticated);

            authProvider.PropertyChanged += (sender, args) =>
            {
                RaisePropertyChanged(nameof(IsAuthenticated));
                LoginCommand.RaiseCanExecuteChanged();
            };

            environmentsManager.PropertyChanged += (sender, args) => RaisePropertyChanged(nameof(CurrentEnvironment));
        }

        public EnvironmentConfig CurrentEnvironment
        {
            get => environmentsManager.CurrentEnvironment;
            set
            {
                if (value != environmentsManager.CurrentEnvironment)
                {
                    environmentsManager.CurrentEnvironment = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IEnumerable<EnvironmentConfig> Environments => environmentsManager.Environments;

        private async Task OnLogin()
        {
            IsAuthenticating = true;
            try
            {
                var authToken = await authProvider.Login();
                Output +=
                    $"\nLogged into {environmentsManager.CurrentEnvironment.Name} at {DateTimeOffset.Now}\n{authToken}\nExpires: {authProvider.CurrentAccessTokenExpiry!.Value.ToLocalTime()}";
                navigation.RequestNavigate(NavigationTargets.Search);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                IsAuthenticating = false;
            }
        }

        public bool IsAuthenticated => authProvider.IsLoggedIn;

        public bool IsAuthenticating
        {
            get => isIsAuthenticating;
            set
            {
                if (isIsAuthenticating != value)
                {
                    isIsAuthenticating = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsAuthenticated));
                    LoginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DelegateCommand LoginCommand { get; }

        public string Output
        {
            get => output;
            set
            {
                if (output != value)
                {
                    output = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}