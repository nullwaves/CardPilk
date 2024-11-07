using CardPilkApp.Services;

namespace CardPilkApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider;
        public static IAlertService Alerts;

        public App(IServiceProvider provider)
        {
            InitializeComponent();
            ServiceProvider = provider;
            Alerts = provider.GetService<IAlertService>();
            MainPage = new AppShell();
        }
    }
}
