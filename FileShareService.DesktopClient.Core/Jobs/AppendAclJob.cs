using Prism.Mvvm;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class AppendAclJob : BindableBase, IJob
    {
        public string DisplayName { get; set; }

        public AppendAclJobParams ActionParams { get; set; }
    }

    public class AppendAclJobParams : Acl
    {
        public string BatchId { get; set; }
    }
}