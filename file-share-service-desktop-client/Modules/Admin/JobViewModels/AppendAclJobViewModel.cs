using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.FileShareService.DesktopClient.Core;
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
        public string JobId
        {
            get
            {
                return $"appendAcl-{DisplayName.Replace(" ", string.Empty).ToLower()}";
            }
        }
        protected internal override Task OnExecuteCommand()
        {
            throw new System.NotImplementedException();
        }

        protected override bool CanExecute()
        {
            ValidationErrors.Clear();

            if (JobValidationErrors.ValidationErrors.ContainsKey(JobId))
            {
                ValidationErrors.AddRange(
                    JobValidationErrors.ValidationErrors[JobId]);
            }

            ValidateViewModel();

            return !ValidationErrors.Any();
        }

        private void ValidateViewModel()
        {
            if(string.IsNullOrWhiteSpace(BatchId))
            {
                ValidationErrors.Add("Batch id is missing.");
            }

            if(!ReadGroups.Any() && !ReadUsers.Any())
            {
                ValidationErrors.Add("ReadGroups/ReadUsers are not specified.");
            }  
        }
    }
}