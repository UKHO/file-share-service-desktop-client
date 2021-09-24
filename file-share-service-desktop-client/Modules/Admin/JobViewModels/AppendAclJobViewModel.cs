using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class AppendAclJobViewModel : BaseBatchJobViewModel
    {
        private readonly AppendAclJob job;

        public AppendAclJobViewModel(AppendAclJob job):base (job)
        {
            this.job = job;
        }

        public string BatchId => job.ActionParams.BatchId;
        public List<string> ReadUsers => job.ActionParams.ReadUsers;
        public List<string> ReadGroups => job.ActionParams.ReadGroups;
        protected internal override Task OnExecuteCommand()
        {
            throw new System.NotImplementedException();
        }

        protected override bool CanExecute()
        {
            return Validate();
        }

        private bool Validate()
        {
            if(string.IsNullOrWhiteSpace(BatchId))
            {
                ValidationErrors.Add("Batch id is missing.");
            }

            if(ReadGroups.Count == 0 && ReadUsers.Count == 0)
            {
                ValidationErrors.Add("ReadGroups/ReadUsers are not specified.");
            }

            return ValidationErrors.Count == 0;
        }
    }
}