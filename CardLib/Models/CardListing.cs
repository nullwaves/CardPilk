#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using SQLite;

namespace CardLib.Models
{
    public class CardListing
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int TCGplayerId { get; set; }
        [Indexed]
        public int ProductLineId { get; set; }
        [Indexed]
        public int SetId { get; set; }
        public string Name { get; set; }
        public string CardNumber { get; set; }
        [Indexed]
        public int RarityId { get; set; }
        [Indexed]
        public int ConditionId { get; set; }
        public int TotalQuantity { get; set; }
        public decimal Price { get; set; }
        public string ImageUri { get; set; }
    }
}
