namespace CardPilkApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("/Carts", typeof(CartListPage));
            Routing.RegisterRoute("/Repricer", typeof(RepricerHistoryPage));
            Routing.RegisterRoute("/Scryfall", typeof(ScryfallSettingsPage));
        }
    }
}
