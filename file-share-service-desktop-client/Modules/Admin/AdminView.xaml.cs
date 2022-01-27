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


        private void handleAlertFocus(Border? border, string alertMessageTextBoxName)
        {
            if ((border?.FindName(alertMessageTextBoxName)) is TextBox txtAlertMessage && border.IsVisible)
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
            handleAlertFocus(sender as Border, "txtBatchCommitResponse");
        }

        private void pnlBatchCommitInProgressResponse_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            handleAlertFocus(sender as Border, "txtBatchCommitInProgressResponse");
        }

        private void pnlAppendAclExecutionComplete_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            handleAlertFocus(sender as Border, "txtAppendAclResponse");
        }

        private void pnlsetExpiryAclExecutionComplete_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            handleAlertFocus(sender as Border, "txtSetExpiryDateResponse");
        }

        private void pnlReplaceAclExecutionComplete_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            handleAlertFocus(sender as Border, "txtReplaceAclResponse");
        }
    }
}
