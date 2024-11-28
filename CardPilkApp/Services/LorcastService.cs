using CardLib.Models;
using CardPilkApp.DataObjects;
using Polly;
using RestSharp;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace CardPilkApp.Services
{
    public interface ILorcastService
    {
        Task<List<LorcastSet>> GetSets();
        Task<string?> GetCardImageByNumber(string setId, string collectorNumber);
    }

    public class LorcastService : ILorcastService
    {
        internal List<LorcastSet>? Sets;

        private RateLimitedRestClient _client;
        private Dictionary<string, string> _headers =>
            new Dictionary<string, string>()
            {
                { "User-Agent", "CardPilkDesktop/1.0" },
                { "Accept", "*/*" },
            };

        public LorcastService()
        {
            _client = new RateLimitedRestClient("https://api.lorcast.com/v0", 100);
        }

        public async Task<List<LorcastSet>> GetSets()
        {
            RestRequest request = new("/sets");
            request.AddHeaders(_headers);
            var response = await _client.ExecuteAsync(request);
            if (response != null && response.Content != null)
            {
                var list = JsonSerializer.Deserialize<SetList>(response.Content);
                if (list != null)
                {
                    return list.results.ToList();
                }
            }
            return [];
        }

        public async Task<string?> GetCardImageByNumber(string setId, string collectorNumber)
        {
            RestRequest request = new($"/cards/{setId}/{collectorNumber}");
            request.AddHeaders(_headers);
            var response = await _client.ExecuteAsync(request);
            if (response != null && response.Content != null)
            {
                var card = JsonSerializer.Deserialize<LorcastCard>(response.Content);
                var digital = card.image_uris?.FirstOrDefault().Value;
                var first_image = digital?.ContainsKey("normal") ?? false ? digital?["normal"] : digital?.FirstOrDefault().Value;
                return first_image?.Length > 0 ? first_image : null;
            }
            return null;
        }
    }

    public class SetList
    {
        public List<LorcastSet> results { get; set; } = [];
    }

    public struct LorcastSet
    {
        public string id { get; set; }
        public string name { get; set; }
        public string code { get; set; }
        public DateTime released_at { get; set; }
        public DateTime prereleased_at { get; set; }
    }

    public struct LorcastCard
    {
        public string id { get; set; }
        public string name { get; set; }
        public string version { get; set; }
        public Dictionary<string, Dictionary<string, string>> image_uris { get; set; }
    }
}
