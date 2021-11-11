using System.Linq;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class SetExpiryDateJobViewModel : BaseBatchJobViewModel
    {
        private readonly SetExpiryDateJob job;

        public SetExpiryDateJobViewModel(SetExpiryDateJob job) : base(job)
        {
            this.job = job;
        }
        
        public string BatchId => job.ActionParams.BatchId;
        public string ExpiryDate => job.ActionParams.ExpiryDate;
        protected internal override Task OnExecuteCommand()
        {
            throw new System.NotImplementedException();
        }
        protected override bool CanExecute()
        {
            ValidationErrors.Clear();

            ValidationErrors = job.ErrorMessages;

            return ValidationErrors.Any();
        }
    }
}