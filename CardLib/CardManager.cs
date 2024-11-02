using CardLib.Models;
using CardCondition = CardLib.Models.Condition;
using SQLite;
using System.Diagnostics;

namespace CardLib
{

    public class CardManager
    {
        // Consts
        static string[] TCGplayerHeaders = [
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

        // Props
        private string _dbPath;
        private SQLiteAsyncConnection _connection;

        public CardManager(string dbPath)
        {
            _dbPath = dbPath;
            _connection = new SQLiteAsyncConnection(_dbPath);
            InitDatabase();
        }

        private void InitDatabase()
        {
            _connection.CreateTablesAsync(
                CreateFlags.AutoIncPK, 
                [
                    typeof(ProductLine),
                    typeof(Rarity),
                    typeof(CardCondition),
                    typeof(Set),
                    typeof(CardListing),
                    typeof(TCGMarketPriceHistory),
                ]
            );
        }

        public async Task<TCGplayerImportResult> ImportFromTCGplayer(FileResult file)
        {
            Stream fs = await file.OpenReadAsync();
            string headerLn = fs.Readln();
            var headers = headerLn.SplitCSV();

            if (!ValidateHeaders(headers))
                return TCGplayerImportResult.InvalidHeaders;
            List<TCGplayerIOItem> items = new();
            int invalidLines = 0;
            while(fs.Position < fs.Length-1)
            {
                string ln = fs.Readln();
                try
                {
                    items.Add(TCGplayerIOItem.FromRowString(ln));
                } catch (Exception e)
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
                        int rarin = await _connection.InsertAsync(new Rarity() { Name = rawItem.Rarity});
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

        private async Task<CardListing?> GetCardListingByTCGId(int id)
        {
            return await _connection.Table<CardListing>().Where(x => x.TCGplayerId == id).FirstOrDefaultAsync();
        }

        private async Task<CardCondition?> GetConditionByName(string name)
        {
            return await _connection.Table<CardCondition>().Where(x=>x.Name == name).FirstOrDefaultAsync();
        }

        private async Task<ProductLine?> GetProductLineByName(string name)
        {
            return await _connection.Table<ProductLine>().Where(x => x.Name == name).FirstOrDefaultAsync();
        }

        private async Task<Rarity?> GetRarityByName(string name)
        {
            return await _connection.Table<Rarity>().Where(x=>x.Name == name).FirstOrDefaultAsync();
        }

        private async Task<Set?> GetSetByName(string name)
        {
            return await _connection.Table<Set>().Where(x => x.Name == name).FirstOrDefaultAsync();
        }

        private bool ValidateHeaders(string[] headers)
        {
            if (headers.Length != TCGplayerHeaders.Length) return false;
            for (int i = 0; i < headers.Length; i++)
            {
                if (headers[i] != TCGplayerHeaders[i]) {
                    Debug.WriteLine($"Header Index {i} \"{headers[i]}\"does not match \"{TCGplayerHeaders[i]}\"");
                    return false;
                }
            }
            return true;
        }

        public async Task<IEnumerable<CardListing>> GetListings()
        {
            return await _connection.Table<CardListing>().OrderBy(x => x.Name).ToListAsync();
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
    }

    public class TCGplayerImportResult
    {
        public bool ValidHeaders { get; set;}
        public int InvalidRows { get; set;}
        public TCGplayerIOItem[]? Items { get; set;}

        public static TCGplayerImportResult InvalidHeaders => new TCGplayerImportResult() { ValidHeaders = false };

        public TCGplayerImportResult(TCGplayerIOItem[]? items = null, int invalid = 0)
        {
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
