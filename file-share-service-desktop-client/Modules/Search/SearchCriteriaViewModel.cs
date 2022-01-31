using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Commands;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.FileShareService.DesktopClient.Core.Models;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    public interface ISearchCriteriaViewModel : INotifyPropertyChanged
    {
        IEnumerable<Attribute> AvailableAttributes { get; }
    }

    public class SearchCriteriaViewModel : BindableBase, ISearchCriteriaViewModel
    {
        private readonly IFssSearchStringBuilder fssSearchStringBuilder;
        private readonly IFssUserAttributeListProvider fssUserAttributeListProvider;
        private readonly Attribute[] systemAttributes;
        private IEnumerable<Attribute> availableAttributes;

        public SearchCriteriaViewModel(IFssSearchStringBuilder fssSearchStringBuilder,
            IFssUserAttributeListProvider fssUserAttributeListProvider,
            IEnvironmentsManager environmentsManager)
        {
            this.fssSearchStringBuilder = fssSearchStringBuilder;
            this.fssUserAttributeListProvider = fssUserAttributeListProvider;
            AddNewCriterionCommand = new DelegateCommand(OnAddNewSearchCriterion);
            DeleteRowCommand = new DelegateCommand<SearchCriterionViewModel>(OnDeleteRow);
            AddRowCommand = new DelegateCommand<SearchCriterionViewModel>(OnAddRow);
            systemAttributes = new Attribute[]
            {
                new("Filename", "filename", AttributeType.String),
                new("File Size (in bytes)", "fileSize", AttributeType.Number),
                new("MIME type", "mimetype", AttributeType.String),
                new("Batch Published Date", "batchPublishedDate", AttributeType.Date),
                new("Batch Expiry Date", "expiryDate", AttributeType.NullableDate),
                new("Business Unit", "businessUnit", AttributeType.String)
            };
            availableAttributes = systemAttributes.ToImmutableList();

            environmentsManager.PropertyChanged += EnvironmentsManagerOnPropertyChanged;
            EnvironmentsManagerOnPropertyChanged(this, new PropertyChangedEventArgs(nameof(environmentsManager.CurrentEnvironment)));
            
            SearchCriteria.CollectionChanged += OnSearchCriteriaCollectionChanged;

            OnAddNewSearchCriterion();
        }

        private void EnvironmentsManagerOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (InitTask == null || InitTask.IsCompleted)
            {
                AvailableAttributes = systemAttributes.ToImmutableList();

                InitTask = fssUserAttributeListProvider.GetAttributesAsync().ContinueWith(t =>
                {
                    if (t.IsCompleted)
                        AvailableAttributes = systemAttributes
                            .Concat(t.Result.Select(s => new Attribute(s, AttributeType.UserAttributeString)))
                            .ToImmutableList();
                }, TaskScheduler.Current);
            }
        }

        public Task? InitTask { get; private set; }

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

            if (e.PropertyName == nameof(SearchCriterionViewModel.Operator))
            {
                SetEnablePropertyForValueControl();
            }

            if (e.PropertyName == nameof(SearchCriterionViewModel.SelectedField))
            {
                SetDefaultOperator();
            }
        }

        private void OnAddRow(SearchCriterionViewModel obj)
        {
            SearchCriteria.Insert(SearchCriteria.IndexOf(obj), new SearchCriterionViewModel(this));
            SetVisiblePropertyForAndControl();
        }

        private void OnDeleteRow(SearchCriterionViewModel searchCriterionViewModelToRemove)
        {
            SearchCriteria.Remove(searchCriterionViewModelToRemove);
            SetVisiblePropertyForAndControl();
        }

        private void OnAddNewSearchCriterion()
        {
            SearchCriteria.Add(new SearchCriterionViewModel(this));
            SetVisiblePropertyForAndControl();
        }

        public ObservableCollection<SearchCriterionViewModel> SearchCriteria { get; } = new();

        public IEnumerable<Attribute> AvailableAttributes
        {
            get => availableAttributes;
            private set
            {
                if (availableAttributes != value)
                {
                    availableAttributes = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DelegateCommand AddNewCriterionCommand { get; }

        public DelegateCommand<SearchCriterionViewModel> DeleteRowCommand { get; }
        public DelegateCommand<SearchCriterionViewModel> AddRowCommand { get; }

        public string GetSearchString()
        {
            return fssSearchStringBuilder.BuildSearch(SearchCriteria);
        }

        /// <summary>
        /// Set visible property for AndOr control based on index.
        /// If index is 0, this control should not be visible.
        /// </summary>
        private void SetVisiblePropertyForAndControl()
        {
            if (SearchCriteria != null)
            {
                foreach (var (item, index) in SearchCriteria.Select((v, i) => (v, i)))
                {
                    item.IsAndOrVisible = index != 0;
                }
            }
            RaisePropertyChanged(nameof(SearchCriteria));
        }

        /// <summary>
        /// Set enable property of value control, based on operators.
        /// </summary>
        private void SetEnablePropertyForValueControl()
        {
            if (SearchCriteria != null)
            {
                foreach (var item in SearchCriteria)
                {
                    item.IsValueEnabled = item.Operator == null ||
                                            (item.Operator != Operators.Exists &&
                                            item.Operator != Operators.NotExists);

                    if (!item.IsValueEnabled)
                    {
                        item.Value = string.Empty;
                    }
                }
            }
            RaisePropertyChanged(nameof(SearchCriteria));
        }

        /// <summary>
        /// Set first available operator as default, when existing selected field is changed 
        /// and existing selected operator is not applicable for changed attribute type.
        /// </summary>
        private void SetDefaultOperator()
        {
            foreach (var item in SearchCriteria)
            {
                if (item.SelectedField != null &&
                    item.Operator != null &&
                    !item.AvailableOperators.Any(op => op == item.Operator))
                {
                    item.Operator = item.AvailableOperators.First();
                    item.Value = string.Empty;
                }
            }
        }
    }
}