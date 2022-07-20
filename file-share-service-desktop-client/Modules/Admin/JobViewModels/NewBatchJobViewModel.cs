using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
        private readonly IMacroTransformer macroTransformer;
        private readonly ILogger<NewBatchJobViewModel> logger;
        private IBatchHandle? batchHandle;
        private bool isCanceled;
        private bool IsCommittingOnCancel = false;
        private readonly IDateTimeValidator dateTimeValidator;
        private readonly IMessageBoxService messageBoxService;
        public CancellationTokenSource? CancellationTokenSource;

        public NewBatchJobViewModel(NewBatchJob job, IFileSystem fileSystem,
        ILogger<NewBatchJobViewModel> logger,
        Func<IFileShareApiAdminClient> fileShareClientFactory,
        ICurrentDateTimeProvider currentDateTimeProvider,
        IMacroTransformer macroTransformer,
        IDateTimeValidator dateTimeValidator,
        IMessageBoxService messageBoxService
        ) : base(job, logger)
        {
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
            this.job = job;
            this.fileSystem = fileSystem;
            this.fileShareClientFactory = fileShareClientFactory;
            this.logger = logger;
            this.macroTransformer = macroTransformer;
            this.dateTimeValidator = dateTimeValidator;
            this.messageBoxService = messageBoxService;
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

        public DateTime? ExpiryDate
        {
            get
            {
                if (!expiryDate.HasValue)
                {
                    expiryDate = dateTimeValidator.ValidateExpiryDate(IsExpiryDateKeyExist, RFC3339_FORMATS, RawExpiryDate, job.ErrorMessages);
                }

                return expiryDate.HasValue ?
                expiryDate.Value.ToUniversalTime() : null;
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
                BatchSearchResponse batchSearchResponse = await SearchBatch();
                logger.LogInformation($"{ batchSearchResponse.Count} duplicate batches found for action: {Action} and displayName: {DisplayName}.");
                if (batchSearchResponse.Total > 0 && messageBoxService.ShowMessageBox($"Confirmation for displayName: {DisplayName}", $"{batchSearchResponse.Count} duplicate batches found. Do you still want to continue to execute the job ?",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    logger.LogInformation($"File Share Service create new batch cancelled for action: {Action} and displayName: {DisplayName}, because {batchSearchResponse.Count} duplicate batches found.");
                    ExecutionResult = $"File Share Service create new batch cancelled. ";
                }
                else
                {
                    logger.LogInformation("File Share Service batch create started.");
                    var createBatchResult = await fileShareClient.CreateBatchAsync(buildBatchModel, CancellationToken.None);
                    if (createBatchResult.IsSuccess && createBatchResult.Data is IBatchHandle batchHandle)
                    {
                        ExecutionResult = $"File Share Service batch create completed for batch ID: {batchHandle.BatchId}";
                        logger.LogInformation("File Share Service batch create completed for batch ID:{BatchId}.", batchHandle.BatchId);
                        bool isAddFileToBatchError = false;
                        FileUploadProgress.Clear();
                        try
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await Task.WhenAll(
                            Files.SelectMany(f => f.Files.Select(file => (f, file)))
                            .Select(async f =>
                            {
                                logger.LogInformation("File Share Service upload files started for file:{file} and BatchId:{BatchId} .", f.file.Name, batchHandle.BatchId);
                                var fileUploadProgressViewModel = new FileUploadProgressViewModel(f.file.Name);
                                FileUploadProgress.Add(fileUploadProgressViewModel);

                                var (newBatchFilesViewModel, file) = f;
                                using var openRead = fileSystem.File.OpenRead(file.FullName);
                                var res = await fileShareClient.AddFileToBatch(batchHandle, openRead, file.Name, newBatchFilesViewModel.MimeType,
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
                                    },
                                    cancellationToken,
                                    newBatchFilesViewModel.Attributes?.Select(k => new KeyValuePair<string, string>(k.Key, k.Value)).ToArray());

                                if (res.IsSuccess)
                                {
                                    ExecutionResult = $"File Share Service add file to batch completed for file:{f.file.Name} and BatchId:{batchHandle.BatchId}";
                                    logger.LogInformation("File Share Service add file to batch completed for file:{file} and BatchId:{BatchId} .", f.file.Name, batchHandle.BatchId);
                                }
                                else
                                {
                                    ExecutionResult = GetErrors(res,
                                        $"File Share Service add file to batch failed for batch ID:{batchHandle.BatchId} with status: {res.StatusCode}.");

                                    logger.LogError("File Share Service add file to batch failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                                        DisplayName, batchHandle.BatchId, ExecutionResult);

                                    isAddFileToBatchError = true;
                                    CancellationTokenSource.Cancel();
                                }
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
                                logger.LogInformation("File Share Service batch commit completed for batch ID:{BatchId}.", batchHandle.BatchId);
                                if (!IsCanceled)
                                {
                                    ExecutionResult = !await CheckBatchIsCommitted(fileShareClient, batchHandle, MaxBatchCommitWaitTime)
                                    ? $"Batch didn't committed in expected time. Please contact support team. New batch ID: {batchHandle.BatchId}"
                                    : $"Batch uploaded. New batch ID: {batchHandle.BatchId}";

                                    logger.LogInformation("Execute job completed for Action : {Action}, displayName:{displayName} and batch ID:{BatchId}.", Action, DisplayName, batchHandle.BatchId);
                                }
                            }
                            else
                            {
                                await RollBackBatch(batchHandle, fileShareClient);
                                ExecutionResult = GetErrors(result,
                                $"File Share Service batch commit failed for batch ID:{batchHandle.BatchId} with status: {result.StatusCode}.");

                                logger.LogError("File Share Service batch commit failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                                    DisplayName, batchHandle.BatchId, ExecutionResult);
                            }

                            cancellationToken.ThrowIfCancellationRequested();

                        }
                        catch (OperationCanceledException)
                        {
                            if (isAddFileToBatchError)
                            {
                                await RollBackBatch(batchHandle, fileShareClient);
                            }
                            else
                                ExecutionResult = await HandleCanceledOperationsAsync(batchHandle, IsCommittingOnCancel, fileShareClient);
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e.ToString());

                            if (batchHandle != null)
                            {
                                logger.LogInformation("File Share Service batch rollback started for batch ID:{BatchId}.", batchHandle.BatchId);
                                CancellationTokenSource.Dispose();
                                await RollBackBatch(batchHandle, fileShareClient);
                                logger.LogInformation("File Share Service batch rollback completed for batch ID:{BatchId}.", batchHandle.BatchId);
                            }
                            ExecutionResult = e.Message;
                        }
                    }
                    else
                    {
                        ExecutionResult = GetErrors(createBatchResult,
                            $"File Share Service batch create failed with status: {createBatchResult.StatusCode}.");

                        if (createBatchResult.StatusCode == (int)HttpStatusCode.Forbidden)
                        {
                            ExecutionResult += $"{Environment.NewLine}{BusinessUnitPermissionHint(buildBatchModel.BusinessUnit)}";
                        }

                        logger.LogError("File Share Service batch create failed for displayName:{DisplayName} with error:{responseMessage}.",
                            DisplayName, ExecutionResult);
                    }
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
            MessageBoxResult result = messageBoxService.ShowMessageBox("Cancel Job Confirmation", msg, MessageBoxButton.YesNo, MessageBoxImage.Question);
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
            logger.LogInformation("File Share Service RollBackBatchAsync started for batch ID:{BatchId}.", batchHandle?.BatchId);
            var rollBackBatchResult = await fileShareClient.RollBackBatchAsync(batchHandle, CancellationToken.None);
            if (rollBackBatchResult.IsSuccess)
            {
                logger.LogInformation("File Share Service RollBackBatchAsync completed for batch ID:{BatchId}.", batchHandle?.BatchId);
            }
            else
            {
                logger.LogError("File Share Service rollback failed for displayName:{DisplayName} and batch ID:{BatchId} with error:{responseMessage}.",
                    DisplayName, batchHandle?.BatchId, ExecutionResult);
            }
            
        }

        private async Task SetBatchExpiry(IBatchHandle batchHandle, IFileShareApiAdminClient fileShareClient)
        {
            logger.LogInformation("File Share Service SetExpiryDateAsync started for batch ID:{BatchId}.", batchHandle.BatchId);
            var setBatchExpiryResult = await fileShareClient.SetExpiryDateAsync(batchHandle.BatchId, new BatchExpiryModel { ExpiryDate = ExpiryDate }, CancellationToken.None);
            if (setBatchExpiryResult.IsSuccess)
            {
                ExecutionResult = $"File Share Service set expiry date completed for batch ID: {batchHandle.BatchId}";
            }
            else
            {
                ExecutionResult = GetErrors(setBatchExpiryResult,
                    $"File Share Service set expiry date failed for batch ID:{ batchHandle.BatchId} with status: {setBatchExpiryResult.StatusCode}.");

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
                    string? directory = Path.GetDirectoryName(file.SearchPath);

                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        job.ErrorMessages.Add($"Directory not specified or invalid directory specified - '{file.SearchPath}' (from raw: '{file.RawSearchPath}')");
                        continue;
                    }

                    if (!file.ExpectedFileCountIsValid)
                    {                        
                        job.ErrorMessages.Add($"Expected file count value '{file.ExpectedFileCount}' is invalid for file path '{file.SearchPath}'. '*' or positive non-zero integer value allowed");
                        continue;
                    }

                    if (!IsDirectoryExist(directory))
                    {
                        string directoryNotFoundMessage = $"Directory '{directory}' (from raw: '{file.RawSearchPath}') does not exist or you do not have permission to access the directory.";
                        string accessibleDirectory = GetAccessibleDirectoryName(Convert.ToString(fileSystem.DirectoryInfo.FromDirectoryName(directory).Parent));

                        if (!string.IsNullOrWhiteSpace(accessibleDirectory))
                        {
                            directoryNotFoundMessage = $"{directoryNotFoundMessage}\n\tThe level you can access is: '{accessibleDirectory}'";

                            var directories = GetDirectories(accessibleDirectory);

                            string existingDirectoryNames = directories.Any() ?
                                string.Concat($" and available subdirectories are :\n\t\t", string.Join("\n\t\t", directories.Select(dir => dir.Name))) :
                                $"\n\tNo subdirectories exist in directory '{accessibleDirectory}'";

                            directoryNotFoundMessage = string.Concat(directoryNotFoundMessage, existingDirectoryNames);
                        }

                        job.ErrorMessages.Add(directoryNotFoundMessage);
                        continue;
                    }
                                        
                    if (!file.CorrectNumberOfFilesFound)
                    {
                        string fileCountMismatchErrorMessage = $"Expected file count is {file.ExpectedFileCount}, actual file count is {file.Files?.Count()} in file path '{file.SearchPath}' (from raw: '{file.RawSearchPath}').";

                        var directories = GetDirectories(directory);
                        var files = GetFiles(directory);

                        string existingDirectoryNames = directories.Any() ?
                            $"\n\tThe subdirectories in directory '{directory}' are:\n\t\t" + string.Join("\n\t\t", directories.Select(dir => dir.Name)) :
                            $"\n\tNo subdirectories exist in directory '{directory}'";

                        string existingFileNames = files.Any() ?
                            $"\n\tThe files in directory '{directory}' are:\n\t\t" + string.Join("\n\t\t", files.Select(f => f.Name)) :
                            $"\n\tNo files exist in directory '{directory}'";

                        fileCountMismatchErrorMessage = string.Concat(fileCountMismatchErrorMessage, existingDirectoryNames, existingFileNames);

                        job.ErrorMessages.Add(fileCountMismatchErrorMessage);
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

        private async Task<BatchSearchResponse> SearchBatch()
        {
            var filter = BuildSearchQuery();
            var fileShareClient = fileShareClientFactory();

            return await fileShareClient.Search(filter, null, null);
        }

        private string BuildSearchQuery()
        {
            var queryBuilder = new StringBuilder();

            queryBuilder.Append($"businessunit eq '{BusinessUnit}'");

            if (Attributes != null && Attributes.Any())
            {
                string subqueryBatchAttributes = string.Join(" and ", Attributes.Select(k => $"$batch({k.Key}) eq '{k.Value}'"));

                queryBuilder.Append(" and " + subqueryBatchAttributes);
            }


            return queryBuilder.ToString();
        }

        private IDirectoryInfo[] GetDirectories(string directory)
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return fileSystem.DirectoryInfo.FromDirectoryName(directory).GetDirectories();
            }
            return Array.Empty<IDirectoryInfo>();
        }

        private IFileInfo[] GetFiles(string directory)
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                return fileSystem.DirectoryInfo.FromDirectoryName(directory).GetFiles();
            }
            return Array.Empty<IFileInfo>();
        }

        private string GetErrors<T>(FileShareClient.Models.IResult<T> apiResult, string altMessage)
        {
            string errMsg = "New batch job execution failed: ";
            string responseErrors = (apiResult.Errors != null && apiResult.Errors.Any()) ?
            string.Join(Environment.NewLine, apiResult.Errors.Select(e => e.Description)) :
            altMessage;
            return errMsg + responseErrors;
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

            ExpectedFileCountIsValid = newBatchFile.ExpectedFileCount == "*" || (int.TryParse(newBatchFile.ExpectedFileCount, out var fileCount) && 0 < fileCount);

            Files = (directory != null && directory.Exists && searchFileInfo != null)
                ? GetFiles(directory, searchFileInfo.Name)
                : Array.Empty<IFileSystemInfo>();
                        
            CorrectNumberOfFilesFound = ("*" == newBatchFile.ExpectedFileCount && 0 < Files.Length) || Files.Length.ToString() == newBatchFile.ExpectedFileCount;

            Attributes = this.newBatchFile.Attributes?
                .Where(att => att != null)?
                .Select(k => new KeyValueAttribute(k.Key, expandMacros(k.Value)))?
                .ToList();
        }

        public string RawSearchPath => newBatchFile.SearchPath;
        public string SearchPath { get; }
        public IFileSystemInfo[] Files { get; }
        public string ExpectedFileCount => newBatchFile.ExpectedFileCount;
        public bool ExpectedFileCountIsValid { get; set; }
        public string MimeType => newBatchFile.MimeType;
        public bool CorrectNumberOfFilesFound { get; private set; }
        public List<KeyValueAttribute>? Attributes { get; }

        private IFileSystemInfo[] GetFiles(IDirectoryInfo directory, string searchPattern)
        {
            try
            {
                return directory.EnumerateFileSystemInfos(searchPattern).ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<IFileSystemInfo>();
            }
        }
    }
}