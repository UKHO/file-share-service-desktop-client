using System.Collections.Generic;
using System.Linq;

namespace UKHO.FileShareService.DesktopClient.Core.Jobs
{
    public class Jobs
    {
        private IEnumerable<IJob>? jobs1 = new List<IJob>();

        public IEnumerable<IJob> jobs
        {
            get => jobs1 ?? Enumerable.Empty<IJob>();
            set => jobs1 = value;
        }
    }
}