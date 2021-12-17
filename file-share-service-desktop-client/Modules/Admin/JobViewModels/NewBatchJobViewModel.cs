using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
using UKHO.WeekNumberUtils;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class NewBatchJobViewModel : BaseBatchJobViewModel
    {
        private const double MaxBatchCommitWaitTime = 60;
        private readonly NewBatchJob job;
        private readonly IFileSystem fileSystem;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;
        private readonly ICurrentDateTimeProvider currentDateTimeProvider;
        private bool isExecutingComplete;
        private string executionResult = string.Empty;
        private bool isCommitting;
        private readonly ILogger<NewBatchJobViewModel> logger;
        private IBatchHandle? batchHandle;
        private bool isCanceled;
        private bool IsCommittingOnCancel = false;
        CancellationTokenSource? CancellationTokenSource;

        public NewBatchJobViewModel(NewBatchJob job, IFileSystem fileSystem,
             ILogger<NewBatchJobViewModel> logger,
            Func<IFileShareApiAdminClient> fileShareClientFactory,   
            ICurrentDateTimeProvider currentDateTimeProvider
           ) : base(job,logger)
        {
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);            
            this.job = job;
            this.fileSystem = fileSystem;
            this.fileShareClientFactory = fileShareClientFactory;
            this.logger = logger;
            this.currentDateTimeProvider = currentDateTimeProvider;
            Files = job.ActionParams.Files != null ? 
                job.ActionParams.Files.Select(f => new NewBatchFilesViewModel(f, fileSystem, ExpandMacros)).ToList() 
                    : new List<NewBatchFilesViewModel>();

            CancelJobExecutionCommand = new DelegateCommand(OnCancelJobCommand, () => !IsCanceled);
        }

        public DelegateCommand CancelJobExecutionCommand { get; }

        public string BusinessUnit => job.ActionParams.BusinessUnit;

        public string RawExpiryDate => job.ActionParams.ExpiryDate;

        public DateTime? ExpiryDate
        {
            get
            {
                var expandedDateTime = ExpandMacros(RawExpiryDate);
                if (DateTime.TryParse(expandedDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
                    out var result))
                    return result;
                return null;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Attributes =>
            job.ActionParams.Attributes.Select(kv => new KeyValuePair<string, string>(kv.Key, ExpandMacros(kv.Value)));

        private string ExpandMacros(string value)
        {
            Func<Match, string> now_Year = (match) =>
            {                
                return currentDateTimeProvider.CurrentDateTime.Year.ToString();
            };
            Func<Match, string> nowAddDays_Year = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).Year.ToString();
            };
            Func<Match, string> now_WeekNumber = (match) =>
            {
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime).Week.ToString();
            };
            Func<Match, string> now_WeekNumberYear = (match) =>
            {
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime).Year.ToString();
            };
            Func<Match, string> now_WeekNumberPlusWeeks = (match) =>
            {                
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset * 7)).Week.ToString();                
            };
            Func<Match, string> now_WeekNumberPlusWeeksYear = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset * 7)).Year.ToString();
            };
            Func<Match, string> nowAddDays_Week = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)).Week.ToString();
            };
            Func<Match, string> nowAddDays_WeekYear = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)).Year.ToString();
            };
            Func<Match, string> nowAddDays_Date = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)
                    .ToString(CultureInfo.InvariantCulture);
            };
            Func<Match, string> now_Date = (match) =>
            {
                return currentDateTimeProvider.CurrentDateTime.ToString(CultureInfo.InvariantCulture);
            };


            var replacementExpressions = new Dictionary<string, Func<Match, string>>
            {
                {@"\$\(\s*now\.Year\s*\)", now_Year },
                {@"\$\(\s*now\.Year2\s*\)", (match) => now_Year(match).Substring(2,2) },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Year\s*\)", nowAddDays_Year},
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Year2\s*\)", (match) => nowAddDays_Year(match).Substring(2,2)},
                {@"\$\(\s*now\.WeekNumber\s*\)",now_WeekNumber },
                {@"\$\(\s*now\.WeekNumber\.Year\s*\)", now_WeekNumberYear },
                {@"\$\(\s*now\.WeekNumber\.Year2\s*\)", (match) => now_WeekNumberYear(match).Substring(2,2) },
                {@"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\)", now_WeekNumberPlusWeeks },
                {@"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\.Year\)", now_WeekNumberPlusWeeksYear},                
                {@"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\.Year2\)", (match) => now_WeekNumberPlusWeeksYear(match).Substring(2,2) },                   
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\s*\)",nowAddDays_Week },                    
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\.Year\s*\)", nowAddDays_WeekYear },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\.Year2\s*\)", (match) => nowAddDays_WeekYear(match).Substring(2,2)},
                {@"\$\(now.AddDays\(\s*([+-]?\s*\d+)\s*\)\)", nowAddDays_Date },
                {@"\$\(now\)", now_Date}
            };

            if (string.IsNullOrEmpty(value))
                return value;

            return replacementExpressions.Aggregate(value,
                (input, kv) =>
                {
                    var match = Regex.Match(input, kv.Key);
                    while (match.Success)
                    {
                        var end = Math.Min(match.Index + match.Length, input.Length);
                        input = input[..match.Index] +
                                match.Result(kv.Value(match)) +
                                input[end..];

                        match = Regex.Match(input, kv.Key);
                    }

                    return input;
                });
        }

        protected override bool CanExecute()
        {
            ValidationErrors.Clear();
            //Validate files
            ValidateFiles();

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

        public bool IsExecutingComplete
        {
            get => isExecutingComplete;
            set
            {
                if (isExecutingComplete != value)
                {
                    isExecutingComplete = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool IsCommitting
        {
            get => isCommitting;
            set
            {
                if (isCommitting != value)
                {
                    isCommitting = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ExecutionResult
        {
            get => executionResult;
            set
            {
                if (executionResult != value)
                {
                    executionResult = value;
                    RaisePropertyChanged();
                }
            }
        }

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
            IsCanceled = false;
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = CancellationTokenSource.Token;
            try
            {
                logger.LogInformation("Execute job started for Action : {Action} and displayName :{displayName} .",Action,DisplayName);
                var fileShareClient = fileShareClientFactory();
                var buildBatchModel = BuildBatchModel();
                BatchSearchResponse batchSearchResponse = await SearchBatch();
                logger.LogInformation($"{ batchSearchResponse.Count} duplicate batches found.");
                if (batchSearchResponse.Count >0 && MessageBox.Show($"{batchSearchResponse.Count} duplicate batches found. Do you still want to create batch with same configuration?",
                                      "Confirmation", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel)
                {
                    logger.LogInformation("File Share Service create new batch job cancelled.");
                    ExecutionResult = $"File Share Service batch create job cancelled for {batchSearchResponse.Count} duplicate batches found. ";
                }
                else
                {
                    logger.LogInformation("File Share Service batch create started.");
                    batchHandle = await fileShareClient.CreateBatchAsync(buildBatchModel, CancellationToken.None);
                    logger.LogInformation("File Share Service batch create completed for batch ID:{BatchId}.", batchHandle.BatchId);
                    FileUploadProgress.Clear();
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
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
                                                logger.LogInformation("File Share Service upload files completed for file:{file} and BatchId:{BatchId} .", f.file.Name, batchHandle.BatchId);
                                            }
                                        }, cancellationToken).ContinueWith(_ => openRead.Dispose());
                                }).ToArray());
                        //cleaning up file progress as all uploaded
                        FileUploadProgress.Clear();
                        cancellationToken.ThrowIfCancellationRequested();

                        logger.LogInformation("File Share Service batch commit started for batch ID:{BatchId}.", batchHandle.BatchId);
                        ExecutionResult = $"Files uploaded, batch commit in progress. New batch ID: {batchHandle.BatchId}";
                        IsCommitting = !IsCanceled;
                        IsCommittingOnCancel = true;

                        await fileShareClient.CommitBatch(batchHandle);
                        logger.LogInformation("File Share Service batch commit completed for batch ID:{BatchId}.", batchHandle.BatchId);
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
                        ExecutionResult = await HandleCanceledOperationsAsync(batchHandle, IsCommittingOnCancel, fileShareClient);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e.Message);
                        logger.LogInformation("File Share Service batch rollback started for batch ID:{BatchId}.", batchHandle.BatchId);
                        CancellationTokenSource.Dispose();
                        await fileShareClient.RollBackBatchAsync(batchHandle);
                        logger.LogInformation("File Share Service batch rollback completed for batch ID:{BatchId}.", batchHandle.BatchId);
                        ExecutionResult = e.ToString();
                        throw;
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
            string msg = batchHandle !=null ? string.Format("Are you sure you want to cancel this job batch ID: {0}?", batchHandle.BatchId): "Are you sure you want to cancel this job?";            
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
                        await fileShareClient.RollBackBatchAsync(batchHandle);
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

        private async Task SetBatchExpiry(IBatchHandle batchHandle, IFileShareApiAdminClient fileShareClient)
        {
            logger.LogInformation("File Share Service SetExpiryDateAsync started for batch ID:{BatchId}.", batchHandle.BatchId);
            string expiryDateString = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            await fileShareClient.SetExpiryDateAsync(batchHandle.BatchId, new BatchExpiryModel { ExpiryDate = expiryDateString }, CancellationToken.None);
            logger.LogInformation("File Share Service SetExpiryDateAsync completed for batch ID:{BatchId}.", batchHandle.BatchId);
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
                ExpiryDate = ExpiryDate
            };
        }

        private void ValidateFiles()
        {

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

        private async Task<BatchSearchResponse> SearchBatch()
        {
            var filter = buildSearchQuery();
            var fileShareClient = fileShareClientFactory();
            
            return await fileShareClient.Search(filter, null, null);
        }

        private string buildSearchQuery()
        {
            var queryBuilder = new StringBuilder();
            
            queryBuilder.Append($"businessunit eq '{BusinessUnit}'");
            
            if (Attributes != null)
            {
               string subqueryBatchAttributes = string.Join(" and ", Attributes.Select(k => $"$batch({k.Key}) eq '{k.Value}'"));
               
                queryBuilder.Append(" and "+subqueryBatchAttributes);
            }
            
            var filesSystem = Files.SelectMany(f => f.Files);
            if (filesSystem.Count() > 0)
            {
                string subqueryFileSystemName = string.Join(" or ", filesSystem.Select(f => $"filename eq '{f.Name}'"));
                
                queryBuilder.Append(" and " + "(" + subqueryFileSystemName + ")");
            }
           
            return queryBuilder.ToString();
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
            catch(DirectoryNotFoundException )
            {
                return Enumerable.Empty<IFileSystemInfo>();
            }
        }

    }

    
}