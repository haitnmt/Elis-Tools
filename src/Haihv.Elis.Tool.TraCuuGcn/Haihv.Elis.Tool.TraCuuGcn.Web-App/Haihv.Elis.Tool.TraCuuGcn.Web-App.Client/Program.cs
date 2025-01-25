using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddMudServices();
builder.Services.AddHttpClient(
        "Auth",
        opt => opt.BaseAddress = new Uri(builder.Configuration["BackendUrl"] ?? "https://localhost:7263/"));
await builder.Build().RunAsync();