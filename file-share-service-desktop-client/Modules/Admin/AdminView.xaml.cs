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


        private void handleAlertFocus(Border border, string alertMessageTextBoxName)
        {
            TextBox txtAlertMessage = border.FindName(alertMessageTextBoxName) as TextBox;
            if (border.IsVisible && txtAlertMessage != null)
            {
                txtAlertMessage.Dispatcher.BeginInvoke(
                new Action(
                delegate
                {
                    Keyboard.ClearFocus();
                    Keyboard.Focus(txtAlertMessage);
                    bool flag = txtAlertMessage.Focus();
                }));
            }
        }

        private void pnlExecutionComplete_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Border border = sender as Border;
            handleAlertFocus(border, "txtBatchCommitResponse");
        }

        private void pnlBatchCommitInProgressResponse_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Border border = sender as Border;
            handleAlertFocus(border, "txtBatchCommitInProgressResponse");
        }

        private void pnlAppendAclExecutionComplete_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Border border = sender as Border;
            handleAlertFocus(border, "txtAppendAclResponse");
        }

        private void pnlsetExpiryAclExecutionComplete_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Border border = sender as Border;
            handleAlertFocus(border, "txtSetExpiryDateResponse");
        }

        private void pnlReplaceAclExecutionComplete_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            Border border = sender as Border;
            handleAlertFocus(border, "txtReplaceAclResponse");
        }
    }
}
