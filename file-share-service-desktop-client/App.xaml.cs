using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Modules.Admin;
using UKHO.FileShareService.DesktopClient.Modules.Auth;
using UKHO.FileShareService.DesktopClient.Modules.Search;
using Unity;

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
            var containerExtension = (UnityContainerExtension) base.CreateContainerExtension();
#if DEBUG
            containerExtension.Instance.AddExtension(new Diagnostic());
#endif

            var loggerFactory = LoggerFactory.Create(logging =>
            {
                logging.AddEventLog(eventLogSettings => { eventLogSettings.SourceName = "FSS"; });
            });
            containerExtension.Instance.RegisterInstance(loggerFactory);


            var loggerFactoryMethod = typeof(LoggerFactoryExtensions)
                .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                .First(x => x.ContainsGenericParameters);

            containerExtension.Instance.RegisterFactory(typeof(ILogger<>),
                (c, t, s) =>
                {
                    var genFactoryMethod = loggerFactoryMethod.MakeGenericMethod(t.GetGenericArguments()[0]);
                    return genFactoryMethod.Invoke(null, new object[] {loggerFactory});
                });
            return containerExtension;
        }


        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IEnvironmentsManager, EnvironmentLoader>();
            containerRegistry.RegisterSingleton<IAuthProvider, AuthProvider>();
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