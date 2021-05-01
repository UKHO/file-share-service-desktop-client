using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    /// <summary>
    /// Interaction logic for SearchView.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class SearchView : UserControl
    {
        public SearchView(SearchViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}