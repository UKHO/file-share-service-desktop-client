namespace UKHO.FSSDesktop.UI.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using FSSDesktop.Services;
    using Security;

    internal class FrameworkLogin : ObservableObject
    {
        private readonly RelayCommand _loginCommand;
        private string _emailAddress;
        private SecureString? _password;
        private IEnumerable<DeploymentEnvironment> _environments;
        private DeploymentEnvironment _selectedEnvironment;

        public FrameworkLogin(IAuthenticationService authenticationService)
        {
            _loginCommand = new RelayCommand(() =>
            {
                authenticationService.Authenticate(_emailAddress!, _password!, _selectedEnvironment!);
                
            }, () => !string.IsNullOrWhiteSpace(_emailAddress) && _password?.Length > 0 && _selectedEnvironment != null);

            _emailAddress = string.Empty;

            _environments = new List<DeploymentEnvironment>()
            {
                new("Dev", new Uri("https://someaddress/dev"), "Development - UNSTABLE"),
                new("QA", new Uri("https://someaddress/qa"), "Testing"),
                new("Live", new Uri("https://someaddress/dev"), "Live production environment"),
            };

            _selectedEnvironment = _environments.First();
        }

        public ICommand LoginCommand => _loginCommand;

        public IEnumerable<DeploymentEnvironment> Environments => _environments;

        public DeploymentEnvironment SelectedEnvironment
        {
            get => _selectedEnvironment;
            set
            {
                SetProperty(ref _selectedEnvironment, value);

                _loginCommand.NotifyCanExecuteChanged();
            }
        }

        public string EmailAddress
        {
            get => _emailAddress;
            set
            {
                SetProperty(ref _emailAddress, value);
                
                _loginCommand.NotifyCanExecuteChanged();
            }
        }

        public SecureString? Password
        {
            private get => _password;
            set
            {
                _password = value;

                _loginCommand.NotifyCanExecuteChanged();
            }
        }
    }
}