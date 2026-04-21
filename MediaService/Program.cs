using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using MediaService.Grpc;
using MediaService.Options;
using MediaService.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(
    builder.Configuration.GetSection(StorageOptions.SectionName));

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
    var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
    var endpoint = string.IsNullOrWhiteSpace(options.Endpoint)
        ? "http://localhost:9000"
        : options.Endpoint.Trim();
    var config = new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.USEast1,
        ServiceURL = endpoint,
        ForcePathStyle = options.UsePathStyle,
        UseHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase),
        EndpointDiscoveryEnabled = false,
        DisableHostPrefixInjection = true
    };
    return new AmazonS3Client(credentials, config);
});

builder.Services.AddScoped<IMediaStorageService, S3MediaStorageService>();

builder.WebHost.ConfigureKestrel(options =>
{
    var inContainer = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
    var listenAddress = inContainer ? System.Net.IPAddress.Any : System.Net.IPAddress.Loopback;
    options.Listen(listenAddress, 5121, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc(options =>
{
    options.MaxSendMessageSize = 1000 * 1024 * 1024;
    options.MaxReceiveMessageSize = 1000 * 1024 * 1024;
    options.EnableDetailedErrors = true;
});

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGrpcService<MediaGrpcService>();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();
