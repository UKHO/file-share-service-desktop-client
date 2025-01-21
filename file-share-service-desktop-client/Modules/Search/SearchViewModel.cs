using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UKHO.FileShareAdminClient.Models;
using UKHO.FileShareClient.Models;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Events;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    public interface ISearchViewModel
    {
    }

    public class SearchViewModel : BindableBase, ISearchViewModel
    {
        private readonly IFileShareApiAdminClientFactory fileShareApiAdminClientFactory;
        private readonly IMessageBoxService messageBoxService;
        private readonly IFileService fileService;
        private readonly ISaveFileDialogService saveFileDialogService;
        private readonly ILogger<SearchViewModel> logger;
        private readonly IEventAggregator eventAggregator;
        private string searchText = string.Empty;
        private string searchResultAsJson = string.Empty;
        private bool searchInProgress;
        private BatchSearchResponse? searchResult;
        private int pageOffset = 0;
        private const int pageSize = 25;
        private const string NO_BATCH_FOUND = "No batches found.";

        private List<BatchDetailsViewModel>? batchDetailsVM;        

        public SearchViewModel(IAuthProvider authProvider,
            IFssSearchStringBuilder fssSearchStringBuilder,
            IFileShareApiAdminClientFactory fileShareApiAdminClientFactory,
            IFssUserAttributeListProvider fssUserAttributeListProvider,
            IEnvironmentsManager environmentsManager,
            IMessageBoxService messageBoxService,
            IFileService fileService,
            ISaveFileDialogService saveFileDialogService,
            ILogger<SearchViewModel> logger,
            IEventAggregator eventAggregator)
        {
            this.fileShareApiAdminClientFactory = fileShareApiAdminClientFactory;
            this.messageBoxService = messageBoxService;
            this.fileService = fileService;
            this.saveFileDialogService = saveFileDialogService;
            this.logger = logger;
            this.eventAggregator = eventAggregator;

            SearchCriteria = new SearchCriteriaViewModel(fssSearchStringBuilder, fssUserAttributeListProvider, environmentsManager);
            SearchCommand = new DelegateCommand(async () => await OnSearch(),
                () => authProvider.IsLoggedIn && !SearchInProgress);
            PreviousPageCommand = new DelegateCommand(async () => await OnPreviousPage(),
                () => SearchResult != null && pageOffset >= pageSize);
            NextPageCommand = new DelegateCommand(async () => await OnNextPage(),
                () => SearchResult != null && pageOffset + pageSize < SearchResult.Total);
            authProvider.PropertyChanged += (sender, args) => SearchCommand.RaiseCanExecuteChanged();
            SearchCriteria.PropertyChanged += OnSearchCriteriaPropertyChanged;

            ExportToCsvCommand = new DelegateCommand(async () => await ExportToCsv(false), () => SearchResult != null && SearchResult.Entries.Any());

            ExportAllToCsvCommand = new DelegateCommand(async () => await ExportToCsv(true), () => SearchResult != null && SearchResult.Entries.Any());

            this.eventAggregator.GetEvent<BatchExpiredEvent>().Subscribe(async () => await OnSearch(), ThreadOption.UIThread);         
        }
        
        private void OnSearchCriteriaPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            SearchText = SearchCriteria.GetSearchString();
        }       

        private async Task OnSearch()
        {
            SearchResultAsJson = "";
            SearchResult = null;
            pageOffset = 0;
            await ExecuteSearch();
        }

        private async Task OnNextPage()
        {
            if (SearchResult == null) return;
            if (pageOffset + pageSize < SearchResult.Total)
                pageOffset += pageSize;
            await ExecuteSearch();
        }

        private async Task OnPreviousPage()
        {
            if (SearchResult == null) return;
            if (pageOffset >= pageSize)
                pageOffset -= pageSize;
            await ExecuteSearch();
        }

        private async Task ExecuteSearch()
        {
            SearchInProgress = true;
            batchDetailsVM = new List<BatchDetailsViewModel>();
            var canSetBatchExpiryDate = false;
            try
            {
                logger.LogInformation("File Share Service search started for SearchText :{searchText}", searchText);
                var fssClient = fileShareApiAdminClientFactory.Build();
                var result = await fssClient.Search(searchText, pageSize, pageOffset, CancellationToken.None);

                if (!result.IsSuccess)
                {
                    var errors = result.Errors ?? new List<Error>();
                    if (result.StatusCode == (int)HttpStatusCode.BadRequest)
                    {                      
                        messageBoxService.ShowMessageBox("Error", $"File Share Service search failed  with status: {result.StatusCode} and Error Description : {string.Join(Environment.NewLine, errors.Select(e => e.Description))}", MessageBoxButton.OK, MessageBoxImage.Error);                        
                    }
                    else 
                    { 
                        messageBoxService.ShowMessageBox("Error", $"Something went wrong, Please try again later or contact administrator.", MessageBoxButton.OK, MessageBoxImage.Error);                        
                    }
                    logger.LogError("File Share Service search failed  with status: {StatusCode} and Error Description : {Description}", result.StatusCode, string.Join(Environment.NewLine, errors.Select(e => e.Description)));
                    return;
                }

                // call FSS api SetExpiryDate by passing empty guid to check whether user has permission to set batch expiry date
                // Status code other than forbidden indicates user have permission.
                var setExpiryDateResult = await fssClient.SetExpiryDateAsync(Guid.Empty.ToString(), new BatchExpiryModel { }, CancellationToken.None);
                if (!setExpiryDateResult.IsSuccess && setExpiryDateResult.StatusCode != (int)HttpStatusCode.Forbidden)
                { 
                    canSetBatchExpiryDate = true;
                }

                SearchResultAsJson = result.Data.ToJson();
                SearchResult = result.Data;

                foreach (var entries in SearchResult.Entries)
                {
                    var bdvm = new BatchDetailsViewModel(fileShareApiAdminClientFactory, messageBoxService,fileService,saveFileDialogService, eventAggregator)
                    {
                        BatchId = entries.BatchId,
                        Attributes = entries.Attributes,
                        BatchPublishedDate = entries.BatchPublishedDate,
                        Files = entries.Files,
                        ExpiryDate = entries.ExpiryDate,
                        CanSetBatchExpiryDate = canSetBatchExpiryDate
                    };
                    batchDetailsVM.Add(bdvm);                    
                }
                logger.LogInformation("File Share Service search result Found :{Total} record.", SearchResult.Total);
            }
            catch (Exception e)
            {
                SearchResult = null;
                SearchResultAsJson = e.ToString();
                logger.LogError("File Share Service search failed  with  Error Description : {Description}", e.Message);
            }
            finally
            {
                RaisePropertyChanged(nameof(BatchDetailsVM));
                NextPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
                SearchInProgress = false;
            }
        }

        private async Task ExportToCsv(bool exportAll = false)
        {
            SearchInProgress = true;

            try
            {
                logger.LogInformation("Export search result to CSV started :{searchText}", searchText);

                if (SearchResult == null || !SearchResult.Entries.Any())
                {
                    messageBoxService.ShowMessageBox("Error", "No search results to export.", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var fssSearchResult = SearchResult;

                if (exportAll)
                {
                    var fssClient = fileShareApiAdminClientFactory.Build();
                    var result = await fssClient.SearchAsync(searchText, SearchResult.Total, 0, CancellationToken.None);

                    if (!result.IsSuccess)
                    {
                        logger.LogError("Failed to fetch all results for CSV export with status: {StatusCode}", result.StatusCode);
                        messageBoxService.ShowMessageBox("Error", "Failed to retrieve all results for export. Please try again.", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    fssSearchResult = result.Data;

                    if (fssSearchResult == null || !fssSearchResult.Entries.Any())
                    {
                        messageBoxService.ShowMessageBox("Error", "No search results to export.", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var saveFilePath = saveFileDialogService.SaveFileDialog("SearchResults.csv");
            
                if (string.IsNullOrEmpty(saveFilePath))
                    return;
                
                saveFilePath = Path.Combine(saveFilePath, $"SearchResults-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
                
                var csvContent = new StringBuilder();

                var header = "BatchId";
                var attributeKeys = fssSearchResult.Entries
                    .SelectMany(entry => entry.Attributes.Select(attr => attr.Key))
                    .Distinct()
                    .ToList();

                header += "," + string.Join(",", attributeKeys.Select(EscapeCsvField));
                csvContent.AppendLine(header);

                foreach (var entry in fssSearchResult.Entries)
                {
                    var attributes = attributeKeys
                            .Select(key =>
                            {
                                var value = entry.Attributes.FirstOrDefault(a => a.Key == key)?.Value ?? string.Empty;
                                return EscapeCsvField(value);
                            });
                    csvContent.AppendLine($"{EscapeCsvField(entry.BatchId)},{string.Join(",", attributes)}");
                }

                fileService.WriteAllText(saveFilePath, csvContent.ToString());
                messageBoxService.ShowMessageBox("Success", $"Search results exported successfully.{Environment.NewLine}{saveFilePath}", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to export search results to CSV: {Message}", ex.Message);
                messageBoxService.ShowMessageBox("Error", "Failed to export search results. Please try again.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SearchInProgress = false;
            }
        }

        private static string EscapeCsvField(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        public string SearchText
        {
            get => searchText;
            set
            {
                if (searchText != value)
                {
                    searchText = value;
                    RaisePropertyChanged();
                }
            }
        }

        public SearchCriteriaViewModel SearchCriteria { get; }

        public bool SearchInProgress
        {
            get => searchInProgress;
            set
            {
                if (searchInProgress != value)
                {
                    searchInProgress = value;
                    RaisePropertyChanged();
                    SearchCommand.RaiseCanExecuteChanged();
                    ExportToCsvCommand.RaiseCanExecuteChanged();
                    ExportAllToCsvCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public DelegateCommand SearchCommand { get; }

        public DelegateCommand ExportToCsvCommand { get; }

        public DelegateCommand ExportAllToCsvCommand { get; }

        public string SearchResultAsJson
        {
            get => searchResultAsJson;
            set
            {
                if (searchResultAsJson != value)
                {
                    searchResultAsJson = value;
                    RaisePropertyChanged();
                }
            }
        }

        public BatchSearchResponse? SearchResult
        {
            get => searchResult;
            set
            {
                // ReSharper disable once PossibleUnintendedReferenceComparison
                if (searchResult != value)
                {
                    searchResult = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(SearchCountSummary));
                    ExportToCsvCommand.RaiseCanExecuteChanged();
                    ExportAllToCsvCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SearchCountSummary
        {
            get
            {
                if (SearchResult == null)
                    return "";

                if (searchResult?.Total == 0)
                    return NO_BATCH_FOUND;

                return $"Showing {pageOffset + 1}-{pageOffset + SearchResult.Count} of {SearchResult.Total}";
            }
        }
        public DelegateCommand NextPageCommand { get; }
        public DelegateCommand PreviousPageCommand { get; }
        public List<BatchDetailsViewModel>? BatchDetailsVM 
        { 
            get => batchDetailsVM;
            set
            { 
               if(batchDetailsVM != value)
                {
                    batchDetailsVM = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}