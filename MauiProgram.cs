using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace NumberSearchApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<SimulationPage>();

            return builder.Build();
        }
    }
}
