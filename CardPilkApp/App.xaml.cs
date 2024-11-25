#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
using CardPilkApp.Services;

namespace CardPilkApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider;
        public static IAlertService Alerts;
        public static ICardLibService CardLib;
        public static IScryfallService Scryfall;

        public App(IServiceProvider provider)
        {
            InitializeComponent();
            ServiceProvider = provider;
            Alerts = provider.GetService<IAlertService>();
            CardLib = provider.GetService<ICardLibService>();
            Scryfall = provider.GetService<IScryfallService>();
            MainPage = new AppShell();
        }
    }
}
