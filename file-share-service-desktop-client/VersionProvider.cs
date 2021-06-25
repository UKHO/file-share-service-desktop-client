using System.Linq;
using System.Reflection;

namespace UKHO.FileShareService.DesktopClient
{
    public interface IVersionProvider
    {
        string Version { get; }
    }

    public class VersionProvider : IVersionProvider
    {
        public string Version => Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>()
            .Single().Version;
    }
}