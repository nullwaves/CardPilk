using CardLib.Models;
using CardCondition = CardLib.Models.Condition;
using SQLite;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace CardLib
{

    public class CardManager
    {
        // Consts
        public static string[] TCGplayerHeaders = [
            "TCGplayer Id",
            "Product Line",
            "Set Name",
            "Product Name",
            "Title",
            "Number",
            "Rarity",
            "Condition",
            "TCG Market Price",
            "TCG Direct Low",
            "TCG Low Price With Shipping",
            "TCG Low Price",
            "Total Quantity",
            "Add to Quantity",
            "TCG Marketplace Price",
            "Photo URL"
            ];
        public static string[] Pricers = ["Low", "Market", "Shipped Low", "Direct Low"];

        // Props
        private string _dbPath;
        private SQLiteAsyncConnection _connection;

        public CardManager(string dbPath)
        {
            _dbPath = dbPath;
            _connection = new SQLiteAsyncConnection(_dbPath);
            MainThread.BeginInvokeOnMainThread(InitDatabase);
        }

        private async void InitDatabase()
        {
            CreateTablesResult res = await _connection.CreateTablesAsync(
                CreateFlags.AutoIncPK,
                [
                    typeof(ProductLine),
                    typeof(Rarity),
                    typeof(CardCondition),
                    typeof(Set),
                    typeof(CardListing),
                    typeof(TCGMarketPriceHistory),
                    typeof(RepricerUpdate),
                    typeof(Cart),
                ]
            );
            foreach (var item in res.Results.Keys)
            {
                Debug.WriteLine($"TABLES: {item.Name} - Status: {res.Results[item]}");
            }
        }

        #region TCGImport
        public async Task<TCGplayerImportResult> ImportFromTCGplayer(FileResult file)
        {
            Stream fs = await file.OpenReadAsync();
            string headerLn = fs.Readln();
            var headers = headerLn.SplitCSV();

            if (!ValidateHeaders(headers))
                return TCGplayerImportResult.InvalidHeaders;
            List<TCGplayerIOItem> items = new();
            int invalidLines = 0;
            while (fs.Position < fs.Length - 1)
            {
                string ln = fs.Readln();
                try
                {
                    items.Add(TCGplayerIOItem.FromRowString(ln));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    invalidLines++;
                }
            }
            return new TCGplayerImportResult(items.ToArray(), invalidLines);
        }

        public async Task<TCGplayerUpsertResult> UpsertTCGplayerRows(IEnumerable<TCGplayerIOItem> items)
        {
            TCGplayerUpsertResult results = new TCGplayerUpsertResult();
            foreach (TCGplayerIOItem rawItem in items)
            {
                decimal market = decimal.TryParse(rawItem.TCGMarketPrice, out market) ? market : 0;
                decimal direct = decimal.TryParse(
                    rawItem.TCGMarketPrice, out direct) ? direct : 0;
                decimal lowWithShipping = decimal.TryParse(rawItem.TCGLowPriceWithShipping, out lowWithShipping) ? lowWithShipping : 0;
                decimal low = decimal.TryParse(rawItem.TCGLowPrice, out low) ? low : 0;
                int qty = int.TryParse(rawItem.TotalQuantity, out qty) ? qty : 0;
                decimal myprice = decimal.TryParse(rawItem.TCGMarketplacePrice, out myprice) ? myprice : 0;
                var existing = await GetCardListingByTCGId(rawItem.TCGplayerId);
                if (existing == null)
                {
                    // Card Does not Exist, Create Card & Properties if needed
                    var productLine = await GetProductLineByName(rawItem.ProductLine);
                    if (productLine == null)
                    {
                        int prosert = await _connection.InsertAsync(new ProductLine() { Name = rawItem.ProductLine });
                        results.CreatedProductLines += prosert;
                        productLine = await GetProductLineByName(rawItem.ProductLine);
                        if (productLine == null) throw new Exception("Failed to Insert new Product Line");
                    }

                    var set = await GetSetByName(rawItem.SetName);
                    if (set == null)
                    {
                        int setin = await _connection.InsertAsync(new Set() { Name = rawItem.SetName });
                        results.CreatedSets += setin;
                        set = await GetSetByName(rawItem.SetName);
                        if (set == null) throw new Exception("Failed to Insert new Set");
                    }

                    var rarity = await GetRarityByName(rawItem.Rarity);
                    if (rarity == null)
                    {
                        int rarin = await _connection.InsertAsync(new Rarity() { Name = rawItem.Rarity });
                        results.CreatedRarities += rarin;
                        rarity = await GetRarityByName(rawItem.Rarity);
                        if (rarity == null) throw new Exception("Failed to Insert new Rarity");
                    }

                    var condition = await GetConditionByName(rawItem.Condition);
                    if (condition == null)
                    {
                        int conin = await _connection.InsertAsync(new CardCondition() { Name = rawItem.Condition });
                        results.CreatedConditions += conin;
                        condition = await GetConditionByName(rawItem.Condition);
                        if (condition == null) throw new Exception("Failed to Insert new Condition");
                    }

                    CardListing card = new()
                    {
                        TCGplayerId = rawItem.TCGplayerId,
                        ProductLineId = productLine.Id,
                        SetId = set.Id,
                        Name = rawItem.ProductName,
                        CardNumber = rawItem.CardNumber,
                        RarityId = rarity.Id,
                        ConditionId = condition.Id,
                        TotalQuantity = qty,
                        Price = myprice > 0 ? market : myprice,
                    };
                    int cardsert = await _connection.InsertAsync(card);
                    results.CreatedCards += cardsert;
                    existing = await GetCardListingByTCGId(card.TCGplayerId);
                    if (existing == null) throw new Exception("Failed to insert Card");
                }

                // Card Listing Exists, update prices
                TCGMarketPriceHistory prices = new TCGMarketPriceHistory()
                {
                    TCGplayerId = rawItem.TCGplayerId,
                    Timestamp = DateTime.UtcNow,
                    TCGMarketPrice = market,
                    TCGDirectLow = direct,
                    TCGLowPrice = low,
                    TCGLowPriceWithShipping = lowWithShipping,
                };
                int insert = await _connection.InsertAsync(prices);
                results.CreatedPrices += insert;

                if (existing.TotalQuantity != qty)
                {
                    existing.TotalQuantity = qty;
                    int qdate = await _connection.UpdateAsync(existing);
                    results.UpdatedQuantities += qdate;
                }
            }
            return results;
        }

        private bool ValidateHeaders(string[] headers)
        {
            if (headers.Length != TCGplayerHeaders.Length) return false;
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i] != TCGplayerHeaders[i])
                {
                    Debug.WriteLine($"Header Index {i} \"{headers[i]}\"does not match \"{TCGplayerHeaders[i]}\"");
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Getters
        public async Task<IEnumerable<CardListing>> FilterListings(Expression<Func<CardListing, bool>> expression, int limit)
        {
            return await _connection.Table<CardListing>().Where(expression).Take(limit).ToListAsync();
        }

        private async Task<CardListing?> GetCardListingByTCGId(int id)
        {
            return await _connection.Table<CardListing>().Where(x => x.TCGplayerId == id).FirstOrDefaultAsync();
        }

        private async Task<CardCondition?> GetConditionByName(string name)
        {
            return await _connection.Table<CardCondition>().Where(x => x.Name == name).FirstOrDefaultAsync();
        }

        private async Task<ProductLine?> GetProductLineByName(string name)
        {
            return await _connection.Table<ProductLine>().Where(x => x.Name == name).FirstOrDefaultAsync();
        }

        private async Task<Rarity?> GetRarityByName(string name)
        {
            return await _connection.Table<Rarity>().Where(x => x.Name == name).FirstOrDefaultAsync();
        }

        private async Task<Set?> GetSetByName(string name)
        {
            return await _connection.Table<Set>().Where(x => x.Name == name).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<CardListing>> GetListings()
        {
            return await _connection.Table<CardListing>().OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<IEnumerable<CardListing>> GetListings(string searchText)
        {
            return await _connection
                .Table<CardListing>()
                .OrderBy(x => x.Name)
                .Where(x => x.Name.ToUpper().Contains(searchText.ToUpper()))
                .ToListAsync();
        }

        public async Task<CardListing?> GetListingById(int id)
        {
            return await _connection.Table<CardListing>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<ProductLine>> GetProductLines()
        {
            return await _connection.Table<ProductLine>().OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<IEnumerable<Rarity>> GetRarities()
        {
            return await _connection.Table<Rarity>().OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<IEnumerable<CardCondition>> GetConditions()
        {
            return await _connection.Table<CardCondition>().OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<IEnumerable<Set>> GetSets()
        {
            return await _connection.Table<Set>().OrderBy(x => x.Name).ToListAsync();
        }

        public async Task<TCGMarketPriceHistory> GetNewestPrices(int tcgId)
        {
            var price = await _connection
                .Table<TCGMarketPriceHistory>()
                .OrderByDescending(x => x.Timestamp)
                .Where(x => x.TCGplayerId == tcgId)
                .FirstOrDefaultAsync();
            return price ?? new TCGMarketPriceHistory() { TCGplayerId = tcgId };
        }

        public async Task<IEnumerable<Cart>> GetCarts()
        {
            return await _connection.Table<Cart>().ToListAsync();
        }
        #endregion

        public async Task<int> UpsertCart(Cart cart)
        {
            if (cart.Id < 0)
                return await _connection.InsertAsync(cart);
            return await _connection.UpdateAsync(cart);
        }

        public async Task<int> DeleteCart(Cart cart)
        {
            return await _connection.DeleteAsync(cart);
        }

        public AsyncTableQuery<Cart> QueryCarts()
        {
            return _connection.Table<Cart>();
        }

        public async Task<RepricerUpdate> RepriceCards(bool includeOOS, string basepricer, decimal percent, decimal minPrice)
        {
            percent = percent / 100;
            RepricerUpdate update = new()
            {
                RunAgainstAllCards = includeOOS,
                BasePrice = basepricer,
                Percentage = percent,
                MinimumPrice = minPrice,
                PricesChanged = 0,
                GrossChange = 0,
                NetChange = 0,
            };
            if (Pricers.Contains(basepricer))
            {
                List<CardListing> toUpdate = includeOOS ? await _connection.Table<CardListing>().ToListAsync() : await _connection.Table<CardListing>().Where(x => x.TotalQuantity > 0).ToListAsync();
                Stack<int> NoOp = [];
                List<RepricerChange> changes = new List<RepricerChange>(toUpdate.Count);
                for (int i = 0; i < toUpdate.Count; i++)
                {
                    TCGMarketPriceHistory refPrices = await GetNewestPrices(toUpdate[i].TCGplayerId);
                    decimal oldPrice = toUpdate[i].Price;
                    decimal refPrice;
                    switch (basepricer)
                    {
                        case "Low":
                            refPrice = refPrices.TCGLowPrice; break;
                        case "Shipped Low":
                            refPrice = refPrices.TCGLowPriceWithShipping; break;
                        case "Direct Low":
                            refPrice = refPrices.TCGDirectLow; break;
                        case "Market":
                        default:
                            refPrice = refPrices.TCGMarketPrice; break;
                    }
                    decimal newPrice = Math.Round(refPrice * percent, 2);
                    if (newPrice != oldPrice)
                    {
                        var change = new RepricerChange
                        {
                            CardId = toUpdate[i].Id,
                            Old = oldPrice,
                            Delta = newPrice - oldPrice
                        };
                        if (newPrice < minPrice) newPrice = minPrice;
                        changes.Add(change);
                        toUpdate[i].Price = newPrice;
                        update.PricesChanged++;
                        update.GrossChange += Math.Abs(change.Delta);
                        update.NetChange += change.Delta;
                    }
                    else
                    {
                        NoOp.Push(i);
                    }
                }
                int remove;
                while (NoOp.TryPop(out remove))
                {
                    toUpdate.RemoveAt(remove);
                }
                update.SetChanges(changes.ToArray());
                int res = await _connection.UpdateAllAsync(toUpdate.ToArray());
                if (res != update.PricesChanged) Debug.WriteLine($"WARNING: Updated {res} listings, but expected to update {update.PricesChanged}");
            }
            await _connection.InsertAsync(update);
            return update;
        }

        public AsyncTableQuery<CardListing> QueryCardListings()
        {
            return _connection.Table<CardListing>();
        }

        #region Export Fns
        public async Task<Stream?> CreateCSVFromCart(int id)
        {
            if (await _connection.Table<Cart>().FirstOrDefaultAsync(x => x.Id == id) is Cart cart)
            {
                CartLineItem[] lines = cart.GetLines();
                List<TCGplayerIOItem> outlines = new();
                Dictionary<int, ProductLine> products = new();
                Dictionary<int, Set> sets = new();
                Dictionary<int, CardCondition> conditions = new();
                Dictionary<int, Rarity> raritys = new();
                StringBuilder sb = new(string.Join(',', TCGplayerHeaders.Select(x => $"\"{x}\"")));
                async Task<string> getProductLineName(int id)
                {
                    if (!products.ContainsKey(id))
                    {
                        products[id] = await _connection.Table<ProductLine>().FirstAsync(x => x.Id == id);
                    }
                    return products[id].Name;
                };
                async Task<string> getSetName(int id)
                {
                    if (!sets.ContainsKey(id))
                    {
                        sets[id] = await _connection.Table<Set>().FirstAsync(x => x.Id == id);
                    }
                    return sets[id].Name;
                };
                async Task<string> getConditionName(int id)
                {
                    if (!conditions.ContainsKey(id))
                    {
                        conditions[id] = await _connection.Table<CardCondition>().FirstAsync(x => x.Id == id);
                    }
                    return conditions[id].Name;
                };
                async Task<string> getRarityName(int id)
                {
                    if (!raritys.ContainsKey(id))
                    {
                        raritys[id] = await _connection.Table<Rarity>().FirstAsync(x => x.Id == id);
                    }
                    return raritys[id].Name;
                };
                for (int i = 0; i < lines.Length; i++)
                {
                    CartLineItem line = lines[i];
                    CardListing? listing = await GetListingById(line.CardId);
                    if (listing == null) { throw new Exception($"Database Integrity Exception: Could not locate card '{line.CardId}' refrenced in cart '{cart.Id}'."); }
                    TCGMarketPriceHistory prices = await GetNewestPrices(listing.TCGplayerId);
                    outlines.Add(new TCGplayerIOItem()
                    {
                        TCGplayerId = listing.TCGplayerId,
                        ProductLine = await getProductLineName(listing.ProductLineId),
                        SetName = await getSetName(listing.SetId),
                        ProductName = listing.Name,
                        Title = "",
                        CardNumber = listing.CardNumber,
                        Rarity = await getRarityName(listing.RarityId),
                        Condition = await getConditionName(listing.ConditionId),
                        TCGMarketPrice = prices.TCGMarketPrice.ToString("0.00"),
                        TCGDirectLow = prices.TCGDirectLow.ToString("0.00"),
                        TCGLowPriceWithShipping = prices.TCGLowPriceWithShipping.ToString("9.00"),
                        TCGLowPrice = prices.TCGLowPrice.ToString("0.00"),
                        TotalQuantity = listing.TotalQuantity.ToString(),
                        AddtoQuantity = line.Quantity.ToString(),
                        TCGMarketplacePrice = listing.Price.ToString("0.00"),
                        PhotoURL = ""
                    });
                    sb.Append($"\n{outlines[i].ToCSV()}");
                }
                return new MemoryStream(Encoding.Default.GetBytes(sb.ToString()));
            }
            else { return null; }
        }

        public async Task<IEnumerable<Set>> GetSetsFromProductLineId(int id)
        {
            string query = $"SELECT * FROM `Set` WHERE Id IN (SELECT SetId FROM CardListing WHERE ProductLineId=?);";
            object[] args = { id };
            var mapping = await _connection.GetMappingAsync<Set>();
            return await _connection.QueryAsync<Set>(query, args);
        }

        public async Task<IEnumerable<RepricerUpdate>> GetRepricerUpdates(int limit)
        {
            return await _connection.Table<RepricerUpdate>().OrderByDescending(x => x.CreatedAt).Take(limit).ToListAsync();
        }
        #endregion
    }

    public class TCGplayerImportResult
    {
        public bool ValidHeaders { get; set; }
        public int InvalidRows { get; set; }
        public TCGplayerIOItem[]? Items { get; set; }

        public static TCGplayerImportResult InvalidHeaders => new TCGplayerImportResult() { ValidHeaders = false };

        public TCGplayerImportResult(TCGplayerIOItem[]? items = null, int invalid = 0)
        {
            ValidHeaders = true;
            Items = items;
            InvalidRows = invalid;
        }
    }

    public class TCGplayerUpsertResult
    {
        public int CreatedCards { get; set; }
        public int CreatedPrices { get; set; }
        public int CreatedConditions { get; set; }
        public int CreatedProductLines { get; set; }
        public int CreatedRarities { get; set; }
        public int CreatedSets { get; set; }
        public int UpdatedQuantities { get; set; }
    }

}
