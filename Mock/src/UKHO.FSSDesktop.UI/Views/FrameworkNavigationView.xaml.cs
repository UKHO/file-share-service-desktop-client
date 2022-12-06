namespace UKHO.FSSDesktop.UI.Views
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using CommunityToolkit.Mvvm.Messaging;
    using Messages;
    using Microsoft.Extensions.DependencyInjection;
    using ModernWpf.Controls;
    using Navigation;
    using Services;

    /// <summary>
    ///     Interaction logic for FrameworkNavigationView.xaml
    /// </summary>
    public partial class FrameworkNavigationView : UserControl
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _processNavigationSelection;

        public FrameworkNavigationView(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();

            _processNavigationSelection = true;

            Loaded += (sender, e) =>
            {
                var navigationItems = ((FrameworkNavigation) DataContext).NavigationItems.ToList();

                if (navigationItems.Any())
                {
                    NavigationView.SelectedItem = navigationItems.First();
                }
            };
        }

        private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (_processNavigationSelection)
            {
                if (args.IsSettingsSelected)
                {
                    NavigateTo(typeof(FrameworkSettings), "Settings");
                }
                else
                {
                    if (args.SelectedItem is INavigationItem item)
                    {
                        NavigateTo(item);
                    }
                }
            }
        }

        public void NavigateTo(INavigationItem item)
        {
            _processNavigationSelection = false;
            NavigationView.SelectedItem = item;
            _processNavigationSelection = true;

            NavigateTo(item.ModelType, item.Title);
        }

        private void NavigateTo(Type modelType, string header)
        {
            var model = _serviceProvider.GetService(modelType);

            var viewService = _serviceProvider.GetService<IViewService>()!;
            var view = viewService.GetViewFor<FrameworkElement>(model!);

            NavigationFrame.Navigate(view);
            NavigationView.Header = header;
        }
    }
}