using CardLib;
using CardLib.Models;
using CardPilkApp.DataObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Animations;
using SQLitePCL;
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
        [ObservableProperty]
        private int filterBySetIndex = -1;
        public CardCondition FilterByCondition { get; set; } = NoConditionFilter;
        public ObservableCollection<CardListingDO> ResultListings { get; set; }

        // Cart Properties
        public ObservableCollection<CartLineItemDO> CartItems { get; set; }
        [ObservableProperty] private string cartCountString = "0 items";
        [ObservableProperty] private string cartSubtotalString = "$0.00";

        public CardListViewModel(CardManager manager)
        {
            MaxListingsOptions = [25, 50, 100, 200];
            MaxListings = 50;
            Listings = [];
            ProductLines = [];
            Sets = [];
            Conditions = [];
            ResultListings = [];
            SearchText = string.Empty;
            CartItems = [];
            this.manager = manager;
        }

        internal static string fmtPrice(decimal price)
        {
            return price.ToString("$0.00").Replace("$0.00", "--");
        }

        internal async Task PopulateListings(IEnumerable<CardListing> newListings)
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
            foreach (var s in sets)
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
            foreach (CardListing l in newListings.OrderByDescending(x => x.Price).DistinctBy(x => x.Name))
            {
                CardListingDO mlist = new()
                {
                    Id = l.Id,
                    TCGplayerId = l.TCGplayerId,
                    ProductLine = lines.Where(x => x.Id == l.ProductLineId).First(),
                    Set = sets.Where(x => x.Id == l.SetId).First(),
                    Name = l.Name,
                    CardNumber = l.CardNumber,
                    Rarity = raritys.Where(x => x.Id == l.RarityId).First(),
                    Variants = [],
                };
                var vars = newListings.Where(x => x.Name == l.Name).ToArray();
                foreach (CardListing sl in vars)
                {
                    TCGMarketPriceHistory pricing = await manager.GetNewestPrices(l.TCGplayerId);
                    mlist.Variants.Add(new()
                    {
                        Id = sl.Id,
                        TCGplayerId = sl.TCGplayerId,
                        Condition = conditions.Where(c => c.Id == sl.ConditionId).First(),
                        TotalQuantity = sl.TotalQuantity,
                        Price = sl.Price,
                        PriceString = fmtPrice(sl.Price),
                        TCGMarket = fmtPrice(pricing.TCGMarketPrice),
                        TCGLow = fmtPrice(pricing.TCGLowPrice),
                        TCGShippedLow = fmtPrice(pricing.TCGLowPriceWithShipping),
                        TCGDirectLow = fmtPrice(pricing.TCGDirectLow),
                    });
                }
                Listings.Add(mlist);
            }
        }

        internal async Task RefreshListings()
        {
            await PopulateListings(await manager.GetListings(SearchText));
            Search();
        }

        internal void UpdateSetsFilter()
        {
            var oldsetfid = FilterBySet?.Id;
            Sets.Clear();
            Sets.Add(NoSetFilter);
            var line = FilterByProductLine;
            List<Set> lsets;
            if (line?.Id > -1)
            {
                lsets = Listings
                    .Where(x => x.ProductLine.Id == FilterByProductLine.Id)
                    .DistinctBy(x => x.Set.Id)
                    .Select(x => x.Set)
                    .ToList();
            }
            else
            {
                lsets = Listings.DistinctBy(x => x.Set.Id).Select(x => x.Set).ToList();
            }
            foreach (Set s in lsets) Sets.Add(s);
            if (oldsetfid is int fid && lsets.Where(x => x.Id == fid).FirstOrDefault() is Set newsetf)
                FilterBySetIndex = Sets.IndexOf(newsetf);
            else FilterBySetIndex = 0;
            Search();
        }

        internal void Search()
        {
            ResultListings.Clear();
            var res = Listings.Where(x => x.SearchString.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            if (InStockOnly)
                res = res.Where(x => x.SumQuantity > 0);
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
                res = res.Where(x => x.Variants.Where(v => v.Condition.Id == FilterByCondition.Id).Any());
            }
            res = res.Take(MaxListings);
            foreach (var item in res)
            {
                ResultListings.Add(item);
            }
        }

        [RelayCommand]
        internal void AddToCart(CardVariantDO card)
        {
            bool success = AddOneToCart(card);
            if (!success) App.Alerts.ShowAlert("Cart Error", $"Failed to add card id: {card.Id} to the cart.");
        }

        [RelayCommand]
        internal void AddToCartById(int variantId)
        {
            var variant = GetVariantById(variantId);
            if (variant == null) return;
            AddOneToCart(variant);
        }

        [RelayCommand]
        internal void RemoveOneFromCartById(int variantId)
        {
            var variant = GetVariantById(variantId);
            if (variant == null) return;
            var line = CartItems.Where(x => x.Id == variant.Id).FirstOrDefault();
            if (line == null) { App.Alerts.ShowAlert("Cart Error", $"Failed to remove card id: {variant.Id}"); return; }
            line.Quantity--;
            if (line.Quantity == 0)
            {
                CartItems.Remove(line);
            }
            RecalculateCart();
        }

        [RelayCommand]
        internal void RemoveFromCartById(int variantId)
        {
            var variant = GetVariantById(variantId);
            if (variant == null) return;
            var line = CartItems.Where(x => x.Id == variant.Id).FirstOrDefault();
            if (line == null) { App.Alerts.ShowAlert("Cart Error", $"Failed to remove card id: {variant.Id}"); return; }
            CartItems.Remove(line);
            RecalculateCart();
        }

        internal bool AddOneToCart(CardVariantDO card)
        {
            var listing = Listings.Where(x => x.Id == card.Id).FirstOrDefault();
            if (listing == null) return false;
            if (CartItems.Where(x => x.Id == card.Id).FirstOrDefault() is CartLineItemDO cartItem)
            {
                cartItem.Quantity += 1;
            }
            else
            {
                string name = $"{listing.Name} " +
                    (listing.CardNumber.Length > 0 ? $"- {listing.CardNumber} " : string.Empty) + 
                    (listing.Rarity.Name.Length > 0 ? $"- {listing.Rarity.Name}" : string.Empty);
                CartItems.Add(new()
                {
                    Id = card.Id,
                    Name = name,
                    Condition = card.Condition.Name,
                    Price = card.Price,
                    Quantity = 1,
                });
            }
            RecalculateCart();
            return true;
        }

        private CardVariantDO? GetVariantById(int variantId) => Listings
            .Where(x => x.Id == variantId)
            .FirstOrDefault()?
            .Variants.Where(x => x.Id == variantId)
            .FirstOrDefault();
        private void RecalculateCart()
        {
            int q = CartItems.Sum(x => x.Quantity);
            CartCountString = $"{q} item" + (q > 1 ? "s" : string.Empty);
            decimal subtotal = 0;
            foreach (var i in CartItems) subtotal += i.Price * i.Quantity;
            CartSubtotalString = subtotal.ToString("$0.00");
        }
    }
}
