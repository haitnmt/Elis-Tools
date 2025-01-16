using System.Text;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

//Set Console Support Vietnamese
Console.OutputEncoding = Encoding.UTF8;

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Add Serilog
builder.AddLogToElasticsearch();

//Add MemoryCache
builder.Services.AddMemoryCache();

//Add HybridCache
var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCaching(redisConnectionString);
#pragma warning restore EXTEXP0018

//Add ConnectionElisData
builder.Services.AddSingleton<IConnectionElisData, ConnectionElisData>();

//Add GiayChungNhanService
builder.Services.AddSingleton<IGiayChungNhanService, GiayChungNhanService>();
//Add ChuSuDungService
builder.Services.AddSingleton<IChuSuDungService, ChuSuDungService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGiayChungNhan();
app.MapChuSuDung();

app.Run();