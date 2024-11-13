using CardLib.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CardPilkApp.DataObjects
{
    internal class RepricerUpdateDO : ObservableObject
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string BasePrice { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public decimal MinimumPrice { get; set; }
        public bool RunAgainstAllCards { get; set; }
        public int PricesChanged { get; set; }
        public RepricerChange[] Changes { get; set; } = Array.Empty<RepricerChange>();
        public decimal GrossChange { get; set; }
        public decimal NetChange { get; set; }

        public string PercentageString => Percentage.ToString("0.00%");
        public string MinimumPriceString => MinimumPrice.ToString("$0.00");
        public string GrossChangeString => GrossChange.ToString("$0.00");
        public string NetChangeString => NetChange.ToString("$0.00");

        public static RepricerUpdateDO FromModel(RepricerUpdate update)
        {
            return new()
            {
                Id = update.Id,
                CreatedAt = update.CreatedAt,
                BasePrice = update.BasePrice,
                Percentage = update.Percentage,
                MinimumPrice = update.MinimumPrice,
                RunAgainstAllCards = update.RunAgainstAllCards,
                PricesChanged = update.PricesChanged,
                Changes = update.GetChanges(),
                GrossChange = update.GrossChange,
                NetChange = update.NetChange,
            };
        }

    }
}
