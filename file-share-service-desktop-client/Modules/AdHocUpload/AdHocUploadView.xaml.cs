using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using System.Windows.Input;

namespace UKHO.FileShareService.DesktopClient.Modules.AdHocUpload
{
    /// <summary>
    /// Interaction logic for AdminView.xaml
    /// </summary>
    [ExcludeFromCodeCoverage]//Basic View
    public partial class AdHocUploadView : UserControl
    {
        public AdHocUploadView(AdHocUploadViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
