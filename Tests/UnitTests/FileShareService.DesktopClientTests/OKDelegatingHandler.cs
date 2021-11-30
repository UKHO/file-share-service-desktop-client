using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FileShareService.DesktopClientTests
{
    public class OKDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpResponse = new HttpResponseMessage();
            httpResponse.RequestMessage = new HttpRequestMessage();
            httpResponse.StatusCode = HttpStatusCode.OK;
            return Task.FromResult(httpResponse);
        }
    }
}
