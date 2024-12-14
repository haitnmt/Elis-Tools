using Haihv.Elis.Tool.ChuyenDvhc.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using MudBlazor.Services;

namespace Haihv.Elis.Tool.ChuyenDvhc
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
            builder.Services.AddMemoryCache();
            builder.Services.AddMauiBlazorWebView();
            
            // Đăng ký FileService
            builder.Services.AddSingleton<IFileService, FileService>();
            
            // Đăng ký MudBlazor
            builder.Services.AddMudServices();


#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif
            
            return builder.Build();
        }
    }
}