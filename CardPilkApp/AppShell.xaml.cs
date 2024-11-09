namespace CardPilkApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("/Carts", typeof(CartListPage));
        }
    }
}
