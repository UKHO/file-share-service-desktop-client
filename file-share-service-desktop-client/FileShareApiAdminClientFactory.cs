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
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FileShareServiceDesktopClient",
                versionProvider.Version));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(nameof(FileShareApiAdminClient),
                typeof(FileShareApiAdminClient)
                    .Assembly
                    .GetCustomAttributes<AssemblyFileVersionAttribute>()
                    .Single()
                    .Version));
            return client;
        }
    }
}