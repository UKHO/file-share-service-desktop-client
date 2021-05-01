using System.Diagnostics.CodeAnalysis;
using UKHO.FileShareService.DesktopClient.ViewModels;

namespace UKHO.FileShareService.DesktopClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class MainWindow
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}