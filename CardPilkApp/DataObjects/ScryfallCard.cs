#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace CardPilkApp.DataObjects
{
    public class ScryfallCard
    {
        public int? arena_id { get; set; }
        public string id { get; set; } = string.Empty;
        public string lang { get; set; } = string.Empty;
        public int? tcgplayer_id { get; set; }
        public int? tcgplayer_etched_id { get; set; }
        public int? cardmarket_id { get; set; }
        public Uri uri { get; set; }
        public string name { get; set; }
        public string collector_number { get; set; }
        public Dictionary<string, string> image_uris { get; set; }
    }
}
