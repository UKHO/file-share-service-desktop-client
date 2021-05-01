using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Prism.Mvvm;

namespace UKHO.FileShareService.DesktopClient.Core
{
    public interface IEnvironmentsManager : INotifyPropertyChanged
    {
        IReadOnlyList<EnvironmentConfig> Environments { get; }
        EnvironmentConfig CurrentEnvironment { get; set; }
    }

    public class EnvironmentLoader : BindableBase, IEnvironmentsManager
    {
        private EnvironmentConfig currentEnvironment;
        public IReadOnlyList<EnvironmentConfig> Environments { get; }

        public EnvironmentLoader()
        {
            var readAllText = File.ReadAllText("environments.json");
            var environments = JsonConvert.DeserializeObject<List<EnvironmentConfig>>(readAllText);
            if (environments == null || !environments.Any())
                throw new ApplicationException("No environments have been configured");

            Environments = environments;
            foreach (var environment in Environments)
            {
                if (string.IsNullOrEmpty(environment.Name))
                    throw new ArgumentException("An environment has a null or empty name");
                if (string.IsNullOrEmpty(environment.TenantId))
                    throw new ArgumentException(
                        $"Environment {environment.Name} has a null or empty {nameof(environment.TenantId)}");
                if (string.IsNullOrEmpty(environment.ClientId))
                    throw new ArgumentException(
                        $"Environment {environment.Name} has a null or empty {nameof(environment.ClientId)}");
                if (string.IsNullOrEmpty(environment.BaseUrl))
                    throw new ArgumentException(
                        $"Environment {environment.Name} has a null or empty {nameof(environment.BaseUrl)}");
            }

            currentEnvironment = Environments.First();
        }

        public EnvironmentConfig CurrentEnvironment
        {
            get => currentEnvironment;
            set
            {
                if (currentEnvironment != value)
                {
                    currentEnvironment = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}