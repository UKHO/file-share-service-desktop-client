using System.ComponentModel;

namespace UKHO.FileShareService.DesktopClient
{
    public interface IPageButton : INotifyPropertyChanged
    {
        public string DisplayName { get; }
        public bool Enabled { get; }
        public string NavigationTarget { get; }
    }
}