#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using SQLite;
using System.Text.Json;

namespace CardLib.Models
{
    public class ImportBatch
    {
        [PrimaryKey]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ImportSource { get; set; }
        public string Items { get; set; } // JSON int[] CardListing.Ids
        public int InvalidRows { get; set; }
        public int CreatedCards { get; set; }
        public int CreatedPrices { get; set; }
        public int CreatedConditions { get; set; }
        public int CreatedProductLines { get; set; }
        public int CreatedRarities { get; set; }
        public int CreatedSets { get; set; }
        public int UpdatedQuantities { get; set; }

        public void SetItems(int[] ids)
        {
            Items = JsonSerializer.Serialize(ids);
        }

        public int[] GetItems()
        {
            return JsonSerializer.Deserialize<int[]>(Items) ?? new int[0];
        }
    }
}
