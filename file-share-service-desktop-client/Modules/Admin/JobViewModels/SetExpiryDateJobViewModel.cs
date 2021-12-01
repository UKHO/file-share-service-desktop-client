using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class SetExpiryDateJobViewModel : BaseBatchJobViewModel
    {
        private readonly SetExpiryDateJob job;
        private readonly ILogger<SetExpiryDateJobViewModel> logger;

        public SetExpiryDateJobViewModel(SetExpiryDateJob job, ILogger<SetExpiryDateJobViewModel> logger) : base(job, logger)
        {
            this.job = job;
            this.logger = logger;
        }
        
        public string BatchId => job.ActionParams.BatchId;
        public string ExpiryDate => job.ActionParams.ExpiryDate;
        protected internal override Task OnExecuteCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}