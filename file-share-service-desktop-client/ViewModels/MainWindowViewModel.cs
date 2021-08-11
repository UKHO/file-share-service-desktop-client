using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prism.Commands;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core;
using Unity;

namespace UKHO.FileShareService.DesktopClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEnvironmentsManager environmentsManager;
        private readonly INavigation navigation;
        private IEnumerable<IPageButton> pageButtons = Enumerable.Empty<IPageButton>();

        public MainWindowViewModel(IEnvironmentsManager environmentsManager, IUnityContainer containerRegistry,
            IAuthProvider authProvider, INavigation navigation)
        {
            this.environmentsManager = environmentsManager;
            this.navigation = navigation;

            environmentsManager.PropertyChanged += (sender, args) =>
            {
                RaisePropertyChanged(nameof(CurrentEnvironment));
            };

            authProvider.PropertyChanged += (sender, args) =>
                PageButtons = containerRegistry.ResolveAll<IPageButton>();

            PageButtonCommand = new DelegateCommand<IPageButton>(OnPageButtonExecute);
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

        public IEnumerable<IPageButton> PageButtons
        {
            get => pageButtons;
            private set
            {
                if (pageButtons != value)
                {
                    pageButtons = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DelegateCommand<IPageButton> PageButtonCommand { get; }

        public void OnPageButtonExecute(IPageButton pageButton)
        {
            navigation.RequestNavigate(pageButton.NavigationTarget);
        }

        public string Version => Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>()
            .Single().Version;
    }
}