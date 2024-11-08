#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using CardLib.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CardCondition = CardLib.Models.Condition;

namespace CardPilkApp.DataObjects
{
    public partial class CardListingDO : ObservableObject
    {
        public int Id { get; set; }
        public int TCGplayerId { get; set; }
        public ProductLine ProductLine { get; set; }
        public Set Set { get; set; }
        public string Name { get; set; }
        public string CardNumber { get; set; }
        public Rarity Rarity { get; set; }
        public List<CardVariantDO> Variants { get; set; }
        public string Price => Variants.First().PriceString;
        public string TCGMarket => Variants.First().TCGMarket;
        public string TCGLow => Variants.First().TCGLow;
        public string TCGShippedLow => Variants.First().TCGShippedLow;
        public string TCGDirectLow => Variants.First().TCGDirectLow;
        public int SumQuantity => Variants.Sum(x => x.TotalQuantity);


        [ObservableProperty]
        private CardVariantDO? selectedVariant;

        private int selectedVariantIndex = 0;
        public int SelectedVariantIndex 
        { 
            get { return selectedVariantIndex; } 
            set 
            { 
                SetProperty(ref selectedVariantIndex, value);
                SelectedVariant = value > -1 ? Variants[value] : null;
            } 
        }

        public string VariantCount => $"{Variants.Count} Variant{(Variants.Count > 1 ? "s" : "")}";
        public string SearchString => $"{Name} {CardNumber}";
    }

    public class CardVariantDO
    {
        public int Id { get; set; }
        public int TCGplayerId { get; set; }
        public CardCondition Condition { get; set; }
        public decimal Price { get; set; }
        public string PriceString { get; set; }
        public string TCGMarket { get; set; }
        public string TCGLow { get; set; }
        public string TCGShippedLow { get; set; }
        public string TCGDirectLow { get; set; }
        public int TotalQuantity { get; set; }

        public override string ToString()
        {
            return $"{Condition.Name} - {PriceString} ({TotalQuantity})";
        }
    }
}
