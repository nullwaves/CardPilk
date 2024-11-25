using RestSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardPilkApp.Services
{
    public interface IScryfallService
    {
        Task<Stream?> FetchBulkDataAsync();

    }

    public class ScryfallService : IScryfallService
    {
        private RestClient _client;
        private Dictionary<string, string> _scryHeaders =>
            new Dictionary<string, string>()
            {
                { "User-Agent", "CardPilkDesktop/1.0" },
                { "Accept", "*/*" },
            };

        public ScryfallService()
        {
            _client = new("https://api.scryfall.com");
        }

        internal async Task<BulkData?> FetchBulkDataObjectAsync()
        {
            RestRequest request = new RestRequest("/bulk-data/default_cards");
            request.AddHeaders(_scryHeaders);
            var response = await _client.GetAsync(request);
            if (response != null && response.Content != null)
            {
                var data = JsonSerializer.Deserialize<BulkData>(response.Content);
                return data;
            }
            return null;
        }

        public async Task<Stream?> FetchBulkDataAsync()
        {
            var bdo = await FetchBulkDataObjectAsync();
            if (bdo == null) return null;
            string size = byteSize(bdo.size);
            var acceptSizeWarning = await Shell.Current.DisplayAlert("Scryfall Fetcher", $"This will download {size} of data from Scryfall. Continue?", "OK", "Cancel");
            if (!acceptSizeWarning) return null;
            RestRequest request = new RestRequest(bdo.download_uri);
            request.AddHeaders(_scryHeaders);
            return await _client.DownloadStreamAsync(request);
        }

        private static string[] byteSizeSuffixes = { "B", "KB", "MB", "GB", "TB" };
        private string byteSize(int size)
        {
            const string format = "{0}{1:0.#} {2}";
            if (size == 0) { return string.Format(format, null, size, byteSizeSuffixes[0]); }
            var abs = Math.Abs(size);
            var power = (int)Math.Log(abs, 1000);
            var unit = power >= byteSizeSuffixes.Length ? byteSizeSuffixes.Length - 1 : power;
            var norm = abs / Math.Pow(1000, unit);
            return string.Format(format, size < 0 ? "-" : null, norm, byteSizeSuffixes[unit]);
        }
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    internal class BulkData
    {
        [JsonPropertyName("object")]
        public string object_type { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public string updated_at { get; set; }
        public string uri { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int size { get; set; }
        public string download_uri { get; set; }
        public string content_type { get; set; }
        public string content_encoding { get; set; }
    }
#pragma warning restore CS8618 
}
