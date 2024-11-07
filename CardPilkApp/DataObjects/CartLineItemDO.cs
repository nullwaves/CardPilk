#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace CardPilkApp.DataObjects
{
    public partial class CartLineItemDO : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Condition { get; set; }
        public decimal Price { get; set; }
        [ObservableProperty]
        private int quantity;

        public string PriceString => Price.ToString("$0.00");
    }
}
