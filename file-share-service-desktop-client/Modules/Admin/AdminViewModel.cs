using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
using UKHO.FileShareService.DesktopClient.Helper;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin
{
    public class AdminViewModel : BindableBase
    {
        private readonly IFileSystem fileSystem;
        private readonly IKeyValueStore keyValueStore;
        private readonly IJobsParser jobsParser;
        private readonly IFileShareApiAdminClientFactory fileShareApiAdminClientFactory;
        private readonly ICurrentDateTimeProvider currentDateTimeProvider;
        private readonly IMacroTransformer macroTransformer;
        private readonly IDateTimeValidator dateTimeValidator;
        private IEnumerable<IBatchJobViewModel> batchJobs = new List<IBatchJobViewModel>();
        private readonly ILogger<AdminViewModel> logger;
        private readonly ILogger<NewBatchJobViewModel> Nlogger;
        private readonly ILogger<SetExpiryDateJobViewModel> setExpirylogger;

        public AdminViewModel(IFileSystem fileSystem,
            IKeyValueStore keyValueStore,
            IJobsParser jobsParser,
            IFileShareApiAdminClientFactory fileShareApiAdminClientFactory,
            ICurrentDateTimeProvider currentDateTimeProvider,
            IMacroTransformer macroTransformer,
            IDateTimeValidator dateTimeValidator,
            IEnvironmentsManager environmentsManager,
            ILogger<AdminViewModel> logger,
            ILogger<NewBatchJobViewModel> Nlogger,
            ILogger<SetExpiryDateJobViewModel> setExpirylogger)
        {
            this.fileSystem = fileSystem;
            this.keyValueStore = keyValueStore;
            this.jobsParser = jobsParser;
            this.fileShareApiAdminClientFactory = fileShareApiAdminClientFactory;
            this.currentDateTimeProvider = currentDateTimeProvider;
            this.macroTransformer = macroTransformer;
            this.dateTimeValidator = dateTimeValidator;
            this.logger = logger;
            this.Nlogger = Nlogger;
            this.setExpirylogger = setExpirylogger;
            OpenFileCommand = new DelegateCommand(OnOpenFile);

            environmentsManager.PropertyChanged += OnEnvironmentsManagerPropertyChanged;

            logger.LogInformation("Admin Module selected.");
        }

        private void OnEnvironmentsManagerPropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            BatchJobs = Enumerable.Empty<IBatchJobViewModel>();
        }

        public DelegateCommand OpenFileCommand { get; }

        private void OnOpenFile()
        {        
            var openFileDialog = new OpenFileDialog
            {
                ShowReadOnly = true,
                CheckFileExists = true,
                DefaultExt = ".json",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                InitialDirectory = keyValueStore["InitialDirectory"] ??
                                   Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var fileName = openFileDialog.FileName;
                keyValueStore["InitialDirectory"] =
                    fileSystem.FileInfo.FromFileName(fileName).Directory.FullName;

                LoadBatchJobsFile(fileName);
            }
            logger.LogInformation("Config file loaded.");
        }

        public void LoadBatchJobsFile(string? fileName)
        {
            using var fs = fileSystem.File.OpenText(fileName);
            BatchJobs = jobsParser.Parse(fs.ReadToEnd()).jobs.Select(BuildJobViewModel).ToList();
        }

        private IBatchJobViewModel BuildJobViewModel(IJob job)
        {
            return job switch
            {
                NewBatchJob newBatch => new NewBatchJobViewModel(newBatch, fileSystem,Nlogger,
                    () => fileShareApiAdminClientFactory.Build(),
                    currentDateTimeProvider,macroTransformer),
                AppendAclJob appendAcl => new AppendAclJobViewModel(appendAcl),
                SetExpiryDateJob setExpiryDate => new SetExpiryDateJobViewModel(setExpiryDate, setExpirylogger, 
                                () => fileShareApiAdminClientFactory.Build(), dateTimeValidator),
                ErrorDeserializingJobsJob errorDeserializingJobs => new ErrorDeserializingJobsJobViewModel(
                    errorDeserializingJobs),
                _ => throw new ArgumentException("Not implemented for job " + job.GetType())
            };
        }

        public IEnumerable<IBatchJobViewModel> BatchJobs
        {
            get => batchJobs;
            set
            {
                if (batchJobs != value)
                {
                    batchJobs = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}