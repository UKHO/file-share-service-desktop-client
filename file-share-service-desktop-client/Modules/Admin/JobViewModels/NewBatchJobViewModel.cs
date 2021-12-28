using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareClient.Models;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Core.Models;
using UKHO.FileShareService.DesktopClient.Helper;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class NewBatchJobViewModel : BaseBatchJobViewModel
    {
        private const double MaxBatchCommitWaitTime = 60;
        private readonly NewBatchJob job;
        private readonly IFileSystem fileSystem;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;
        private readonly ICurrentDateTimeProvider currentDateTimeProvider;
        private readonly IMacroTransformer macroTransformer;
        private readonly ILogger<NewBatchJobViewModel> logger;
        private IBatchHandle? batchHandle;
        private bool isCanceled;
        private bool IsCommittingOnCancel = false;
        CancellationTokenSource? CancellationTokenSource;
        private readonly IDateTimeValidator dateTimeValidator;

        public NewBatchJobViewModel(NewBatchJob job, IFileSystem fileSystem,
             ILogger<NewBatchJobViewModel> logger,
            Func<IFileShareApiAdminClient> fileShareClientFactory,
            ICurrentDateTimeProvider currentDateTimeProvider,
            IMacroTransformer macroTransformer,
            IDateTimeValidator dateTimeValidator) : base(job, logger)
        {
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
            this.job = job;
            this.fileSystem = fileSystem;
            this.fileShareClientFactory = fileShareClientFactory;
            this.logger = logger;
            this.currentDateTimeProvider = currentDateTimeProvider;
            this.macroTransformer = macroTransformer;
            this.dateTimeValidator = dateTimeValidator;
            Files = job.ActionParams.Files != null ?
                job.ActionParams.Files.Select(f => new NewBatchFilesViewModel(f, fileSystem, macroTransformer.ExpandMacros)).ToList()
                    : new List<NewBatchFilesViewModel>();

            CancelJobExecutionCommand = new DelegateCommand(OnCancelJobCommand, () => !IsCanceled);
        }

        public DelegateCommand CancelJobExecutionCommand { get; }

        public List<KeyValueAttribute>? Attributes => job.ActionParams.Attributes?
            .Where(att => att != null)?
            .Select(k => new KeyValueAttribute(k.Key, macroTransformer.ExpandMacros(k.Value)))?
            .ToList();

        public string BusinessUnit => job.ActionParams.BusinessUnit;

        public bool IsExpiryDateKeyExist => job.IsExpiryDateKeyExist;

        public string RawExpiryDate => job.ActionParams.ExpiryDate;

        private DateTime? expiryDate = null;

        public string? ExpiryDate
        {
            get
            {
                if (!expiryDate.HasValue)
                {
                    expiryDate = dateTimeValidator.ValidateExpiryDate(IsExpiryDateKeyExist, RFC3339_FORMATS, RawExpiryDate, job.ErrorMessages);
                }

                return expiryDate.HasValue ?
                    ConvertToRFC3339Format(expiryDate.Value.ToUniversalTime()) : null;
            }
        }

        protected override bool CanExecute()
        {
            ValidationErrors.Clear();
            //Validate view model
            ValidateViewModel();

            ValidationErrors = job.ErrorMessages;
            for (int i = 0; i < ValidationErrors.Count; i++)
            {
                logger.LogError("Configuration Error : {ValidationErrors} for Action : {Action}, displayName:{displayName}. ", ValidationErrors[i].ToString(), Action, DisplayName);
            }

            return !ValidationErrors.Any();
        }

        public List<NewBatchFilesViewModel> Files { get; }

        public List<string> ReadUsers => job.ActionParams.Acl.ReadUsers;
        public List<string> ReadGroups => job.ActionParams.Acl.ReadGroups;

        public bool IsCanceled
        {
            get => isCanceled;
            set
            {
                if (isCanceled != value)
                {
                    isCanceled = value;
                    RaisePropertyChanged();
                    CancelJobExecutionCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<FileUploadProgressViewModel> FileUploadProgress { get; } = new();

        protected internal override async Task OnExecuteCommand()
        {
            IsExecuting = true;
            IsCanceled = false;
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = CancellationTokenSource.Token;

            try
            {
                logger.LogInformation("Execute job started for Action : {Action} and displayName :{displayName} .", Action, DisplayName);
                var fileShareClient = fileShareClientFactory();
                var buildBatchModel = BuildBatchModel();
                logger.LogInformation("File Share Service batch create started.");
                var createBatchResult = await fileShareClient.CreateBatchAsync(buildBatchModel, CancellationToken.None);
                if (createBatchResult.IsSuccess)
                {
                    batchHandle = createBatchResult.Data;
                    ExecutionResult = $"File Share Service batch create completed for batch ID: {batchHandle.BatchId}";
                    logger.LogInformation("File Share Service batch create completed for batch ID:{BatchId}.", createBatchResult.Data.BatchId);
                    bool isAddFileToBatchError = false;

                    FileUploadProgress.Clear();
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.WhenAll(
                            Files.SelectMany(f => f.Files.Select(file => (f, file)))
                                .Select(f =>
                                {
                                    logger.LogInformation("File Share Service upload files started for file:{file} and BatchId:{BatchId} .", f.file.Name, createBatchResult.Data.BatchId);
                                    var fileUploadProgressViewModel = new FileUploadProgressViewModel(f.file.Name);
                                    FileUploadProgress.Add(fileUploadProgressViewModel);

                                    var (newBatchFilesViewModel, file) = f;
                                    var openRead = fileSystem.File.OpenRead(file.FullName);

                                    return fileShareClient.AddFileToBatch(batchHandle, openRead, file.Name,
                                        newBatchFilesViewModel.MimeType,
                                        progress =>
                                        {
                                            Application.Current.Dispatcher.Invoke(() =>
                                            {
                                                fileUploadProgressViewModel.CompleteBlocks = progress.blocksComplete;
                                                fileUploadProgressViewModel.TotalBlocks = progress.totalBlockCount;
                                            });
                                            if (fileUploadProgressViewModel.CompleteBlocks == fileUploadProgressViewModel.TotalBlocks)
                                            {
                                                logger.LogInformation("File Share Service upload files completed for file:{file} and BatchId:{BatchId} .", f.file.Name, batchHandle.BatchId);
                                            }
                                        }, cancellationToken, newBatchFilesViewModel.Attributes?.Select(k => new KeyValuePair<string, string>(k.Key, k.Value)).ToArray())
                                    .ContinueWith(
                                        res =>
                                        {
                                            if (res.Result.IsSuccess)
                                            {
                                                ExecutionResult = $"File Share Service add file to batch completed for file:{f.file.Name} and BatchId:{batchHandle.BatchId}";
                                            }
                                            else if (res.Result.Errors != null && res.Result.Errors.Any())
                                            {                                                
                                                ExecutionResult = (res.Result.Errors != null && res.Result.Errors.Any()) ?
                        string.Join(Environment.NewLine, res.Result.Errors.Select(e => e.Description)) :
                        $"File Share Service add file to batch failed for batch ID:{batchHandle.BatchId} with status: {res.Result.StatusCode}.";

                                                logger.LogError("File Share Service add file to batch failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                                                    DisplayName, batchHandle.BatchId, ExecutionResult);

                                                isAddFileToBatchError = true;
                                                CancellationTokenSource.Cancel();
                                            }
                                        })
                                    .ContinueWith(_ => openRead.Dispose());
                                }).ToArray());
                        //cleaning up file progress as all uploaded

                        FileUploadProgress.Clear();
                        cancellationToken.ThrowIfCancellationRequested();

                        logger.LogInformation("File Share Service batch commit started for batch ID:{BatchId}.", batchHandle.BatchId);
                        ExecutionResult = $"Files uploaded, batch commit in progress. New batch ID: {batchHandle.BatchId}";
                        IsCommitting = !IsCanceled;
                        IsCommittingOnCancel = true;

                        var result = await fileShareClient.CommitBatch(batchHandle, CancellationToken.None);

                        if (result.IsSuccess)
                        {
                            ExecutionResult = $"File Share Service batch commit completed for batch ID: {batchHandle.BatchId}";
                        }
                        else
                        {
                            ExecutionResult = (result.Errors != null && result.Errors.Any()) ?
                                string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)) :
                                $"File Share Service batch commit failed for batch ID:{batchHandle.BatchId} with status: {result.StatusCode}.";

                            logger.LogError("File Share Service batch commit failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                                DisplayName, batchHandle.BatchId, ExecutionResult);
                        }

                        if (!IsCanceled)
                        {
                            ExecutionResult = !await CheckBatchIsCommitted(fileShareClient, batchHandle, MaxBatchCommitWaitTime)
                                ? $"Batch didn't committed in expected time. Please contact support team. New batch ID: {batchHandle.BatchId}"
                                : $"Batch uploaded. New batch ID: {batchHandle.BatchId}";

                            logger.LogInformation("Execute job completed for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.", Action, DisplayName, batchHandle.BatchId);
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    catch (OperationCanceledException)
                    {
                        if (isAddFileToBatchError)
                        {
                            await RollBackBatch(batchHandle, fileShareClient);
                            ExecutionResult = $"File Share Service - error while uploading files for batch ID: {batchHandle?.BatchId}";
                        }
                        else
                            ExecutionResult = await HandleCanceledOperationsAsync(batchHandle, IsCommittingOnCancel, fileShareClient);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e.ToString());

                        if (batchHandle != null)
                        {
                            logger.LogInformation("File Share Service batch rollback started for batch ID:{BatchId}.", batchHandle?.BatchId);
                            CancellationTokenSource.Dispose();
                            await RollBackBatch(batchHandle, fileShareClient);                            
                        }
                        ExecutionResult = e.Message;
                    }
                }
                else
                {
                    ExecutionResult = (createBatchResult.Errors != null && createBatchResult.Errors.Any()) ?
                        string.Join(Environment.NewLine, createBatchResult.Errors.Select(e => e.Description)) :
                        $"File Share Service batch create failed for batch ID:{createBatchResult.Data.BatchId} with status: {createBatchResult.StatusCode}.";

                    logger.LogError("File Share Service batch create failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                        DisplayName, createBatchResult.Data.BatchId, ExecutionResult);
                }
            }
            finally
            {
                IsCommitting = false;
                IsExecuting = false;
                IsExecutingComplete = true;
                IsCanceled = false;
                IsCommittingOnCancel = false;
                batchHandle = null;
                CancellationTokenSource.Dispose();
            }
        }

        private void OnCancelJobCommand()
        {
            IsCanceled = false;
            logger.LogInformation("Cancel job requested for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.", Action, DisplayName, batchHandle?.BatchId);
            string msg = batchHandle != null ? string.Format("Are you sure you want to cancel this job batch ID: {0}?", batchHandle.BatchId) : "Are you sure you want to cancel this job?";
            MessageBoxResult result = MessageBox.Show(msg, "Cancel Job Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                if (CancellationTokenSource != null && !CancellationTokenSource.IsCancellationRequested)
                {
                    logger.LogInformation("Cancel job confirmed for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.", Action, DisplayName, batchHandle?.BatchId);
                    IsCanceled = true;
                    IsCommitting = false;
                    CancellationTokenSource.Cancel();
                }
            }
            else
            {
                logger.LogInformation("Cancel job canceled for Action : {Action} and displayName :{displayName} and batch ID:{BatchId}.", Action, DisplayName, batchHandle?.BatchId);
            }
        }

        private async Task<string> HandleCanceledOperationsAsync(IBatchHandle batchHandle, bool IsCommitting, IFileShareApiAdminClient fileShareClient)
        {
            logger.LogInformation("Cancel job execution started for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.", Action, DisplayName, batchHandle.BatchId);
            try
            {
                if (IsCommitting)
                {
                    await SetBatchExpiry(batchHandle, fileShareClient);
                }
                else
                {
                    try
                    {
                        logger.LogInformation("File Share Service batch rollback started for batch ID:{BatchId}.", batchHandle.BatchId);
                        await RollBackBatch(batchHandle, fileShareClient);
                        logger.LogInformation("File Share Service batch rollback completed for batch ID:{BatchId}.", batchHandle.BatchId);
                    }
                    catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Conflict)
                    {
                        logger.LogInformation("File Share Service batch rollback attempted but got conflict and hence setting expiry for batch ID:{BatchId}.", batchHandle.BatchId);
                        await SetBatchExpiry(batchHandle, fileShareClient);
                    }
                }
                ExecutionResult = string.Format("Canceled job is completed for batch ID:{0}", batchHandle.BatchId);
                logger.LogInformation("Cancel job execution completed for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.", Action, DisplayName, batchHandle.BatchId);
            }
            catch (Exception ex)
            {
                ExecutionResult = ex.ToString();
                logger.LogError("Cancel Job Error : {Error} for Action : {Action}, displayName:{displayName}. ", ex.Message, Action, DisplayName);
                throw;
            }

            return ExecutionResult;
        }

        private async Task RollBackBatch(IBatchHandle batchHandle, IFileShareApiAdminClient fileShareClient)
        {
            logger.LogInformation("File Share Service RollBackBatchAsync started for batch ID:{BatchId}.", batchHandle.BatchId);
            //await fileShareClient.RollBackBatchAsync(batchHandle);
            var rollBackBatchResult = await fileShareClient.RollBackBatchAsync(batchHandle, CancellationToken.None);
            if (rollBackBatchResult.IsSuccess)
            {
                ExecutionResult = $"File Share Service rollback completed for batch ID: {batchHandle?.BatchId}";
            }
            else
            {
                ExecutionResult = (rollBackBatchResult.Errors != null && rollBackBatchResult.Errors.Any()) ?
                    string.Join(Environment.NewLine, rollBackBatchResult.Errors.Select(e => e.Description)) :
                    $"File Share Service rollback failed for batch ID:{batchHandle?.BatchId} with status: {rollBackBatchResult.StatusCode}.";

                logger.LogError("File Share Service rollback failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                    DisplayName, batchHandle?.BatchId, ExecutionResult);
            }
            logger.LogInformation("File Share Service RollBackBatchAsync completed for batch ID:{BatchId}.", batchHandle.BatchId);
        }

        private async Task SetBatchExpiry(IBatchHandle batchHandle, IFileShareApiAdminClient fileShareClient)
        {
            logger.LogInformation("File Share Service SetExpiryDateAsync started for batch ID:{BatchId}.", batchHandle.BatchId);
            DateTime? expiryDateString = DateTime.UtcNow.AddDays(-7);
            //await fileShareClient.SetExpiryDateAsync(batchHandle.BatchId, new BatchExpiryModel { ExpiryDate = expiryDateString }, CancellationToken.None);
            var setExpiryDateResult = await fileShareClient.SetExpiryDateAsync(batchHandle.BatchId, new BatchExpiryModel { ExpiryDate = expiryDateString }, CancellationToken.None);

            if (setExpiryDateResult.IsSuccess)
            {
                ExecutionResult = $"File Share Service set expiry date completed for batch ID: {batchHandle.BatchId}";
            }
            else
            {
                ExecutionResult = (setExpiryDateResult.Errors != null && setExpiryDateResult.Errors.Any()) ?
                    string.Join(Environment.NewLine, setExpiryDateResult.Errors.Select(e => e.Description)) :
                    $"File Share Service set expiry date failed for batch ID:{batchHandle.BatchId} with status: {setExpiryDateResult.StatusCode}.";

                logger.LogError("File Share Service set expiry date failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                    DisplayName, batchHandle.BatchId, ExecutionResult);

            }
            logger.LogInformation("File Share Service SetExpiryDateAsync completed for batch ID:{BatchId}.", batchHandle.BatchId);
        }

        public async Task<bool> CheckBatchIsCommitted(IFileShareApiAdminClient fileShareClient, IBatchHandle batchHandle, double waitTimeInMinutes)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(waitTimeInMinutes))
            {
                BatchStatusResponse status = await fileShareClient.GetBatchStatusAsync(batchHandle);
                if (status?.Status == BatchStatusResponse.StatusEnum.Committed)
                {
                    return true;
                }

                await Task.Delay(10000);
            }
            return false;
        }

        public BatchModel BuildBatchModel()
        {
            return new()
            {
                BusinessUnit = BusinessUnit,
                Acl = new FileShareAdminClient.Models.Acl
                {
                    ReadGroups = ReadGroups,
                    ReadUsers = ReadUsers
                },
                Attributes = Attributes?.Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value))?.ToList(),
                ExpiryDate = expiryDate
            };
        }

        private void ValidateViewModel()
        {
            // Add validations for expiry date.
            if (expiryDate.HasValue && DateTime.Compare(expiryDate.Value.ToUniversalTime(), DateTime.UtcNow) <= 0)
            {
                job.ErrorMessages.Add("Expiry date cannot be a past date.");
            }

            if (Files.Any())
            {
                foreach (var file in Files)
                {
                    string? directory = Path.GetDirectoryName(file.RawSearchPath);

                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        job.ErrorMessages.Add($"Invalid directory specified - '{file.RawSearchPath}'");
                        continue;
                    }

                    if (file.ExpectedFileCount <= 0)
                    {
                        job.ErrorMessages.Add($"File expected count is missing or invalid in file path '{file.RawSearchPath}'");
                        continue;
                    }

                    if (!IsDirectoryExist(directory))
                    {
                        string directoryNotFoundMessage = $"Directory '{directory}' does not exist or you do not have permission to access the directory selected.";
                        string accessibleDirectory = GetAccessibleDirectoryName(Convert.ToString(fileSystem.DirectoryInfo.FromDirectoryName(directory).Parent));

                        if (!string.IsNullOrWhiteSpace(accessibleDirectory))
                        {
                            directoryNotFoundMessage = $"{directoryNotFoundMessage}\n\tThe level you can access is: '{accessibleDirectory}'";
                        }


                        job.ErrorMessages.Add(directoryNotFoundMessage);
                        continue;
                    }

                    if (!file.CorrectNumberOfFilesFound)
                    {
                        string fileCountMismatchErrorMessage = $"Expected file count is {file.ExpectedFileCount}, actual file count is {file.Files?.Count()} in file path '{file.RawSearchPath}'.";

                        if (file.Files?.Count() > 0)
                        {
                            string existingFileNames = string.Join(", ", file.Files.Select(f => f.Name).ToArray());
                            fileCountMismatchErrorMessage += $"\n\tThe existing files are: {existingFileNames}";
                        }
                        job.ErrorMessages.Add($"{fileCountMismatchErrorMessage}");
                    }
                }
            }
        }
        private string GetAccessibleDirectoryName(string? directory)
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                if (IsDirectoryExist(directory))
                {
                    return directory;
                }
                else
                {
                    if (fileSystem.DirectoryInfo.FromDirectoryName(directory).Parent != null)
                    {
                        return GetAccessibleDirectoryName(Convert.ToString(fileSystem.DirectoryInfo.FromDirectoryName(directory).Parent));
                    }
                }
            }
            return string.Empty;
        }

        private bool IsDirectoryExist(string directory)
        {
            try
            {
                _ = fileSystem.DirectoryInfo.FromDirectoryName(directory).GetDirectories();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class NewBatchFilesViewModel
    {
        private readonly NewBatchFiles newBatchFile;

        public NewBatchFilesViewModel(NewBatchFiles newBatchFile, IFileSystem fileSystem,
            Func<string, string> expandMacros)
        {
            this.newBatchFile = newBatchFile;
            SearchPath = expandMacros(newBatchFile.SearchPath);
            var searchFileInfo = string.IsNullOrWhiteSpace(SearchPath) ? null : fileSystem.FileInfo.FromFileName(SearchPath);
            var directory = searchFileInfo == null ? null : fileSystem.DirectoryInfo.FromDirectoryName(searchFileInfo.DirectoryName);


            Files = (directory != null && directory.Exists)
                    ? GetFiles(directory, searchFileInfo.Name)
                    : Enumerable.Empty<IFileSystemInfo>();

            Attributes = this.newBatchFile.Attributes?
                .Where(att => att != null)?
                .Select(k => new KeyValueAttribute(k.Key, expandMacros(k.Value)))?
                .ToList();
        }

        public string RawSearchPath => newBatchFile.SearchPath;
        public string SearchPath { get; }
        public IEnumerable<IFileSystemInfo> Files { get; }
        public int ExpectedFileCount => newBatchFile.ExpectedFileCount;
        public string MimeType => newBatchFile.MimeType;

        public bool CorrectNumberOfFilesFound => ExpectedFileCount == Files.Count();
        public List<KeyValueAttribute>? Attributes { get; }

        private IEnumerable<IFileSystemInfo> GetFiles(IDirectoryInfo directory, string filePathName)
        {
            try
            {
                return directory.EnumerateFileSystemInfos(filePathName);

            }
            catch (DirectoryNotFoundException)
            {
                return Enumerable.Empty<IFileSystemInfo>();
            }
        }
    }
}