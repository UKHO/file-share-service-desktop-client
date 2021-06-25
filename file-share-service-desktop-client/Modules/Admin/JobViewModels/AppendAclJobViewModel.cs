using System.Collections.Generic;
using UKHO.FileShareService.DesktopClient.Core.Jobs;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public class AppendAclJobViewModel : IBatchJobViewModel
    {
        private readonly AppendAclJob job;

        public AppendAclJobViewModel(AppendAclJob job)
        {
            this.job = job;
        }

        public string DisplayName => job.DisplayName;

        public string BatchId => job.ActionParams.BatchId;
        public List<string> ReadUsers => job.ActionParams.ReadUsers;
        public List<string> ReadGroups => job.ActionParams.ReadGroups;
    }
}