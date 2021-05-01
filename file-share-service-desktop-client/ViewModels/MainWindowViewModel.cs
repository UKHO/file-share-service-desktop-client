using System.Collections.Generic;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core;

namespace UKHO.FileShareService.DesktopClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IEnvironmentsManager environmentsManager;

        public MainWindowViewModel(IEnvironmentsManager environmentsManager)
        {
            this.environmentsManager = environmentsManager;
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
    }
}