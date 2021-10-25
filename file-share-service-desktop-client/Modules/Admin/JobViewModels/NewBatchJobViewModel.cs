using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Prism.Commands;
using UKHO.FileShareAdminClient;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.WeekNumberUtils;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class NewBatchJobViewModel : BaseBatchJobViewModel
    {
        private readonly NewBatchJob job;
        private readonly IFileSystem fileSystem;
        private readonly Func<IFileShareApiAdminClient> fileShareClientFactory;
        private readonly ICurrentDateTimeProvider currentDateTimeProvider;
        private bool isExecutingComplete;
        private string executionResult = string.Empty;

        public NewBatchJobViewModel(NewBatchJob job, IFileSystem fileSystem,
            Func<IFileShareApiAdminClient> fileShareClientFactory,
            ICurrentDateTimeProvider currentDateTimeProvider) : base(job)
        {
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
            this.job = job;
            this.fileSystem = fileSystem;
            this.fileShareClientFactory = fileShareClientFactory;
            this.currentDateTimeProvider = currentDateTimeProvider;
            Files = job.ActionParams.Files.Select(f => new NewBatchFilesViewModel(f, fileSystem, ExpandMacros))
                .ToList();
        }

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

        public string JobId
        {
            get
            {
                return $"newBatch-{DisplayName.Replace(" ", string.Empty).ToLower()}";
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Attributes =>
            job.ActionParams.Attributes.Select(kv => new KeyValuePair<string, string>(kv.Key, ExpandMacros(kv.Value)));

        private string ExpandMacros(string value)
        {
            var replacementExpressions = new Dictionary<string, Func<Match, string>>
            {
                {@"\$\(\s*now\.Year\s*\)", (_) => "" + currentDateTimeProvider.CurrentDateTime.Year},
                {
                    @"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Year\s*\)", (match) =>
                    {
                        var capturedNumber = match.Groups[1].Value;
                        var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                        return "" + currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).Year;
                    }
                },
                {
                    @"\$\(\s*now\.WeekNumber\s*\)",
                    (_) => "" + WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime).Week
                },
                {
                    @"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\)",
                    (match) =>
                    {
                        var capturedNumber = match.Groups[1].Value;
                        var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                        return "" + WeekNumber
                            .GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset * 7))
                            .Week;
                    }
                },
                {
                    @"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\s*\)",
                    (match) =>
                    {
                        var capturedNumber = match.Groups[1].Value;
                        var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                        return "" + WeekNumber
                            .GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)).Week;
                    }
                },
                {
                    @"\$\(now.AddDays\(\s*([+-]?\s*\d+)\s*\)\)", (match) =>
                    {
                        var capturedNumber = match.Groups[1].Value;
                        var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                        return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)
                            .ToString(CultureInfo.InvariantCulture);
                    }
                },
                {@"\$\(now\)", (_) => currentDateTimeProvider.CurrentDateTime.ToString(CultureInfo.InvariantCulture)}
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
            PopulateValidationErrors(JobId);
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
                var fileShareClient = fileShareClientFactory();
                var buildBatchModel = BuildBatchModel();
                var batchHandle = await fileShareClient.CreateBatchAsync(buildBatchModel);
                FileUploadProgress.Clear();
                try
                {
                    await Task.WhenAll(
                        Files.SelectMany(f => f.Files.Select(file => (f, file)))
                            .Select(f =>
                            {
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
                                    }).ContinueWith(_ => openRead.Dispose());
                            }).ToArray());

                    await fileShareClient.CommitBatch(batchHandle);
                    ExecutionResult = $"Batch uploaded. New batch ID: {batchHandle.BatchId}";
                }
                catch (Exception e)
                {
                    await fileShareClient.RollBackBatchAsync(batchHandle);
                    ExecutionResult = e.ToString();
                    throw;
                }
            }
            finally
            {
                IsExecuting = false;
                IsExecutingComplete = true;
            }
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

        protected override void ValidateViewModel()
        {
            if(string.IsNullOrWhiteSpace(BusinessUnit))
            {
                ValidationErrors.Add("Business unit is missing or not specified.");
            }

            if(ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.UtcNow)
            {
                ValidationErrors.Add("Batch expiry date is past date.");
            }

            if(Files.Count == 0)
            {
                ValidationErrors.Add("File is not specified for upload.");
            }

            foreach (var file in Files)
            {
                string directory = Path.GetDirectoryName(file.RawSearchPath);

                if(string.IsNullOrWhiteSpace(directory))
                {
                    ValidationErrors.Add($"Invalid directory specified - {file.RawSearchPath}");
                    continue;
                }

                if(!IsDirectoryExist(directory))
                {
                    string directoryNotFoundMessage = $"Either directory '{directory}' doesn't exist or user does not have permission to access it.";
                    string accessibleDirectory = GetAccessibleDirectoryName(directory);

                    ValidationErrors.Add(string.IsNullOrWhiteSpace(accessibleDirectory) ? directoryNotFoundMessage : $"{directoryNotFoundMessage}.\n\tThe accessible level is: '{accessibleDirectory}'");
                    continue;
                }

                if(!file.CorrectNumberOfFilesFound)
                {
                    string fileCountMismatchErrorMessage = $"Expected file count is {file.ExpectedFileCount}, but actual existing file count is {file.Files?.Count()} in file path '{file.RawSearchPath}'.";

                    if (file.Files?.Count() > 0)
                    {
                        string existingFileNames = string.Join(", ", file.Files.Select(f => f.Name).ToArray());
                        fileCountMismatchErrorMessage += $"\n\tThe existing files are: {existingFileNames}";
                    }
                    ValidationErrors.Add($"{fileCountMismatchErrorMessage}");
                }
            }
        }

        private bool IsDirectoryExist(string directory)
        {
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
                _ = directoryInfo.GetDirectories();

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private string GetAccessibleDirectoryName(string directory)
        {
           if(!string.IsNullOrWhiteSpace(directory))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);

                if (IsDirectoryExist(directory))
                {
                    return directory;
                }
                else
                {
                    if (directoryInfo.Parent != null)
                    {
                        return GetAccessibleDirectoryName(Convert.ToString(directoryInfo.Parent));
                    }                    
                }
            }
            return string.Empty;
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
            var searchFileInfo = fileSystem.FileInfo.FromFileName(SearchPath);
            var directory = fileSystem.DirectoryInfo.FromDirectoryName(searchFileInfo.DirectoryName);

            Files = directory.Exists
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
            catch(DirectoryNotFoundException ex)
            {
                return Enumerable.Empty<IFileSystemInfo>();
            }
        }

    }

    
}