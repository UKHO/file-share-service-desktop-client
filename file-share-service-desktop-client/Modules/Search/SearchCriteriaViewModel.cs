using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Prism.Commands;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Models;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    public interface ISearchCriteriaViewModel
    {
        IEnumerable<Attribute> AvailableAttributes { get; }
    }

    public class SearchCriteriaViewModel : BindableBase, ISearchCriteriaViewModel
    {
        private readonly IFssSearchStringBuilder fssSearchStringBuilder;

        public SearchCriteriaViewModel(IFssSearchStringBuilder fssSearchStringBuilder)
        {
            this.fssSearchStringBuilder = fssSearchStringBuilder;
            AddNewCriterionCommand = new DelegateCommand(OnAddNewSearchCriterion);
            DeleteRowCommand = new DelegateCommand<SearchCriterionViewModel>(OnDeleteRow);
            AddRowCommand = new DelegateCommand<SearchCriterionViewModel>(OnAddRow);
            AvailableAttributes = new Attribute[]
            {
                new("Filename", "filename", AttributeType.String),
                new("File Size", "fileSize", AttributeType.Number),
                new("MIME type", "mimetype", AttributeType.String),
                new("Batch Published Date", "batchPublishedDate", AttributeType.Date),
                new("Batch Expiry Date", "expiryDate", AttributeType.NullableDate),
                new("Business Unit", "businessUnit", AttributeType.String)
            }.Concat(new[]
            {
                "CellName",
                "EditionNumber",
                "UpdateNumber",
                "ProductCode",
                "Agency",
                "Product Type",
                "Media Type",
                "Year",
                "Week Number",
                "S63 Version",
                "Exchange Set Type"
            }.Select(s => new Attribute(s, AttributeType.UserAttributeString))).ToImmutableList();

            SearchCriteria.CollectionChanged += OnSearchCriteriaCollectionChanged;

            OnAddNewSearchCriterion();
        }

        private void OnSearchCriteriaCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
                foreach (var newItem in e.NewItems?.OfType<SearchCriterionViewModel>() ??
                                        Enumerable.Empty<SearchCriterionViewModel>())
                    newItem.PropertyChanged += OnChildCriterionPropertyChanged;

            if (e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Remove ||
                e.Action == NotifyCollectionChangedAction.Reset)
                foreach (var newItem in e.OldItems?.OfType<SearchCriterionViewModel>() ??
                                        Enumerable.Empty<SearchCriterionViewModel>())
                    newItem.PropertyChanged -= OnChildCriterionPropertyChanged;
        }

        private void OnChildCriterionPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(SearchCriteria));
        }

        private void OnAddRow(SearchCriterionViewModel obj)
        {
            SearchCriteria.Insert(SearchCriteria.IndexOf(obj), new SearchCriterionViewModel(this));
        }

        private void OnDeleteRow(SearchCriterionViewModel searchCriterionViewModelToRemove)
        {
            SearchCriteria.Remove(searchCriterionViewModelToRemove);
        }

        private void OnAddNewSearchCriterion()
        {
            SearchCriteria.Add(new SearchCriterionViewModel(this));
        }

        public ObservableCollection<SearchCriterionViewModel> SearchCriteria { get; } = new();

        public IEnumerable<Attribute> AvailableAttributes { get; }

        public DelegateCommand AddNewCriterionCommand { get; }

        public DelegateCommand<SearchCriterionViewModel> DeleteRowCommand { get; }
        public DelegateCommand<SearchCriterionViewModel> AddRowCommand { get; }

        public string GetSearchString()
        {
            return fssSearchStringBuilder.BuildSearch(SearchCriteria);
        }
    }
}