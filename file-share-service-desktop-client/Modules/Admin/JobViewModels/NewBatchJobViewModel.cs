﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.Abstractions;
using System.Linq;
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

        public NewBatchJobViewModel(NewBatchJob job, IFileSystem fileSystem,
             ILogger<NewBatchJobViewModel> logger,
            Func<IFileShareApiAdminClient> fileShareClientFactory,   
            ICurrentDateTimeProvider currentDateTimeProvider) : base(job)
        {
            CloseExecutionCommand = new DelegateCommand(OnCloseExecutionCommand);
            this.job = job;
            this.fileSystem = fileSystem;
            this.fileShareClientFactory = fileShareClientFactory;
            this.logger = logger;
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
            return Files.All(f => f.CorrectNumberOfFilesFound);
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
                ExpiryDate = ExpiryDate
            };
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

            Files =
                directory.Exists
                    ? directory.EnumerateFileSystemInfos(searchFileInfo.Name)
                    : Enumerable.Empty<IFileSystemInfo>();
        }

        public string RawSearchPath => newBatchFile.SearchPath;
        public string SearchPath { get; }
        public IEnumerable<IFileSystemInfo> Files { get; }
        public int ExpectedFileCount => newBatchFile.ExpectedFileCount;
        public string MimeType => newBatchFile.MimeType;

        public bool CorrectNumberOfFilesFound => ExpectedFileCount == Files.Count();
    }
}