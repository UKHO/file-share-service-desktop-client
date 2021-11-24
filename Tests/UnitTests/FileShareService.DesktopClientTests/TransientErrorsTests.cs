using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
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
        public async Task WhenTooManyRequests__RetryShouldBeCalled()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();

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
            fakeLogger.VerifyLog(LogLevel.Warning).MustHaveHappened();//TooManyRequest is transient error so retry will be called logs warning
            Assert.AreEqual(HttpStatusCode.TooManyRequests, result.StatusCode);
        }
        [Test]
        public async Task WhenOKResponse_RetryShouldNotBeCalled()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(TransientErrorsHelper.GetRetryPolicy(fakeLogger, "File Share Service", retryCount, sleepDuration))
            .AddHttpMessageHandler(() => new OKDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://test.com");

            // Assert
            fakeLogger.VerifyLog(LogLevel.Warning).MustNotHaveHappened();//Ok response will not call retry so there will be no log
            Assert.AreEqual(HttpStatusCode.OK, result.StatusCode);
        }
    }
}