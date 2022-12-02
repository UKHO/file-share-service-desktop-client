namespace UKHO.FSSDesktop.Search.Views
{
    using FileShareClient.Models;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Controls;

    public class ResultGridBuilder
    {
        private readonly BatchSearchResponse _results;
        private readonly DataGrid _grid;

        public ResultGridBuilder(BatchSearchResponse results, DataGrid grid)
        {
            _results = results;
            _grid = grid;
        }

        public void Build()
        {
            _grid.Items.Clear();
            _grid.Columns.Clear();
            
            var dictionary = new Dictionary<string, List<BatchDetails>>();

            foreach (var entry in _results.Entries)
            {
                var productTypeAttribute = entry.Attributes.SingleOrDefault(a => a.Key == "Product Type");

                if (productTypeAttribute == null)
                {
                    AddToDictionary("(none)", entry, dictionary);
                }
                else
                {
                    AddToDictionary(productTypeAttribute.Value, entry, dictionary);
                }
            }
        }

        private void AddToDictionary(string key, BatchDetails entry, Dictionary<string, List<BatchDetails>> dictionary)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, new List<BatchDetails>());
            }

            dictionary[key].Add(entry);
        }

    }
}