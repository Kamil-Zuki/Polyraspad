using AgentService.AutoMapperProfiles;
using AgentService.Data;
using AgentService.Grpc;
using AgentService.Options;
using AgentService.Services;
using AgentService.Validations;
using FluentValidation;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using static Pvs.Content.Grpc.AnalyticsService;
using static Pvs.Content.Grpc.AIService;
using static Pvs.Content.Grpc.ContentService;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var builder = WebApplication.CreateBuilder(args);

var connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AgentServiceContext>(options =>
    options.UseNpgsql(connection, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "internal");
    }));

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.Configure<VocabularyServiceOptions>(builder.Configuration.GetSection(VocabularyServiceOptions.SectionName));

builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<ListAgentThreadsRequestValidator>();

builder.Services.AddScoped<IAgentThreadService, AgentThreadService>();
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
builder.Services.AddScoped<IVocabularyProjectAccessValidator, VocabularyProjectAccessValidator>();
builder.Services.AddScoped<IVocabularyGrpcClient, VocabularyGrpcClient>();

builder.Services.AddHttpClient<IAgentLlmProvider, OpenAiCompatibleAgentLlmProvider>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiOptions>>().Value;
    var baseUrl = (options.BaseUrl ?? "https://api.openai.com/v1").TrimEnd('/');
    client.BaseAddress = new Uri(baseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 600));
});

var vocabularyAddress = builder.Configuration.GetValue<string>("Vocabulary:GrpcAddress") ?? "http://localhost:5117";
builder.Services.AddGrpcClient<ContentServiceClient>(o => o.Address = new Uri(vocabularyAddress))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { EnableMultipleHttp2Connections = true, UseProxy = false });
builder.Services.AddGrpcClient<AnalyticsServiceClient>(o => o.Address = new Uri(vocabularyAddress))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { EnableMultipleHttp2Connections = true, UseProxy = false });
builder.Services.AddGrpcClient<AIServiceClient>(o => o.Address = new Uri(vocabularyAddress))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { EnableMultipleHttp2Connections = true, UseProxy = false });

builder.WebHost.ConfigureKestrel(options =>
{
    var inContainer = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
    var listenAddress = inContainer ? System.Net.IPAddress.Any : System.Net.IPAddress.Loopback;
    options.Listen(listenAddress, 5131, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
});

builder.Services.AddGrpc(options =>
{
    options.MaxSendMessageSize = 1000 * 1024 * 1024;
    options.MaxReceiveMessageSize = 1000 * 1024 * 1024;
    options.EnableDetailedErrors = true;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgentServiceContext>();
    db.Database.Migrate();
}

app.MapGrpcService<AgentGrpcService>();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program;
