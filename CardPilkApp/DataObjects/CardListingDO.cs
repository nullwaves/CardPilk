#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using CardLib.Models;
using CardCondition = CardLib.Models.Condition;

namespace CardPilkApp.DataObjects
{
    public class CardListingDO
    {
        public int Id { get; set; }
        public int TCGplayerId { get; set; }
        public ProductLine ProductLine { get; set; }
        public Set Set { get; set; }
        public string Name { get; set; }
        public string CardNumber { get; set; }
        public Rarity Rarity { get; set; }
        public CardCondition Condition { get; set; }
        public int TotalQuantity { get; set; }
        public string Price { get; set; }

        public string TCGMarket { get; set; }
        public string TCGLow { get; set; }
        public string TCGShippedLow { get; set; }
        public string TCGDirectLow { get; set; }
    }
}
