using Microsoft.Extensions.Logging;
using Exman.Services;
using System.Reflection;
using Exman.Converters;

namespace Exman
{
    public static class MauiProgram
    {
        // Create a property to expose the service provider for use in our pages
        public static IServiceProvider Services { get; private set; }
        
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register HttpClientFactory
            builder.Services.AddHttpClient();
            
            // Register Services
            builder.Services.AddSingleton<IEnvironmentService, EnvironmentService>();
            builder.Services.AddSingleton<ICollectionService, CollectionService>();
            builder.Services.AddSingleton<IRequestHistoryService, RequestHistoryService>();
            builder.Services.AddSingleton<IApiRequestService, ApiRequestService>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            
            // Set the static Services property
            Services = app.Services;
            
            return app;
        }
    }
}
