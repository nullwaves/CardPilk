using SQLite;

namespace CardLib.Models
{
    public class TCGMarketPriceHistory
    {
        [PrimaryKey]
        public int Id { get; set; }
        public int TCGplayerId { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal TCGMarketPrice { get; set; }
        public decimal TCGDirectLow { get; set; }
        public decimal TCGLowPriceWithShipping { get; set; }
        public decimal TCGLowPrice { get; set; }

        public TCGMarketPriceHistory() 
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}
