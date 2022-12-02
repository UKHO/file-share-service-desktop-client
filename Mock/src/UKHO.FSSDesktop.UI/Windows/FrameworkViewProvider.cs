namespace UKHO.FSSDesktop.UI.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Messaging;
    using FSSDesktop.Messages.Security;
    using ModernWpf.Controls;
    using Navigation;
    using Services;
    using Views;

    internal class FrameworkViewProvider : ObservableObject, IFrameworkViewProvider, IRecipient<AuthenticationMessage>
    {
        private readonly IViewService _viewService;
        private readonly FrameworkNavigation _navigation;
        private readonly FrameworkLogin _login;
        private readonly FrameworkError _error;
        private readonly FrameworkUnauthorized _unauthorized;
        private bool _showingError;
        private bool _showingLogin;

        private bool _showingNavigation;
        private bool _showingUnauthorized;

        private readonly ObservableCollection<INavigationItem> _navigationItems;

        private ObservableObject _currentModel;
        
        public FrameworkViewProvider(IViewService viewService, FrameworkNavigation navigation, FrameworkLogin login, FrameworkError error, FrameworkUnauthorized unauthorized)
        {
            _viewService = viewService;
            _navigation = navigation;
            _login = login;
            _error = error;
            _unauthorized = unauthorized;
            _navigationItems = new ObservableCollection<INavigationItem>();

            _showingError = false;
            _showingLogin = true;
            _showingNavigation = false;
            _showingUnauthorized = false;

            _currentModel = login;

            WeakReferenceMessenger.Default.Register(this);
        }

        public ObservableObject CurrentModel => _currentModel;

        public FrameworkElement CurrentView => _viewService.GetViewFor<FrameworkElement>(CurrentModel);

        public bool ShowingNavigation
        {
            get => _showingNavigation;
            set => SetProperty(ref _showingNavigation, value);
        }

        public bool ShowingLogin
        {
            get => _showingLogin;
            set => SetProperty(ref _showingLogin, value);
        }

        public bool ShowingUnauthorized
        {
            get => _showingUnauthorized;
            set => SetProperty(ref _showingUnauthorized, value);
        }

        public bool ShowingError
        {
            get => _showingError;
            set => SetProperty(ref _showingError, value);
        }

        public INavigationItem AddNavigation(Type modelType, string title, IconElement icon, IEnumerable<string> claims, bool selectOnInvoke = true)
        {
            return _navigation.AddNavigation(modelType, title, icon, claims, selectOnInvoke);
        }

        public void ShowError(IEnumerable<(string message, string details)> errors)
        {
            ShowingError = true;
            ShowingLogin = false;
            ShowingUnauthorized = false;
            ShowingNavigation = false;

            SetCurrent(_error);
        }

        public void ShowLogin()
        {
            ShowingError = false;
            ShowingLogin = true;
            ShowingUnauthorized = false;
            ShowingNavigation = false;

            SetCurrent(_login);
        }

        public void ShowUnauthorized()
        {
            ShowingError = false;
            ShowingLogin = false;
            ShowingUnauthorized = true;
            ShowingNavigation = false;

            SetCurrent(_unauthorized);
        }

        public void ShowNavigation()
        {
            ShowingError = false;
            ShowingLogin = false;
            ShowingUnauthorized = false;
            ShowingNavigation = true;

            SetCurrent(_navigation);
        }

        private void SetCurrent(ObservableObject model)
        {
            _currentModel = model;

            OnPropertyChanged(nameof(CurrentModel));
            OnPropertyChanged(nameof(CurrentView));
        }

        public void Receive(AuthenticationMessage message)
        {
            if (message.Context == null)
            {
                ShowUnauthorized();
            }
            else
            {
                ShowNavigation();
            }
        }
    }
}