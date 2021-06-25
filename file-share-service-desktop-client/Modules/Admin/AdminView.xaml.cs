using System.Windows.Controls;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin
{
    /// <summary>
    /// Interaction logic for AdminView.xaml
    /// </summary>
    public partial class AdminView : UserControl
    {
        public AdminView(AdminViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}