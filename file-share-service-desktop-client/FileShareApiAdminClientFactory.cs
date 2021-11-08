using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using UKHO.FileShareAdminClient;
using UKHO.FileShareClient;
using UKHO.FileShareService.DesktopClient.Core;

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
            IVersionProvider versionProvider)
        {
            this.environmentsManager = environmentsManager;
            this.authTokenProvider = authTokenProvider;
            this.versionProvider = versionProvider;
        }

        public IFileShareApiAdminClient Build()
        {
            return new FileShareApiAdminClient(new UserAgentClientFactory(versionProvider),
                environmentsManager.CurrentEnvironment.BaseUrl,
                authTokenProvider);
        }
    }


    public class UserAgentClientFactory : IHttpClientFactory
    {
        private readonly IVersionProvider versionProvider;

        public UserAgentClientFactory(IVersionProvider versionProvider)
        {
            this.versionProvider = versionProvider;
        }

        public HttpClient CreateClient(string name)
        {
            const string FSSClient = "FSSClient";
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(FSSClient)
                  //.AddPolicyHandler((services, request) => GetRetryPolicy(services.GetService<ILogger<IFileShareApiAdminClient>>(), "FileShareApiAdminClient", 2, 3));
                  .AddPolicyHandler((services, request) => TransientErrorsHelper.GetRetryPolicy(services.GetService<ILogger<IFileShareApiAdminClient>>(), "FileShareApiAdminClient", 5, 2));
            //.AddHttpMessageHandler(() => new ServiceUnavailableDelegatingHandler());

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