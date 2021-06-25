using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class SetExpiryDateJobViewModel : IBatchJobViewModel
    {
        private readonly SetExpiryDateJob job;

        public SetExpiryDateJobViewModel(SetExpiryDateJob job)
        {
            this.job = job;
        }

        public string DisplayName => job.DisplayName;
        public string BatchId => job.ActionParams.BatchId;
        public string ExpiryDate => job.ActionParams.ExpiryDate;
    }
}