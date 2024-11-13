using CommunityToolkit.Mvvm.ComponentModel;

namespace CardPilkApp.DataObjects
{
    public partial class CartDO : ObservableObject
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public int NumLines { get; set; }
        public int TotalQuantity { get; set; }
        public decimal Subtotal { get; set; }
        public List<CartLineItemDO> Lines { get; set; } = new();

        public string SubtotalString => Subtotal.ToString("$0.00");
    }
}
