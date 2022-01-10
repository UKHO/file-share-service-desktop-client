using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using UKHO.FileShareClient.Models;
using UKHO.FileShareService.DesktopClient.Core;

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
        private string searchText = string.Empty;
        private string searchResultAsJson = string.Empty;
        private bool searchInProgress;
        private BatchSearchResponse? searchResult;
        private int pageOffset = 0;
        private const int pageSize = 25;
        private const string NO_BATCH_FOUND = "No batches found.";

        private List<BatchDetailsViewModel> batchDetailsVM;
        
        public SearchViewModel(IAuthProvider authProvider,
            IFssSearchStringBuilder fssSearchStringBuilder,
            IFileShareApiAdminClientFactory fileShareApiAdminClientFactory,
            IFssUserAttributeListProvider fssUserAttributeListProvider,
            IEnvironmentsManager environmentsManager,
            IMessageBoxService messageBoxService,
            IFileService fileService)
        {
            this.fileShareApiAdminClientFactory = fileShareApiAdminClientFactory;
            this.messageBoxService = messageBoxService;
            this.fileService = fileService;
            SearchCriteria = new SearchCriteriaViewModel(fssSearchStringBuilder, fssUserAttributeListProvider, environmentsManager);
            SearchCommand = new DelegateCommand(async () => await OnSearch(),
                () => authProvider.IsLoggedIn && !SearchInProgress);
            PreviousPageCommand = new DelegateCommand(async () => await OnPreviousPage(),
                () => SearchResult != null && pageOffset >= pageSize);
            NextPageCommand = new DelegateCommand(async () => await OnNextPage(),
                () => SearchResult != null && pageOffset + pageSize < SearchResult.Total);
            authProvider.PropertyChanged += (sender, args) => SearchCommand.RaiseCanExecuteChanged();
            SearchCriteria.PropertyChanged += OnSearchCriteriaPropertyChanged;
           
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
            try
            {
                var fssClient = fileShareApiAdminClientFactory.Build();
                var result = await fssClient.Search(searchText, pageSize, pageOffset);

                SearchResultAsJson = result.ToJson();
                SearchResult = result;
                foreach(var entries in  SearchResult.Entries)
                {
                    BatchDetailsViewModel bdvm = new BatchDetailsViewModel(fileShareApiAdminClientFactory, messageBoxService,fileService)
                    {
                        BatchId = entries.BatchId,
                        Attributes = entries.Attributes,
                        BatchPublishedDate = entries.BatchPublishedDate,
                        Files = entries.Files
                    };
                    batchDetailsVM.Add(bdvm);
                }
                RaisePropertyChanged(nameof(BatchDetailsVM));
                NextPageCommand.RaiseCanExecuteChanged();
                PreviousPageCommand.RaiseCanExecuteChanged();
            }
            catch (Exception e)
            {
                SearchResult = null;
                SearchResultAsJson = e.ToString();
            }
            finally
            {
                SearchInProgress = false;
            }
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
                }
            }
        }

        public DelegateCommand SearchCommand { get; }

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
        public List<BatchDetailsViewModel> BatchDetailsVM 
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