using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using UKHO.FileShareAdminClient;
using UKHO.FileShareClient;
using UKHO.FileShareService.DesktopClient.Core;
using System;

namespace UKHO.FileShareService.DesktopClient
{
    public interface IFileShareApiAdminClientFactory
    {
        IFileShareApiAdminClient Build();
    }

    public class FileShareApiAdminClientFactory : IFileShareApiAdminClientFactory
    {
        private readonly IEnvironmentsManager environmentsManager;
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly IVersionProvider versionProvider;
        private readonly ILogger<IFileShareApiAdminClientFactory> logger;


        public FileShareApiAdminClientFactory(IEnvironmentsManager environmentsManager, IAuthTokenProvider authTokenProvider,
            IVersionProvider versionProvider, ILogger<IFileShareApiAdminClientFactory> logger)
        {
            this.environmentsManager = environmentsManager;
            this.authTokenProvider = authTokenProvider;
            this.versionProvider = versionProvider;
            this.logger = logger;
        }

        public IFileShareApiAdminClient Build()
        {
            return new FileShareApiAdminClient(new UserAgentClientFactory(versionProvider, logger),
                environmentsManager.CurrentEnvironment.BaseUrl,
                authTokenProvider);
        }
    }


    public class UserAgentClientFactory : IHttpClientFactory
    {
        private readonly IVersionProvider versionProvider;
        private readonly ILogger<IFileShareApiAdminClientFactory> logger;

        public UserAgentClientFactory(IVersionProvider versionProvider, ILogger<IFileShareApiAdminClientFactory> logger)
        {
            this.versionProvider = versionProvider;
            this.logger = logger;
        }

        public HttpClient CreateClient(string name)
        {
            int retryCount = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["RetryCount"]);
            int sleepDurationMultiplier = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SleepDurationMultiplier"]);

            const string FSSClient = "FSSClient";
            bool isRetryCalled = false;
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(FSSClient)
                  .AddPolicyHandler((services, request) => TransientErrorsHelper.GetRetryPolicy(this.logger, "FileShareApiAdminClient", retryCount, sleepDurationMultiplier, out isRetryCalled));

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(FSSClient);

            configuredClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("FileShareServiceDesktopClient", versionProvider.Version));
            configuredClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(nameof(FileShareApiAdminClient), typeof(FileShareApiAdminClient)
                    .Assembly
                    .GetCustomAttributes<AssemblyFileVersionAttribute>()
                    .Single()
                    .Version));

            return configuredClient;
        }
    }
}