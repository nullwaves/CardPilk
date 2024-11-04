using CardLib;
using CardLib.Models;
using CardPilkApp.DataObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using CardCondition = CardLib.Models.Condition;

namespace CardPilkApp.ViewModels
{
    public partial class CardListViewModel : ObservableObject
    {
        private CardManager manager;
        private static ProductLine NoProductFilter = new() { Id = -1, Name = "----------" };
        private static Set NoSetFilter = new() { Id = -1, Name = "----------" };
        private static CardCondition NoConditionFilter = new() { Id = -1, Name = "----------" };
        public int[] MaxListingsOptions { get; set; }
        public int MaxListings { get; set; }
        public bool InStockOnly { get; set; }
        public string SearchText { get; set; }
        public List<CardListingDO> Listings { get; set; }
        public ObservableCollection<ProductLine> ProductLines { get; set; }
        public ObservableCollection<Set> Sets { get; set; }
        public ObservableCollection<CardCondition> Conditions { get; set; }
        public ProductLine FilterByProductLine { get; set; } = NoProductFilter;
        public Set FilterBySet { get; set; } = NoSetFilter;
        public CardCondition FilterByCondition { get; set; } = NoConditionFilter;
        public ObservableCollection<CardListingDO> ResultListings { get; set; }

        public CardListViewModel(CardManager manager) 
        {
            MaxListingsOptions = new[] { 25, 50, 100, 200 };
            MaxListings = 50;
            Listings = [];
            ProductLines = [];
            Sets = [];
            Conditions = [];
            ResultListings = [];
            SearchText = string.Empty;
            this.manager = manager;
            RefreshListings();
        }

        internal string fmtPrice(decimal price)
        {
            return price.ToString("$0.00").Replace("$0.00", "--");
        }

        internal async void PopulateListings(IEnumerable<CardListing> newListings)
        {
            Listings.Clear();
            var lines = await manager.GetProductLines();
            ProductLines.Clear();
            ProductLines.Add(NoProductFilter);
            FilterByProductLine = NoProductFilter;
            foreach (var line in lines)
            {
                ProductLines.Add(line);
            }
            var sets = await manager.GetSets();
            Sets.Clear();
            Sets.Add(NoSetFilter);
            FilterBySet = NoSetFilter;
            foreach(var s in sets)
            {
                Sets.Add(s);
            }
            var conds = await manager.GetConditions();
            Conditions.Clear();
            Conditions.Add(NoConditionFilter);
            FilterByCondition = NoConditionFilter;
            foreach (var c in conds) Conditions.Add(c);
            var raritys = await manager.GetRarities();
            var conditions = await manager.GetConditions();
            foreach (CardListing l in newListings)
            {
                TCGMarketPriceHistory pricing = await manager.GetNewestPrices(l.TCGplayerId);
               Listings.Add(new()
                {
                    Id = l.Id,
                    TCGplayerId = l.TCGplayerId,
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
        internal async void RefreshListings()
        {
            PopulateListings(await manager.GetListings(SearchText));
        }

        internal void Search()
        {
            ResultListings.Clear();
            var res = Listings.Where(x => x.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            if (InStockOnly)
                res = res.Where(x => x.TotalQuantity > 0);
            if (FilterByProductLine != null && FilterByProductLine.Id > -1)
            {
                res = res.Where(x => x.ProductLine.Id == FilterByProductLine.Id);
            }
            if (FilterBySet != null && FilterBySet.Id > -1)
            {
                res = res.Where(x => x.Set.Id == FilterBySet.Id);
            }
            if (FilterByCondition != null && FilterByCondition.Id > -1)
            {
                res = res.Where(x => x.Condition.Id == FilterByCondition.Id);
            }
            res = res.Take(MaxListings);
            foreach (var item in res)
            {
                ResultListings.Add(item);
            }
        }
    }
}
