using FakeItEasy;
using FakeItEasy.Configuration;
using Microsoft.Extensions.Logging;
namespace FileShareService.DesktopClientTests
{
    public static class LoggerExtensions
    {
        public static IVoidArgumentValidationConfiguration VerifyLog<T>(this ILogger<T> logger, LogLevel level)
        {
            return A.CallTo(logger)
            .Where(call => call.Method.Name == nameof(logger.Log)
            && call.GetArgument<LogLevel>(0) == level);
        }
    }
}