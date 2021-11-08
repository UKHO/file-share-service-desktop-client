using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient;

namespace FileShareService.DesktopClientTests
{
    public class TransientErrorsTests
    {
        private ILogger<IFileShareApiAdminClientFactory> fakeLogger;
        public int retryCount = 3;
        private const double sleepDuration = 2;
        const string TestClient = "TestClient";
        private bool _isRetryCalled;

        [Test]
        public async Task WhenTooManyRequests_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();
            _isRetryCalled = false;
            retryCount = 1;

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(TransientErrorsHelper.GetRetryPolicy(fakeLogger, "File Share", retryCount, sleepDuration))
                .AddHttpMessageHandler(() => new TooManyRequestsDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://test.com");

            // Assert
            Assert.False(_isRetryCalled);
            Assert.AreEqual(HttpStatusCode.TooManyRequests, result.StatusCode);
        }
    }

    public class TooManyRequestsDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpResponse = new HttpResponseMessage();
            httpResponse.Headers.Add("retry-after", "3600");
            httpResponse.RequestMessage = new HttpRequestMessage();
            httpResponse.RequestMessage.Headers.Add("x-correlation-id", "");
            httpResponse.StatusCode = HttpStatusCode.TooManyRequests;
            return Task.FromResult(httpResponse);
        }
    }
}