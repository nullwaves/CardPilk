#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
using SQLite;

namespace CardLib.Models
{
    public class RepricerUpdate
    {
        [PrimaryKey]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string BasePrice { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public decimal MinimumPrice { get; set; }
        public bool RunAgainstAllCards { get; set; }
        public int PricesChanged { get; set; }
        public string Changes { get; set; } // Serialized JSON RepricerChange[]
        public decimal GrossChange { get; set; }
        public decimal NetChange { get; set; }
    }

    public struct RepricerChange
    {
        public int CardId { get; set; }
        public decimal Old { get; set; }
        public decimal Delta { get; set; }
    }
}
