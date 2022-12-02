namespace UKHO.FSSDesktop.Infrastructure.Services
{
    using FileShareClient.Models;
    using FSSDesktop.Services;
    using System.Reflection;
    using Newtonsoft.Json;

    internal class SearchService : ISearchService
    {
        private const string ResourceName = "UKHO.FSSDesktop.Infrastructure.Assets.search-results.json";

        public BatchSearchResponse Search()
        {
            var json = ReadJson();
            var result = JsonConvert.DeserializeObject<BatchSearchResponse>(json)!;

            return result;
        }

        private string ReadJson()
        {
            var assembly = Assembly.GetExecutingAssembly()!;

            using var stream = assembly.GetManifestResourceStream(ResourceName)!;
            using var reader = new StreamReader(stream);
            
            var json = reader.ReadToEnd();

            return json;
        }
    }
}