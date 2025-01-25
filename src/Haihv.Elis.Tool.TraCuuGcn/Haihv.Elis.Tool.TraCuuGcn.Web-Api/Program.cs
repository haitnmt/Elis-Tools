using System.Text;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Authenticate;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Data;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Endpoints;
using Haihv.Elis.Tool.TraCuuGcn.Web_Api.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

//Add Jwt
builder.Services.AddSingleton(
    _ => new TokenProvider(builder.Configuration["Jwt:SecretKey"]!,
        builder.Configuration["Jwt:Issuer"]!,
        builder.Configuration["Jwt:Audience"]!,
        builder.Configuration.GetValue<int>("Jwt:ExpireMinutes")));

// Add service Authentication and Authorization for Identity Server
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero,
        };
    });

//Add ConnectionElisData
builder.Services.AddSingleton<IConnectionElisData, ConnectionElisData>();

//Add GcnQrService
builder.Services.AddSingleton<IGcnQrService, GcnQrService>();
//Add GiayChungNhanService
builder.Services.AddSingleton<IGiayChungNhanService, GiayChungNhanService>();
//Add ChuSuDungService
builder.Services.AddSingleton<IChuSuDungService, ChuSuDungService>();
//Add AuthenticationService
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();

//Configure CORS
var frontendUrls = builder.Configuration.GetSection("FrontendUrl").Get<string[]>();
if (frontendUrls is null || frontendUrls.Length == 0)
{
    frontendUrls = ["*"];
}
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsPolicyBuilder =>
    {
        corsPolicyBuilder.WithOrigins(frontendUrls)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//Use Cors
app.UseCors();

app.MapGiayChungNhan();
app.MapChuSuDung();
app.MapGcnQr();
app.MapAuthentication();

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

app.Run();