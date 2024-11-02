using CardLib;
using CardLib.Models;
using CardPilkApp.DataObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.ObjectModel;

namespace CardPilkApp.ViewModels
{
    public partial class CardListViewModel : ObservableObject
    {
        private CardManager manager;
        public ObservableCollection<CardListingDO> Listings { get; set; }

        public CardListViewModel(CardManager manager) 
        {
            Listings = [];
            this.manager = manager;
            RefreshListings();
        }

        internal string fmtPrice(decimal price)
        {
            return price.ToString("$0.00");
        }

        internal async void RefreshListings()
        {
            Listings.Clear();
            var lines = await manager.GetProductLines();
            var sets = await manager.GetSets();
            var raritys = await manager.GetRarities();
            var conditions = await manager.GetConditions();
            IEnumerable<CardListing> newListings = await manager.GetListings();
            foreach (CardListing l in newListings)
            {
                TCGMarketPriceHistory pricing = await manager.GetNewestPrices(l.TCGplayerId);
                Listings.Add(new()
                {
                    Id = l.Id,
                    TCGplayerId = l. TCGplayerId,
                    ProductLine = lines.Where(x => x.Id == l.ProductLineId).First(),
                    Set = sets.Where(x => x.Id == l.SetId).First(),
                    Name = l.Name,
                    CardNumber = l.CardNumber,
                    Rarity = raritys.Where(x => x.Id == l.RarityId).First(),
                    Condition = conditions.Where(x => x.Id == l.ConditionId).First(),
                    TotalQuantity = l.TotalQuantity,
                    Price = fmtPrice(l.Price),
                    TCGMarket = fmtPrice(pricing.TCGMarketPrice),
                    TCGLow = fmtPrice(pricing.TCGLowPrice),
                    TCGShippedLow = fmtPrice(pricing.TCGLowPriceWithShipping),
                    TCGDirectLow = fmtPrice(pricing.TCGDirectLow),
                });
            }
        }
    }
}
