namespace UKHO.FSSDesktop.Modules.FSS.Search
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using Models;
    using UKHO.FSSDesktop.Services;

    public class ChartSearch : ObservableObject
    {
        private string? _selectedField;
        private string? _selectedOperator;

        private readonly RelayCommand _searchCommand;
        private string? _value;

        private IEnumerable<BatchDetailModel> _results;

        public ChartSearch(ISearchService searchService, IAuthenticationService authenticationService)
        {
            _selectedField = null;
            _selectedOperator = null;
            _value = null;

            _searchCommand = new RelayCommand(() =>
            {
                var searchResult = searchService.Search();
                _results = searchResult.Entries.Select(x => new BatchDetailModel(x, authenticationService.CurrentContext!)).ToList();

                OnPropertyChanged(nameof(Results));

            }, () => !string.IsNullOrWhiteSpace(_selectedField) && !string.IsNullOrWhiteSpace(_selectedOperator) && !string.IsNullOrWhiteSpace(_value));
        }

        public IEnumerable<BatchDetailModel> Results => _results;

        public string? SelectedField
        {
            get => _selectedField;
            set
            {
                SetProperty(ref _selectedField, value);
                _searchCommand.NotifyCanExecuteChanged();
            }
        }

        public string? SelectedOperator
        {
            get => _selectedOperator;
            set
            {
                SetProperty(ref _selectedOperator, value);
                _searchCommand.NotifyCanExecuteChanged();
            }
        }

        public string? Value
        {
            get => _value;
            set
            {
                SetProperty(ref _value, value);
                _searchCommand.NotifyCanExecuteChanged();
            }
        }

        public ICommand SearchCommand => _searchCommand;

        public IEnumerable<string> Fields => new[] {"Product Type"};

        public IEnumerable<string> Operators => new[] {"Equals"};

    }
}