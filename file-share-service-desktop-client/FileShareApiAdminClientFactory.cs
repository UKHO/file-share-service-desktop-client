using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using UKHO.FileShareAdminClient;
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
        private readonly IAuthProvider authProvider;
        private readonly IVersionProvider versionProvider;

        public FileShareApiAdminClientFactory(IEnvironmentsManager environmentsManager, IAuthProvider authProvider,
            IVersionProvider versionProvider)
        {
            this.environmentsManager = environmentsManager;
            this.authProvider = authProvider;
            this.versionProvider = versionProvider;
        }

        public IFileShareApiAdminClient Build()
        {
            return new FileShareApiAdminClient(new UserAgentClientFactory(versionProvider),
                environmentsManager.CurrentEnvironment.BaseUrl,
                authProvider.CurrentAccessToken);
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