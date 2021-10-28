using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Jobs;
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
        private IEnumerable<IBatchJobViewModel> batchJobs = new List<IBatchJobViewModel>();

        public AdminViewModel(IFileSystem fileSystem,
            IKeyValueStore keyValueStore,
            IJobsParser jobsParser,
            IFileShareApiAdminClientFactory fileShareApiAdminClientFactory,
            ICurrentDateTimeProvider currentDateTimeProvider,
            IEnvironmentsManager environmentsManager)
        {
            this.fileSystem = fileSystem;
            this.keyValueStore = keyValueStore;
            this.jobsParser = jobsParser;
            this.fileShareApiAdminClientFactory = fileShareApiAdminClientFactory;
            this.currentDateTimeProvider = currentDateTimeProvider;
            OpenFileCommand = new DelegateCommand(OnOpenFile);

            environmentsManager.PropertyChanged += OnEnvironmentsManagerPropertyChanged;
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
        }

        public void LoadBatchJobsFile(string? fileName)
        {
            using var fs = fileSystem.File.OpenText(fileName);

            var parsedData = jobsParser.Parse(fs.ReadToEnd());

            List<IJob> jobs = new List<IJob>();

            //Create ErrorDeserializingJob, if no jobs or any null job found after parsing.
            if (parsedData == null || !parsedData.jobs.Any() || parsedData.jobs.Any(job => job == null))
            {
                ErrorDeserializingJobsJob errorJob =
                    new ErrorDeserializingJobsJob(new Exception("There are some errors in configuration file."));

                if (JobValidationErrors.ValidationErrors.ContainsKey(JobValidationErrors.UNKNOWN_JOB_ERROR_CODE))
                {
                    errorJob.ErrorMessages =
                        JobValidationErrors.ValidationErrors[JobValidationErrors.UNKNOWN_JOB_ERROR_CODE];
                }

                jobs.Add(errorJob);
            }


            List<IJob> validJobs = parsedData?.jobs?.Where(job => job != null).ToList();

            if(validJobs != null && validJobs.Any())
            {
                jobs.AddRange(validJobs);
            }

            BatchJobs = jobs.Select(BuildJobViewModel).ToList();
        }

        private IBatchJobViewModel BuildJobViewModel(IJob job)
        {
            return job switch
            {
                NewBatchJob newBatch => new NewBatchJobViewModel(newBatch, fileSystem,
                    () => fileShareApiAdminClientFactory.Build(),
                    currentDateTimeProvider),
                AppendAclJob appendAcl => new AppendAclJobViewModel(appendAcl),
                SetExpiryDateJob setExpiryDate => new SetExpiryDateJobViewModel(setExpiryDate),
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