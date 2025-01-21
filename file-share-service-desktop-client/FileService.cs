using System.IO;

namespace UKHO.FileShareService.DesktopClient
{
    public interface IFileService
    {
        bool Exists(string path);
        void WriteAllText(string path, string content);
    }

    internal class FileService : IFileService
    {
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public void WriteAllText(string path, string content)
        {
            File.WriteAllText(path, content);
        }
    }
}
