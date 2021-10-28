using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Windows;
using System.Windows.Markup;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Serilog;
using Microsoft.Extensions.Logging;
using UKHO.FileShareClient;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Admin;
using UKHO.FileShareService.DesktopClient.Modules.Auth;
using UKHO.FileShareService.DesktopClient.Modules.Search;
using Unity;
using Microsoft.Extensions.DependencyInjection;
using Unity.Microsoft.DependencyInjection;

namespace UKHO.FileShareService.DesktopClient
{  
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class App
    {
        public App()
        {
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        protected override IContainerExtension CreateContainerExtension()
        {
            var serviceCollection = new ServiceCollection();         
            serviceCollection.AddLogging(loggingBuilder =>
            loggingBuilder.AddFile(GetFilePath(), outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}"));

            var container = new UnityContainer();
            container.BuildServiceProvider(serviceCollection);

            return new UnityContainerExtension(container);
        }

        public string GetFilePath()
        {
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FssToolingAppLog\\";
            Directory.CreateDirectory(filePath);
            filePath = filePath + "UKHO.FileShareService.DesktopClient-Logs.txt";
            return filePath;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IEnvironmentsManager, EnvironmentLoader>();
            containerRegistry.RegisterSingleton<IAuthProvider, AuthProvider>();
            containerRegistry.RegisterSingleton<IAuthTokenProvider, AuthProvider>();
            containerRegistry.Register<INavigation, NavigationManager>();
            containerRegistry.Register<IFssSearchStringBuilder, FssSearchStringBuilder>();
            containerRegistry.Register<IJwtTokenParser, JwtTokenParser>();
            containerRegistry.Register<IFileSystem, FileSystem>();
            containerRegistry.Register<IKeyValueStore, KeyValueStore>();
            containerRegistry.Register<IJobsParser, JobsParser>();

            containerRegistry.Register<IFileShareApiAdminClientFactory, FileShareApiAdminClientFactory>();
            containerRegistry.Register<IVersionProvider, VersionProvider>();
            containerRegistry.Register<ICurrentDateTimeProvider, CurrentDateTimeProvider>();
            containerRegistry.Register<Microsoft.Extensions.Logging.ILogger>();
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();           
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule<AuthenticateUiModule>();
            moduleCatalog.AddModule<SearchUiModule>();
            moduleCatalog.AddModule<AdminUiModule>();
        }
    }
}