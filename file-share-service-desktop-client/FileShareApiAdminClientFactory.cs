using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider serviceProvider;

        public FileShareApiAdminClientFactory(IEnvironmentsManager environmentsManager, IAuthTokenProvider authTokenProvider,
        IVersionProvider versionProvider, IServiceProvider serviceProvider)

        {
            this.environmentsManager = environmentsManager;
            this.authTokenProvider = authTokenProvider;
            this.versionProvider = versionProvider;
            this.serviceProvider = serviceProvider;
        }

        public IFileShareApiAdminClient Build()
        {
            return new FileShareApiAdminClient(new UserAgentClientFactory(versionProvider, serviceProvider),
                environmentsManager.CurrentEnvironment.BaseUrl,
                authTokenProvider);
        }
    }


    public class UserAgentClientFactory : IHttpClientFactory
    {
        private readonly IVersionProvider versionProvider;
        private readonly IServiceProvider serviceProvider;

        public UserAgentClientFactory(IVersionProvider versionProvider, IServiceProvider serviceProvider)
        {
            this.versionProvider = versionProvider;
            this.serviceProvider = serviceProvider;
        }

        public HttpClient CreateClient(string name)
        {
            var configuredClient = serviceProvider.GetRequiredService<IHttpClientFactory>()
                    .CreateClient("FSSClient");

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