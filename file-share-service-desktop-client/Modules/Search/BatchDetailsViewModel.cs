using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using UKHO.FileShareClient.Models;
using Microsoft.Extensions.Logging;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{

    public class BatchDetailsViewModel : BindableBase
    {
        private readonly IFileShareApiAdminClientFactory fileShareApiAdminClientFactory;
        private readonly IMessageBoxService messageBoxService;
        public CancellationTokenSource? CancellationTokenSource;
        private readonly ILogger<BatchDetailsViewModel> logger;

        public BatchDetailsViewModel(IFileShareApiAdminClientFactory fileShareApiAdminClientFactory, 
            IMessageBoxService messageBoxService, ILogger<BatchDetailsViewModel> logger)
        {
            this.fileShareApiAdminClientFactory = fileShareApiAdminClientFactory;
            this.messageBoxService = messageBoxService;
            this.logger = logger;
            this.DownloadExecutionCommand = new DelegateCommand<string>(OnDownloadExecutionCommand);
        }

        private async void OnDownloadExecutionCommand(string fileName)
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = CancellationTokenSource.Token;
            long fileSizeInBytes = (long)Files.Find(x => x.Filename == fileName).FileSize;

            var downloadLocation = GetDownloadLocation(fileName);
            if (String.IsNullOrWhiteSpace(downloadLocation)) return;
           
            downloadLocation = Path.Combine(downloadLocation, fileName);
            try
            {
                var fssClient = fileShareApiAdminClientFactory.Build();
                var response = await fssClient.DownloadFileAsync(BatchId, fileName, downloadLocation, fileSizeInBytes, cancellationToken);

                MessageBox.Show($"Download completed for file {fileName} and BatchId {BatchId}.", "Information", MessageBoxButton.OK,MessageBoxImage.Information);
            }
            catch(Exception ex)
            {
             
            }
            
        }

        public DelegateCommand<string> DownloadExecutionCommand { get; set; }

        public string BatchId { get; set; }
        public List<BatchDetailsAttributes> Attributes { get; set; }
        public DateTime? BatchPublishedDate { get; set; }
        public List<BatchDetailsFiles> Files { get; set; }

        #region private methods
        // This should be helper class
        private string GetDownloadLocation(string fileName)
        {
            string downloadLocation = string.Empty;
            //MessageBoxService messageBoxService = new MessageBoxService();

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
                //  Our final value is in path
                downloadLocation = path;
                File.Delete(fileName);
            }

            return downloadLocation;
        }
        #endregion

    }
}
