#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
using SQLite;
using System.Text.Json;

namespace CardLib.Models
{
    public class Cart
    {
        [PrimaryKey]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int NumLines { get; set; }
        public int TotalQuantity { get; set; }
        public decimal Subtotal { get; set; }
        public string Data { get; set; } // JSON Serialized CartData

        public bool TrySetData(CartData data)
        {
            if (!data.Validate()) return false;
            Subtotal = data.LineItems.Sum(x => x.Subtotal);
            TotalQuantity = data.LineItems.Sum(x => x.Quantity);
            NumLines = data.LineItems.Length;
            Data = JsonSerializer.Serialize(data);
            return true;
        }

        public CartData GetData()
        {
            return JsonSerializer.Deserialize<CartData>(Data);
        }

        public CartLineItem[] GetLines() => GetData().LineItems;
    }

    public struct CartData
    {
        public CartLineItem[] LineItems { get; set; }

        public bool Validate()
        {
            return LineItems.Length > 0 && !LineItems.Where(x => !x.Validate()).Any();
        }
    }

    public struct CartLineItem
    {
        public int CardId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal Subtotal { get; set; }

        public bool Validate()
        {
            if (Name.Length < 1) return false;
            if (Quantity < 1) return false;
            if (Quantity * Math.Round(Price * 1-(Discount/100), 2) != Subtotal) return false;
            return true;
        }
    }
}
