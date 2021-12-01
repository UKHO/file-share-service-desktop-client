using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareClient.Models;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
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
        private readonly IDateTimeValidator dateTimeValidator;

        public NewBatchJobViewModel(NewBatchJob job, IFileSystem fileSystem,
             ILogger<NewBatchJobViewModel> logger,
            Func<IFileShareApiAdminClient> fileShareClientFactory,   
            ICurrentDateTimeProvider currentDateTimeProvider,
            IMacroTransformer macroTransformer,
            IDateTimeValidator dateTimeValidator) : base(job)
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
        }

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

        public IEnumerable<KeyValuePair<string, string>> Attributes =>
            job.ActionParams.Attributes.Select(kv => new KeyValuePair<string, string>(kv.Key, macroTransformer.ExpandMacros(kv.Value)));

        protected override bool CanExecute()
        {
            ValidationErrors.Clear();
            //Validate view model
            ValidateViewModel();

            ValidationErrors = job.ErrorMessages;

            return !ValidationErrors.Any();
        }

        public List<NewBatchFilesViewModel> Files { get; }

        public List<string> ReadUsers => job.ActionParams.Acl.ReadUsers;
        public List<string> ReadGroups => job.ActionParams.Acl.ReadGroups;

        public DelegateCommand CloseExecutionCommand { get; }

        private void OnCloseExecutionCommand()
        {
            ExecutionResult = string.Empty;
            IsExecutingComplete = false;
        }

        public ObservableCollection<FileUploadProgressViewModel> FileUploadProgress { get; } = new();

        protected internal override async Task OnExecuteCommand()
        {
            IsExecuting = true;
            try
            {
                logger.LogInformation("Execute job started for displayName :{displayName} .",DisplayName);
                var fileShareClient = fileShareClientFactory();
                var buildBatchModel = BuildBatchModel();
                logger.LogInformation("File Share Service batch create started.");
                var batchHandle = await fileShareClient.CreateBatchAsync(buildBatchModel);
                logger.LogInformation("File Share Service batch create completed for batch ID:{BatchId}.", batchHandle.BatchId);
                FileUploadProgress.Clear();
                try
                {
                   
                    await Task.WhenAll(
                        Files.SelectMany(f => f.Files.Select(file => (f, file)))
                            .Select(f =>
                            {
                                logger.LogInformation("File Share Service upload files started for file:{file} and BatchId:{BatchId} .", f.file.Name, batchHandle.BatchId);
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
                                            logger.LogInformation("File Share Service upload files completed for file:{file} and BatchId:{BatchId} .", f.file.Name ,batchHandle.BatchId);
                                        }
                                    }).ContinueWith(_ => openRead.Dispose());                            
                            }).ToArray());
                    //cleaning up file progress as all uploaded
                    FileUploadProgress.Clear();

                    logger.LogInformation("File Share Service batch commit started for batch ID:{BatchId}.",batchHandle.BatchId);
                    ExecutionResult = $"Files uploaded, batch commit in progress. New batch ID: {batchHandle.BatchId}";
                    IsCommitting = true;                
                    await fileShareClient.CommitBatch(batchHandle);
                    logger.LogInformation("File Share Service batch commit completed for batch ID:{BatchId}.", batchHandle.BatchId);

                    ExecutionResult = !await CheckBatchIsCommitted(fileShareClient,batchHandle, MaxBatchCommitWaitTime)
                        ? $"Batch didn't committed in expected time. Please contact support team. New batch ID: {batchHandle.BatchId}"
                        : $"Batch uploaded. New batch ID: {batchHandle.BatchId}";

                    logger.LogInformation("Execute job completed for displayName:{displayName} and batch ID:{BatchId}.",DisplayName, batchHandle.BatchId);
                }
                catch (Exception e)
                {
                    logger.LogError(e.Message);
                    logger.LogInformation("File Share Service batch rollback started for batch ID:{BatchId}.", batchHandle.BatchId);
                    await fileShareClient.RollBackBatchAsync(batchHandle);
                    logger.LogInformation("File Share Service batch rollback completed for batch ID:{BatchId}.", batchHandle.BatchId);
                    ExecutionResult = e.ToString();
                    throw;
                }
            }
            finally
            {
                IsCommitting = false;
                IsExecuting = false;
                IsExecutingComplete = true;
            }
        }

        public async Task<bool> CheckBatchIsCommitted(IFileShareApiAdminClient fileShareClient,IBatchHandle batchHandle, double waitTimeInMinutes)
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
                Attributes = Attributes.ToList(),
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
                    string directory = Path.GetDirectoryName(file.RawSearchPath);

                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        job.ErrorMessages.Add($"Invalid directory specified - '{file.RawSearchPath}'");
                        continue;
                    }

                    if(file.ExpectedFileCount <= 0)
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
           if(!string.IsNullOrWhiteSpace(directory))
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
                _= fileSystem.DirectoryInfo.FromDirectoryName(directory).GetDirectories();
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
        }

        public string RawSearchPath => newBatchFile.SearchPath;
        public string SearchPath { get; }
        public IEnumerable<IFileSystemInfo> Files { get; }
        public int ExpectedFileCount => newBatchFile.ExpectedFileCount;
        public string MimeType => newBatchFile.MimeType;

        public bool CorrectNumberOfFilesFound => ExpectedFileCount == Files.Count();
        
        private IEnumerable<IFileSystemInfo> GetFiles(IDirectoryInfo directory, string filePathName)
        {
            try
            {
                return directory.EnumerateFileSystemInfos(filePathName);

            }
            catch(DirectoryNotFoundException)
            {
                return Enumerable.Empty<IFileSystemInfo>();
            }
        }
    }
}