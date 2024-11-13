namespace CardLib
{
    public class TCGplayerIOItem
    {
        public int TCGplayerId;
        public required string ProductLine, SetName, ProductName, Title, CardNumber, Rarity, Condition,
            // Possibly Blank Fields
            TCGMarketPrice, TCGDirectLow, TCGLowPriceWithShipping, TCGLowPrice,
            TotalQuantity, AddtoQuantity, TCGMarketplacePrice, PhotoURL;

        public static TCGplayerIOItem FromRowString(string str)
        {
            var split = str.SplitCSV();

            return new TCGplayerIOItem()
            {
                TCGplayerId = int.Parse(split[0]),
                ProductLine = split[1],
                SetName = split[2],
                ProductName = split[3],
                Title = split[4],
                CardNumber = split[5],
                Rarity = split[6],
                Condition = split[7],
                TCGMarketPrice = split[8],
                TCGDirectLow = split[9],
                TCGLowPriceWithShipping = split[10],
                TCGLowPrice = split[11],
                TotalQuantity = split[12],
                AddtoQuantity = split[13],
                TCGMarketplacePrice = split[14],
                PhotoURL = split[15]
            };
        }

        /// <summary>
        /// Format Item as CSV line without any newlines
        /// </summary>
        /// <returns>16-column CSV Formatted string</returns>
        public string ToCSV()
        {
            return $"\"{TCGplayerId}\",\"{ProductLine}\",\"{SetName}\",\"{ProductName}\",\"{Title}\",\"{CardNumber}\",\"{Rarity}\",\"{Condition}\",\"{TCGMarketPrice}\",\"{TCGDirectLow}\",\"{TCGLowPriceWithShipping}\",\"{TCGLowPrice}\",\"{TotalQuantity}\",\"{AddtoQuantity}\",\"{TCGMarketplacePrice}\",\"{PhotoURL}\"";
        }
    }
}
