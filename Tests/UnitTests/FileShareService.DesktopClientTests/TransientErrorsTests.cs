using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient;
using FakeItEasy;

namespace FileShareService.DesktopClientTests
{
    public class TransientErrorsTests
    {
        private ILogger<IFileShareApiAdminClientFactory> fakeLogger = null!;
        public int retryCount = 3;
        private const double sleepDuration = 2;
        const string TestClient = "TestClient";
        private bool _isRetryCalled;

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<IFileShareApiAdminClientFactory>>();
        }

        [Test]
        public async Task WhenTooManyRequests_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();
            _isRetryCalled = false;
            retryCount = 1;

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(TransientErrorsHelper.GetRetryPolicy(fakeLogger, "File Share Service", retryCount, sleepDuration))
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


        [Test]
        public async Task WhenServiceUnavailable_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();
            _isRetryCalled = false;

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(TransientErrorsHelper.GetRetryPolicy(fakeLogger, "File Share Service", retryCount, sleepDuration))
                .AddHttpMessageHandler(() => new ServiceUnavailableDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://test.com");

            // Assert
            Assert.False(_isRetryCalled);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, result.StatusCode);

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

        public class ServiceUnavailableDelegatingHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var httpResponse = new HttpResponseMessage();
                httpResponse.RequestMessage = new HttpRequestMessage();
                httpResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                return Task.FromResult(httpResponse);
            }
        }
    }
}