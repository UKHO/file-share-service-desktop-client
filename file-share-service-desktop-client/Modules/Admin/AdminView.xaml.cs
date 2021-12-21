using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Input;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin
{
    /// <summary>
    /// Interaction logic for AdminView.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]//Basic View
    public partial class AdminView : UserControl
    {
        public AdminView(AdminViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void pnlExecutionComplete_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Border border = sender as Border;
            TextBox txtBatchCommitResponse = border.FindName("txtBatchCommitResponse") as TextBox;
            if (border.IsVisible && txtBatchCommitResponse != null)
            {
                txtBatchCommitResponse.Dispatcher.BeginInvoke(
                new Action(
                delegate
                {
                    Keyboard.ClearFocus();
                    Keyboard.Focus(txtBatchCommitResponse);
                    bool flag = txtBatchCommitResponse.Focus();
                }));
            }
        }

        private void txtBatchCommitInProgressResponse_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            TextBox txtBatchCommitInProgressResponse = sender as TextBox;
            
            if (txtBatchCommitInProgressResponse.IsVisible)
            {
                txtBatchCommitInProgressResponse.Dispatcher.BeginInvoke(
                new Action(
                delegate
                {
                    Keyboard.ClearFocus();
                    Keyboard.Focus(txtBatchCommitInProgressResponse);
                    bool flag = txtBatchCommitInProgressResponse.Focus();
                }));
            }
        }
    }
}