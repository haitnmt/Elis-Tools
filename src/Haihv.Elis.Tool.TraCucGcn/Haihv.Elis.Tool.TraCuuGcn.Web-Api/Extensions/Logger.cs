using System.Reflection;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Serilog;

namespace Haihv.Elis.Tool.TraCuuGcn.Web_Api.Extensions;

public static class Logger
{
    private const string DefaultIndexName = "Elis-TraCuuGcn";

    /// <summary>
    /// Thêm cấu hình ghi log vào Elasticsearch.
    /// </summary>
    /// <param name="builder">Đối tượng HostApplicationBuilder.</param>
    /// <param name="sectionName"> Tên của section chứa cấu hình Elasticsearch trong file appsettings.json.</param>
    /// <param name="uriKey">Khóa cấu hình cho URI của Elasticsearch (mặc định là "Elasticsearch:Uris").</param>
    /// <param name="tokenKey">Khóa cấu hình cho API token của Elasticsearch (mặc định là "Elasticsearch:encoded").</param>
    /// <param name="usernameKey">Khóa cấu hình cho tên người dùng của Elasticsearch (mặc định là "Elasticsearch:username").</param>
    /// <param name="passwordKey">Khóa cấu hình cho mật khẩu của Elasticsearch (mặc định là "Elasticsearch:password").</param>
    public static void AddLogToElasticsearch(this WebApplicationBuilder builder,
        string sectionName = "Elasticsearch", string uriKey = "Uris",
        string? tokenKey = null, string? usernameKey = null, string? passwordKey = null)
    {
        if (builder.Services.All(service => service.ServiceType != typeof(IHttpContextAccessor)))
        {
            builder.Services.AddHttpContextAccessor();
        }

        var loggerConfiguration =
            builder.CreateLoggerConfiguration(sectionName, uriKey, tokenKey, usernameKey, passwordKey);
        builder.Services.AddSerilog(loggerConfiguration.CreateLogger());
    }

    /// <summary>
    /// Tạo cấu hình logger cho Elasticsearch.
    /// </summary>
    /// <param name="builder">Đối tượng HostApplicationBuilder.</param>
    /// <param name="sectionName"> Tên của section chứa cấu hình Elasticsearch trong file appsettings.json.</param>
    /// <param name="uriKey">Khóa cấu hình cho URI của Elasticsearch.</param>
    /// <param name="tokenKey">Khóa cấu hình cho token API của Elasticsearch.</param>
    /// <param name="usernameKey">Khóa cấu hình cho tên người dùng của Elasticsearch.</param>
    /// <param name="passwordKey">Khóa cấu hình cho mật khẩu của Elasticsearch.</param>
    /// <returns>Cấu hình logger.</returns>
    private static LoggerConfiguration CreateLoggerConfiguration(this IHostApplicationBuilder builder,
        string sectionName = "Elasticsearch", string uriKey = "Uris",
        string? tokenKey = null, string? usernameKey = null, string? passwordKey = null)
    {
        tokenKey ??= "Token";
        usernameKey ??= "Username";
        passwordKey ??= "Password";
        var configuration = builder.Configuration.GetSection(sectionName);
        var uris = (from stringUri in configuration.GetSection(uriKey).GetChildren()
            where !string.IsNullOrWhiteSpace(stringUri.Value)
            select new Uri(stringUri.Value!)).ToList();
        var token = configuration[tokenKey] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(token))
        {
            return builder.CreateLoggerConfiguration(uris, token);
        }

        var username = configuration[usernameKey] ?? string.Empty;
        var password = configuration[passwordKey] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            return builder.CreateLoggerConfiguration(uris, username, password);
        }

        return builder.CreateLoggerConfiguration(authorizationHeader: null);
    }

    /// <summary>
    /// Tạo cấu hình logger cho Elasticsearch.
    /// </summary>
    /// <param name="builder">Đối tượng HostApplicationBuilder.</param>
    /// <param name="uris">Danh sách các URI của Elasticsearch.</param>
    /// <param name="authorizationHeader">Header xác thực cho Elasticsearch.</param>
    /// <returns>Cấu hình logger.</returns>
    private static LoggerConfiguration CreateLoggerConfiguration(this IHostApplicationBuilder builder,
        ICollection<Uri>? uris = null,
        AuthorizationHeader? authorizationHeader = null)
    {
        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration);
        if (uris is null || uris.Count == 0 || authorizationHeader == null) return loggerConfiguration;
        var indexName = Assembly.GetEntryAssembly()?.GetName().Name?.ToLower().Replace(".", "-") ??
                        DefaultIndexName;
        var environment = builder.Environment.IsDevelopment() ? "Development" : "Production";
        // Ghi log vào Elasticsearch với mức Warning
        loggerConfiguration.WriteTo.Elasticsearch(uris, opts =>
        {
            opts.DataStream = new DataStreamName("logs", indexName, environment);
            opts.BootstrapMethod = BootstrapMethod.Failure;
        }, transport =>
        {
            transport.Authentication(authorizationHeader); // Basic Authentication
            transport.ServerCertificateValidationCallback(CertificateValidations.AllowAll);
            transport.RequestTimeout(TimeSpan.FromSeconds(30));
        });
        return loggerConfiguration;
    }

    /// <summary>
    /// Tạo cấu hình logger cho Elasticsearch.
    /// </summary>
    /// <param name="builder">Đối tượng HostApplicationBuilder.</param>
    /// <param name="uris">Danh sách các URI của Elasticsearch.</param>
    /// <param name="apiToken">API token xác thực Elasticsearch.</param>
    /// <returns>Cấu hình logger.</returns>
    private static LoggerConfiguration CreateLoggerConfiguration(this IHostApplicationBuilder builder,
        ICollection<Uri> uris,
        string apiToken)
        => CreateLoggerConfiguration(builder, uris, new ApiKey(apiToken));

    /// <summary>
    /// Tạo cấu hình logger cho Elasticsearch.
    /// </summary>
    /// <param name="builder">Đối tượng HostApplicationBuilder.</param>
    /// <param name="uris">Danh sách các URI của Elasticsearch.</param>
    /// <param name="username">Tên người dùng cho xác thực cơ bản.</param>
    /// <param name="password">Mật khẩu cho xác thực cơ bản.</param>
    /// <returns>Cấu hình logger.</returns>
    private static LoggerConfiguration CreateLoggerConfiguration(this IHostApplicationBuilder builder,
        ICollection<Uri> uris,
        string username, string password)
        => CreateLoggerConfiguration(builder, uris, new BasicAuthentication(username, password));
}