using CardLib;
using CardLib.Models;
using CardPilkApp.DataObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
            ResetFilters();
        }

        internal static string fmtPrice(decimal price)
        {
            return price.ToString("$0.00").Replace("$0.00", "--");
        }

        internal async void ResetFilters()
        {
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
        }

        internal async Task PopulateListings(IEnumerable<CardListing> newListings)
        {
            Listings.Clear();
            var raritys = await manager.GetRarities();
            foreach (CardListing l in newListings.OrderByDescending(x => x.Price).DistinctBy(x => x.Name))
            {
                CardListingDO mlist = new()
                {
                    Id = l.Id,
                    TCGplayerId = l.TCGplayerId,
                    ProductLine = ProductLines.Where(x => x.Id == l.ProductLineId).First(),
                    Set = Sets.Where(x => x.Id == l.SetId).First(),
                    Name = l.Name,
                    CardNumber = l.CardNumber,
                    Rarity = raritys.Where(x => x.Id == l.RarityId).First(),
                    Variants = [],
                    ImagePath = l.ImageUri?.Length > 0 ? l.ImageUri : "default_card.png",
                };
                var variants = newListings.Where(x => x.Name == l.Name).ToArray();
                var imagehavers = variants.FirstOrDefault(x => x.ImageUri != null && x.ImageUri != "default_card.png" && x.ImageUri.Length > 0);
                if (imagehavers != null)
                {
                    mlist.ImagePath = imagehavers.ImageUri;
                }
                foreach (CardListing variant in variants)
                {
                    TCGMarketPriceHistory pricing = await manager.GetNewestPrices(l.TCGplayerId);
                    mlist.Variants.Add(new()
                    {
                        Id = variant.Id,
                        TCGplayerId = variant.TCGplayerId,
                        Condition = Conditions.Where(c => c.Id == variant.ConditionId).First(),
                        TotalQuantity = variant.TotalQuantity,
                        Price = variant.Price,
                        PriceString = variant.Price.ToString("$0.00"),
                        TCGMarket = fmtPrice(pricing.TCGMarketPrice),
                        TCGLow = fmtPrice(pricing.TCGLowPrice),
                        TCGShippedLow = fmtPrice(pricing.TCGLowPriceWithShipping),
                        TCGDirectLow = fmtPrice(pricing.TCGDirectLow),
                    });
                }
                Listings.Add(mlist);
            }
        }

        internal async void UpdateSetsFilter()
        {
            var oldsetfid = FilterBySet?.Id;
            Sets.Clear();
            Sets.Add(NoSetFilter);
            var line = FilterByProductLine;
            List<Set> lsets;
            if (line?.Id > -1)
            {
                lsets = (await manager.GetSetsFromProductLineId(line.Id)).ToList();
            }
            else
            {
                lsets = (await manager.GetSets()).ToList();
            }
            foreach (Set s in lsets) Sets.Add(s);
            if (oldsetfid is int fid && lsets.Where(x => x.Id == fid).FirstOrDefault() is Set newsetf)
                FilterBySetIndex = Sets.IndexOf(newsetf);
            else FilterBySetIndex = 0;
            Search();
        }

        [RelayCommand]
        internal void Search() { MainThread.BeginInvokeOnMainThread(ExecuteSearch); }

        internal async void ExecuteSearch()
        {
            var res = manager.QueryCardListings().Where(x => x.Name.ToUpper().Contains(SearchText.ToUpper()) || x.CardNumber.Contains(SearchText));
            if (InStockOnly)
                res = res.Where(x => x.TotalQuantity > 0);
            if (FilterByProductLine != null && FilterByProductLine.Id > -1)
            {
                res = res.Where(x => x.ProductLineId == FilterByProductLine.Id);
            }
            if (FilterBySet != null && FilterBySet.Id > -1)
            {
                res = res.Where(x => x.SetId == FilterBySet.Id);
            }
            if (FilterByCondition != null && FilterByCondition.Id > -1)
            {
                res = res.Where(x => x.ConditionId == FilterByCondition.Id);
            }
            var all = await res.ToListAsync();
            var uniqs = all.Select(x => x.Name).DistinctBy(x => x).Take(MaxListings).ToArray(); ;
            await PopulateListings(await manager.QueryCardListings().Where(x => uniqs.Contains(x.Name)).ToListAsync());
            ResultListings.Clear();
            foreach (var item in Listings)
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
        internal async Task AddToCartById(int variantId)
        {
            var variant = await GetVariantById(variantId);
            if (variant == null) return;
            AddOneToCart(variant);
        }

        [RelayCommand]
        internal void RemoveOneFromCartById(int variantId)
        {
            var line = CartItems.Where(x => x.Id == variantId).FirstOrDefault();
            if (line == null) { App.Alerts.ShowAlert("Cart Error", $"Failed to remove card id: {variantId}"); return; }
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
            var line = CartItems.Where(x => x.Id == variantId).FirstOrDefault();
            if (line == null) { App.Alerts.ShowAlert("Cart Error", $"Failed to remove card id: {variantId}"); return; }
            CartItems.Remove(line);
            RecalculateCart();
        }

        [RelayCommand]
        internal async Task ClearCart()
        {
            var ans = await App.Alerts.ShowConfirmationAsync("CardPilk Cart", "Clear cart?");
            if (ans)
            {
                CartItems.Clear();
                RecalculateCart();
            }
        }

        internal bool AddOneToCart(CardVariantDO card)
        {
            var listing = Listings.Where(x => x.Variants.Where(v => v.Id == card.Id).Any()).FirstOrDefault();
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

        private async Task<CardVariantDO?> GetVariantById(int variantId)
        {
            CardListing? variant = await manager.GetListingById(variantId);
            if (variant == null) return null;
            TCGMarketPriceHistory pricing = await manager.GetNewestPrices(variant.TCGplayerId);
            return new CardVariantDO()
            {
                Id = variant.Id,
                TCGplayerId = variant.TCGplayerId,
                Condition = Conditions.Where(c => c.Id == variant.ConditionId).First(),
                TotalQuantity = variant.TotalQuantity,
                Price = variant.Price,
                PriceString = variant.Price.ToString("$0.00"),
                TCGMarket = fmtPrice(pricing.TCGMarketPrice),
                TCGLow = fmtPrice(pricing.TCGLowPrice),
                TCGShippedLow = fmtPrice(pricing.TCGLowPriceWithShipping),
                TCGDirectLow = fmtPrice(pricing.TCGDirectLow),
            };
        }

        private void RecalculateCart()
        {
            int q = CartItems.Sum(x => x.Quantity);
            CartCountString = $"{q} item" + (q > 1 ? "s" : string.Empty);
            decimal subtotal = 0;
            foreach (var i in CartItems) subtotal += i.Price * i.Quantity;
            CartSubtotalString = subtotal.ToString("$0.00");
        }

        public static string[] Pricers = ["Low", "Market", "Shipped Low", "Direct Low"];

        public async Task<int> SaveCart()
        {
            List<CartLineItem> items = new();
            for (int i = 0; i < CartItems.Count; i++)
            {
                var c = CartItems[i];
                items.Add(new()
                {
                    CardId = c.Id,
                    Name = c.Name + (c.Condition.Length > 0 ? $" - {c.Condition}" : string.Empty),
                    Price = CartItems[i].Price,
                    Quantity = CartItems[i].Quantity,
                    Discount = 0,
                    Subtotal = CartItems[i].Subtotal,
                });
            }
            Cart cart = new Cart();
            var success = cart.TrySetData(new() { LineItems = items.ToArray() });
            var ret = 0;
            if (success)
            {
                ret = await manager.UpsertCart(cart);
                if (ret > 0)
                {
                    CartItems.Clear();
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Cart Error", "Failed to Save Cart", "OK");
            }
            return ret;
        }
    }
}
