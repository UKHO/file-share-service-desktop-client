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

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<IFileShareApiAdminClientFactory>>();
        }

        [Test]
        public async Task WhenServiceUnavailable_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(TransientErrorsHelper.GetRetryPolicy(fakeLogger, "File Share Service", retryCount, sleepDuration));

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://filesqa1.admiralty.co.uk/");

            // Assert
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        }

        [Test]
        public async Task WhenTooManyRequests_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(TransientErrorsHelper.GetRetryPolicy(fakeLogger, "File Share Service", retryCount, sleepDuration));

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://mock.codes/429");

            // Assert
            Assert.AreEqual(HttpStatusCode.TooManyRequests, result.StatusCode);
        }
        [Test]
        public async Task WhenInternalServerError_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(TransientErrorsHelper.GetRetryPolicy(fakeLogger, "File Share Service", retryCount, sleepDuration));

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://mock.codes/500");

            // Assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, result.StatusCode);
        }
        [Test]
        public async Task WhenRequestTimeout_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(TransientErrorsHelper.GetRetryPolicy(fakeLogger, "File Share Service", retryCount, sleepDuration));

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://mock.codes/408");

            // Assert
            Assert.AreEqual(HttpStatusCode.RequestTimeout, result.StatusCode);
        }

    }
}