using System.Windows;

namespace UKHO.FileShareService.DesktopClient
{
    public interface IMessageBoxService
    {
        MessageBoxResult ShowMessageBox(string title, string message, MessageBoxButton btn, MessageBoxImage img);
    }

    public class MessageBoxService : IMessageBoxService
    {
        public MessageBoxResult ShowMessageBox(string title, string message, MessageBoxButton btn = MessageBoxButton.OK, MessageBoxImage img = MessageBoxImage.Information)
        {
            MessageBoxResult result = MessageBox.Show(message, title, btn, img);
            return result;
        }
    }
}

