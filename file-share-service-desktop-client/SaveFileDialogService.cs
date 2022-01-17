using System;


namespace UKHO.FileShareService.DesktopClient
{
    public interface ISaveFileDialogService
    {
        string SaveFileDialog(string fileName);
    }
    public class SaveFileDialogService : ISaveFileDialogService
    {

        public string SaveFileDialog(string fileName)
        {
            string downloadLocation = string.Empty;
            var dialog = new Microsoft.Win32.SaveFileDialog();

            dialog.InitialDirectory = downloadLocation;
            dialog.Title = "Select a Directory";
            dialog.Filter = "Directory|*.this.directory";
            dialog.FileName = "select";

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                // Remove fake filename from resulting path
                path = path.Replace("\\select.this.directory", "");
                path = path.Replace(".this.directory", "");
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                downloadLocation = path;
            }
            return downloadLocation;
        }
    }
}