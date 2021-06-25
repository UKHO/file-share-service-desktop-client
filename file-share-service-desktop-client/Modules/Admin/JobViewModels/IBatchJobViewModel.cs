using Prism.Commands;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels
{
    public interface IBatchJobViewModel
    {
        string DisplayName { get; }
        bool IsExecuting { get; }
        DelegateCommand ExcecuteJobCommand { get; }
    }
}