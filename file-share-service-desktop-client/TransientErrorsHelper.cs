using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace UKHO.FileShareService.DesktopClient
{
    public static class TransientErrorsHelper
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger, string requestType, int retryCount, double sleepDuration)
        {
            return Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .OrResult(r => r.StatusCode == HttpStatusCode.InternalServerError)
                .OrResult(r => r.StatusCode == HttpStatusCode.RequestTimeout)
                .WaitAndRetryAsync(retryCount, (retryAttempt) =>
                {
                    return TimeSpan.FromSeconds(Math.Pow(sleepDuration, (retryAttempt - 1)));
                }, async (response, timespan, retryAttempt, context) =>
                {
                    var retryAfterHeader = response.Result.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "retry-after");
                    var correlationId = response.Result.RequestMessage.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "x-correlation-id");
                    int retryAfter = 0;
                    if (response.Result.StatusCode == HttpStatusCode.TooManyRequests && retryAfterHeader.Value != null && retryAfterHeader.Value.Any())
                    {
                        retryAfter = int.Parse(retryAfterHeader.Value.First());
                        await Task.Delay(TimeSpan.FromMilliseconds(retryAfter));
                    }
                    logger.LogInformation("Re-trying {requestType} service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}.",
                    requestType, response.Result.RequestMessage.RequestUri, timespan.Add(TimeSpan.FromMilliseconds(retryAfter)).TotalMilliseconds, retryAttempt, correlationId.Value, response.Result.StatusCode);

                });
        }
    }
}
