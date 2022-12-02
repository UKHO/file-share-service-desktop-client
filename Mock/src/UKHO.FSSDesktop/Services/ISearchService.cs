using UKHO.FileShareClient.Models;

namespace UKHO.FSSDesktop.Services
{
    public interface ISearchService
    {
        BatchSearchResponse Search();
    }
}