using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareClient.Models;
using UKHO.FileShareService.DesktopClient.Events;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{

    public class BatchDetailsViewModel : BindableBase
    {
        private readonly IFileShareApiAdminClientFactory fileShareApiAdminClientFactory;
        private readonly IMessageBoxService messageBoxService;
        public CancellationTokenSource? CancellationTokenSource;
        private readonly IFileService fileService;
        private readonly ISaveFileDialogService saveFileDialogService;
        private readonly IEventAggregator eventAggregator;

        public BatchDetailsViewModel(IFileShareApiAdminClientFactory fileShareApiAdminClientFactory, 
            IMessageBoxService messageBoxService, IFileService fileService , ISaveFileDialogService saveFileDialogService,
            IEventAggregator eventAggregator
            )
        {
            this.fileShareApiAdminClientFactory = fileShareApiAdminClientFactory;
            this.messageBoxService = messageBoxService;
            this.fileService = fileService;
            this.saveFileDialogService = saveFileDialogService;
            this.DownloadExecutionCommand = new DelegateCommand<string>(OnDownloadExecutionCommand);
            this.ExpireBatchExecutionCommand = new DelegateCommand<string>(OnExpireBatchExecutionCommand);
            this.eventAggregator = eventAggregator;
        }

        private async void OnDownloadExecutionCommand(string fileName)
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = CancellationTokenSource.Token;
            long fileSizeInBytes = (long)Files!.Find(x => x.Filename == fileName)!.FileSize!;

            var downloadLocation = saveFileDialogService.SaveFileDialog(fileName);
            if (string.IsNullOrWhiteSpace(downloadLocation)) return;
            downloadLocation = Path.Combine(downloadLocation, fileName);

            var result= await DownloadFile(BatchId!, downloadLocation, fileName,fileSizeInBytes, cancellationToken);
           
            if(result!=null && result.IsSuccess)
            {
                messageBoxService.ShowMessageBox("Information", $"Download completed for file {fileName} and BatchId {BatchId}.", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void OnExpireBatchExecutionCommand(string batchId)
        {
            var now = DateTime.Now;

            try
            {
                if (messageBoxService.ShowMessageBox("Expire Batch", $"Batch will be expired and no longer be accessible. \nDo you want to continue?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    var fssClient = fileShareApiAdminClientFactory.Build();
                    var result = await fssClient.SetExpiryDateAsync(batchId, new BatchExpiryModel { ExpiryDate = now }, CancellationToken.None);

                    if (result.IsSuccess)
                    {
                        eventAggregator.GetEvent<BatchExpiredEvent>().Publish();
                    }
                    else
                    {
                        var message = (result.Errors != null && result.Errors.Any()) ?
                        string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)) :
                        $"File Share Service set expiry date failed for batch ID:{batchId} with status: {result.StatusCode}.";

                        messageBoxService.ShowMessageBox("Error", message, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {   
                messageBoxService.ShowMessageBox("Error", $"File Share Service set expiry date failed for batch ID: {batchId}. \n Error: {ex.Message}", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public DelegateCommand<string> DownloadExecutionCommand { get; set; }
        public DelegateCommand<string> ExpireBatchExecutionCommand { get; set; }
        
        public string? BatchId { get; set; }
        public List<BatchDetailsAttributes>? Attributes { get; set; }
        public DateTime? BatchPublishedDate { get; set; }
        public List<BatchDetailsFiles>? Files { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool CanSetBatchExpiryDate { get; set; }        

        public async Task <IResult<DownloadFileResponse>> DownloadFile(string BatchId ,string fileDownloadPath, string fileName, long fileSizeInBytes,CancellationToken cancellationToken)
        {
            if (fileService.Exists(fileDownloadPath) && messageBoxService.ShowMessageBox($"Confirmation for fileName: {fileName}", $"{fileName} already exists in selected directory. Do you want to replace it ?",
                  MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return null!;
            }

            using var fileStream = new FileStream(fileDownloadPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
           
            var fssClient = fileShareApiAdminClientFactory.Build();
            var response = await fssClient.DownloadFileAsync(BatchId, fileName, fileStream, fileSizeInBytes, cancellationToken);
            fileStream.Close();           
            return response;
        }
    }
}
