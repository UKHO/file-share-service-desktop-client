using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;

namespace UKHO.FileShareService.DesktopClient.Modules.Auth
{
    /// <summary>
    /// Interaction logic for AuthenticateView.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]
    public partial class AuthenticateView : UserControl
    {
        public AuthenticateView(AuthenticateViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}