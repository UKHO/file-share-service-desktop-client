using System.Collections.Generic;
using System.Linq;
using Prism.Mvvm;
using UKHO.FileShareService.DesktopClient.Core.Models;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    public class SearchCriterionViewModel : BindableBase, ISearchCriterion
    {
        private readonly ISearchCriteriaViewModel searchCriteriaViewModel;
        private AndOr and;
        private Attribute? selectedFssField;
        private Operators? @operator;
        private string value = string.Empty;

        public SearchCriterionViewModel(ISearchCriteriaViewModel searchCriteriaViewModel)
        {
            this.searchCriteriaViewModel = searchCriteriaViewModel;
        }

        public IEnumerable<Attribute> AvailableAttributes => searchCriteriaViewModel.AvailableAttributes;

        public AndOr And
        {
            get => and;
            set
            {
                if (and != value)
                {
                    and = value;
                    RaisePropertyChanged();
                }
            }
        }

        public IFssBatchAttribute? SelectedFssAttribute => selectedFssField;

        public Attribute? SelectedField
        {
            get => selectedFssField;
            set
            {
                if (selectedFssField != value)
                {
                    selectedFssField = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(SelectedFssAttribute));
                    RaisePropertyChanged(nameof(AvailableOperators));
                }
            }
        }

        public IEnumerable<Operators> AvailableOperators =>
            SelectedField?.AvailableOperators() ?? Enumerable.Empty<Operators>();

        public Operators? Operator
        {
            get => @operator;
            set
            {
                if (@operator != value)
                {
                    @operator = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string Value
        {
            get => value;
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    RaisePropertyChanged();
                }
            }
        }

        public override string ToString()
        {
            return $"{GetType().Name} Field:{SelectedField?.DisplayName} {@operator} {value}";
        }
    }
}