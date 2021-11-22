using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using Serilog;
using UKHO.FileShareClient;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Admin;
using UKHO.FileShareService.DesktopClient.Modules.Auth;
using UKHO.FileShareService.DesktopClient.Modules.Search;
using Unity;
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
            //            var containerExtension = base.CreateContainerExtension() as UnityContainerExtension;
            //#if DEBUG
            //            containerExtension.Instance.AddExtension(new Diagnostic());
            //#endif
            //            return containerExtension;

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(loggingBuilder => {
                var logger = new LoggerConfiguration()
                        .ReadFrom.AppSettings()
                        .CreateLogger();
                loggingBuilder.AddSerilog(logger, dispose: true);
            });            

            int retryCount = Convert.ToInt32(ConfigurationManager.AppSettings["RetryCount"]);
            int sleepDurationMultiplier = Convert.ToInt32(ConfigurationManager.AppSettings["SleepDurationMultiplier"]);
            const string FSSClient = "FSSClient";

            serviceCollection.AddHttpClient(FSSClient)
                .AddPolicyHandler((services, request) =>
                {
                    var logger = services.GetService<ILogger<IFileShareApiAdminClientFactory>>();
                    if (logger == null)
                    {
                        throw new ArgumentNullException();
                    }
                    return TransientErrorsHelper.GetRetryPolicy(logger, "FileShareApiAdminClient", retryCount, sleepDurationMultiplier);
                });

            var container = new UnityContainer();
            container.BuildServiceProvider(serviceCollection);

            return new UnityContainerExtension(container);
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
        }

        protected override Window CreateShell()
        {
            var logger = Container.Resolve<ILogger<App>>();
            SetupExceptionHandling(logger);

            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            base.ConfigureModuleCatalog(moduleCatalog);
            moduleCatalog.AddModule<AuthenticateUiModule>();
            moduleCatalog.AddModule<SearchUiModule>();
            moduleCatalog.AddModule<AdminUiModule>();
        }

        private void SetupExceptionHandling(ILogger<App> logger)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException", logger);

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException", logger);
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException", logger);
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source, ILogger<App> logger)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                logger.LogError(exception, message);
                MessageBox.Show(string.Format("An exeption has occured: - {0}", exception.GetBaseException().Message), message, MessageBoxButton.OK, MessageBoxImage.Error);
                //If any issues occured during application startup safely shutdown application
                if(exception.Source == "Prism.Unity.Wpf") Shutdown();
            }
        }
    }
}