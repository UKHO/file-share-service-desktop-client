using System.IO;

namespace UKHO.FileShareService.DesktopClient
{
    public interface IFileService
    {
        bool Exists(string path);
    }

    internal class FileService : IFileService
    {
        public bool Exists(string path)
        {
            return File.Exists(path);
        }
    }
}
