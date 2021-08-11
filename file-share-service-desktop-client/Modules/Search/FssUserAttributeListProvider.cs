using System.Collections.Generic;
using System.Threading.Tasks;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    public interface IFssUserAttributeListProvider
    {
        Task<IEnumerable<string>> GetAttributesAsync();
    }

    public class FssUserAttributeListProvider : IFssUserAttributeListProvider
    {
        private readonly IFileShareApiAdminClientFactory fileShareApiAdminClientFactory;

        public FssUserAttributeListProvider(IFileShareApiAdminClientFactory fileShareApiAdminClientFactory)
        {
            this.fileShareApiAdminClientFactory = fileShareApiAdminClientFactory;
        }

        public Task<IEnumerable<string>> GetAttributesAsync()
        {
            return fileShareApiAdminClientFactory.Build().GetUserAttributesAsync();
        }
    }
}