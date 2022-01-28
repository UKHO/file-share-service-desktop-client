using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
