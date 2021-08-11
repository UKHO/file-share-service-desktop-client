using Prism.Mvvm;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class FileUploadProgressViewModel : BindableBase
    {
        private int completeBlocks = 0;
        private int totalBlocks = 0;

        public string Filename { get; }

        public FileUploadProgressViewModel(string filename)
        {
            Filename = filename;
        }

        public int TotalBlocks
        {
            get => totalBlocks;
            set
            {
                if (totalBlocks != value)
                {
                    totalBlocks = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsIndeterminate));
                    RaisePropertyChanged(nameof(DisplayString));
                }
            }
        }

        public int CompleteBlocks
        {
            get => completeBlocks;
            set
            {
                if (completeBlocks != value)
                {
                    completeBlocks = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(DisplayString));
                }
            }
        }

        public bool IsIndeterminate => TotalBlocks < 1;

        public string DisplayString => IsIndeterminate ? string.Empty : $"{CompleteBlocks * 100.0 / TotalBlocks:F0}%";
    }
}