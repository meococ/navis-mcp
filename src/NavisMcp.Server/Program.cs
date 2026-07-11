using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using NavisMcp.Server.Infrastructure;
using NavisMcp.Server.Tools;

var options = ServerOptions.Parse(args);
var appPaths = new AppPaths();
var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(console =>
{
    console.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(appPaths.LogsPath, $"server-{DateTimeOffset.UtcNow:yyyyMMdd}.log")));
builder.Logging.SetMinimumLevel(options.Verbose ? LogLevel.Debug : LogLevel.Information);

builder.Services.AddSingleton(options);
builder.Services.AddSingleton(appPaths);
builder.Services.AddSingleton<SessionRegistry>();
builder.Services.AddSingleton<PipeRpcClient>();
builder.Services.AddSingleton<WriteGate>();
builder.Services.AddSingleton<AuditLogger>();

var toolTypes = new List<Type>
{
    typeof(NavisMetaTools),
    typeof(NavisSearchTools),
    typeof(NavisClashTools),
    typeof(NavisReportTools),
    typeof(NavisWriteTools)
};

if (ToolsetCatalog.IncludesAdvanced(options.Toolsets))
{
    toolTypes.Add(typeof(NavisAdvancedTools));
}

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools(toolTypes);

await builder.Build().RunAsync();
