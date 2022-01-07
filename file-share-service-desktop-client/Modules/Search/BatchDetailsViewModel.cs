using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UKHO.FileShareAdminClient.Models.Response;
using UKHO.FileShareClient.Models;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{

    public class BatchDetailsViewModel : BindableBase
    {
        private readonly IFileShareApiAdminClientFactory fileShareApiAdminClientFactory;
        private readonly IMessageBoxService messageBoxService;
        public CancellationTokenSource? CancellationTokenSource;
        private readonly IFileService fileService;

        public BatchDetailsViewModel(IFileShareApiAdminClientFactory fileShareApiAdminClientFactory, 
            IMessageBoxService messageBoxService, IFileService fileService)
        {
            this.fileShareApiAdminClientFactory = fileShareApiAdminClientFactory;
            this.messageBoxService = messageBoxService;
            this.fileService = fileService;
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

            var result =await DownloadFile(BatchId, downloadLocation, fileName,fileSizeInBytes,cancellationToken);
            if (result.IsSuccess)
            {
                messageBoxService.ShowMessageBox("Information", $"Download completed for file {fileName} and BatchId {BatchId}.", MessageBoxButton.OK, MessageBoxImage.Information);
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
                File.Delete(fileName);
            }

            return downloadLocation;
        }
        #endregion

        public async Task <IResult<DownloadFileResponse>> DownloadFile(string BatchId ,string fileDownloadPath, string fileName, long fileSizeInBytes,CancellationToken cancellationToken)
        {
            if (fileService.Exists(fileDownloadPath) && messageBoxService.ShowMessageBox($"Confirmation for fileName: {fileName}", $"{fileName} already exists in selected directory. Do you want to replace it ?",
                  MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return null!;
            }

            var fileStream = new FileStream(fileDownloadPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

            var fssClient = fileShareApiAdminClientFactory.Build();
            var response = await fssClient.DownloadFileAsync(BatchId, fileName, fileStream, fileSizeInBytes, cancellationToken);
            fileStream.Close();
            return response;
        }
    }
}
