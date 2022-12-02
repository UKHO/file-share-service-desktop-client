namespace UKHO.FSSDesktop.UI.Windows
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Messaging;
    using FSSDesktop.Messages.Security;
    using Navigation;

    public class FrameworkWindow : ObservableObject, IRecipient<AuthenticationMessage>
    {
        private readonly IFrameworkViewProvider _viewProvider;

        public FrameworkWindow(IFrameworkViewProvider viewProvider)
        {
            _viewProvider = viewProvider;

            WeakReferenceMessenger.Default.Register(this);
        }

        public IFrameworkViewProvider ViewProvider => _viewProvider;
        public void Receive(AuthenticationMessage message)
        {
            if (message.Context != null)
            {
                // Show name in title
            }
        }
    }
}