using CardPilkApp.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace CardPilkApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<IAlertService, AlertService>();
            builder.Services.AddSingleton<ICardLibService, CardLibService>();
            builder.Services.AddSingleton<IScryfallService, ScryfallService>();
            builder.Services.AddSingleton<ILorcastService, LorcastService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
