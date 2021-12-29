using Prism.Commands;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    /// <summary>
    /// Interaction logic for BatchDetailsView.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class BatchDetailsView : UserControl
    {
        public BatchDetailsView()
        {
            InitializeComponent();
            
            this.DownloadExecutionCommand = new DelegateCommand<string>(OnDownloadExecutionCommand);
        }

        private void OnDownloadExecutionCommand(string fileName)
        {
            var downloadLocation = GetDownloadLocation(fileName);
            downloadLocation = Path.Combine(downloadLocation, fileName);

        }

        public DelegateCommand<string> DownloadExecutionCommand { get; private set; }
               
        #region private methods
        //This should be helper class
        private static string GetDownloadLocation(string fileName)
        {
            string downloadLocation = string.Empty;
            MessageBoxService messageBoxService = new MessageBoxService();  

            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.InitialDirectory = downloadLocation; // Use current value for initial dir
            dialog.Title = "Select a Directory"; // instead of default "Save As"
            dialog.Filter = "Directory|*.this.directory"; // Prevents displaying files
            dialog.FileName = "select"; // Filename will then be "select.this.directory"

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                // Remove fake filename from resulting path
                path = path.Replace("\\select.this.directory", "");
                path = path.Replace(".this.directory", ""); // If user has changed the filename, create the new directory
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                // Replace, if file already exists in selected directory
                if (File.Exists(fileName)  && messageBoxService.ShowMessageBox($"Confirmation for fileName: {fileName}", $"{fileName} already exists in selected directory. Do you want to replace it ?",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    return "";
                }
                    // Our final value is in path
                    downloadLocation = path;
                    File.Delete(fileName);
            }

            return downloadLocation;
        }
        #endregion
    }
}