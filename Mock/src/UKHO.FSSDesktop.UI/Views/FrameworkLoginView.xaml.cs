namespace UKHO.FSSDesktop.UI.Views
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    ///     Interaction logic for FrameworkLoginView.xaml
    /// </summary>
    public partial class FrameworkLoginView : UserControl
    {
        public FrameworkLoginView()
        {
            InitializeComponent();
        }

        private void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            ((FrameworkLogin) DataContext).Password = ((PasswordBox) sender).SecurePassword;
        }
    }
}