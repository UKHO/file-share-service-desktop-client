namespace FSSDesktop
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using Microsoft.Extensions.DependencyInjection;
    using ModernWpf;
    using ModernWpf.Controls;
    using Serilog;
    using UKHO.FSSDesktop.Import.Injection;
    using UKHO.FSSDesktop.Search.Injection;
    using UKHO.FSSDesktop.Security.Injection;
    using UKHO.FSSDesktop.UI.Injection;
    using UKHO.FSSDesktop.UI.Services;
    using UKHO.FSSDesktop.UI.Windows;

    internal class FSSDesktopApplication : Application
    {
        private const string LogOutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        private IServiceProvider? _serviceProvider;

        public FSSDesktopApplication()
        {
            _serviceProvider = null;

            ConfigureLogging();
            LoadResources();
        }

        public void Start()
        {
            _serviceProvider = ConfigureInjection();

            var model = _serviceProvider.GetService<FrameworkWindow>()!;

            var viewService = _serviceProvider.GetService<IViewService>()!;
            var view = viewService.GetViewFor<FrameworkWindowView>(model)!;

            Run(view);
        }

        private static IServiceProvider ConfigureInjection()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddFrameworkUI();

            serviceCollection.AddImportModule();
            serviceCollection.AddSearchModule();
            serviceCollection.AddSecurityModule();

            var provider = serviceCollection.BuildServiceProvider();

            return provider;
        }

        private void ConfigureLogging()
        {
            var logConfiguration = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File("logs\\log-.txt", rollingInterval: RollingInterval.Day,
                    outputTemplate: LogOutputTemplate);

            Log.Logger = logConfiguration.CreateLogger();
        }

        private void LoadResources()
        {
            var themeResources = new ThemeResources();

            ((ISupportInitialize) themeResources).BeginInit();
            Resources.MergedDictionaries.Add(themeResources);
            ((ISupportInitialize) themeResources).EndInit();

            Resources.MergedDictionaries.Add(new XamlControlsResources());
        }
    }
}