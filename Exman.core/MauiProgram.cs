using Microsoft.Extensions.Logging;
using Exman.Services;

namespace Exman
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

            builder.Services.AddMauiBlazorWebView();
            
            // Register HttpClientFactory
            builder.Services.AddHttpClient();
            
            // Register Services
            builder.Services.AddSingleton<IEnvironmentService, EnvironmentService>();
            builder.Services.AddSingleton<ICollectionService, CollectionService>();
            builder.Services.AddSingleton<IRequestHistoryService, RequestHistoryService>();
            
            // The ApiRequestService depends on both EnvironmentService and RequestHistoryService
            builder.Services.AddSingleton<IApiRequestService, ApiRequestService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
