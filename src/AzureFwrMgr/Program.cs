using AzureFwrMgr;

var builder = Host.CreateApplicationBuilder();

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Logging:LogLevel:Default"] = "Information",
    ["Logging:LogLevel:Microsoft"] = "Warning",
    ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Warning",
    ["Logging:Debug:LogLevel:Default"] = "None",

    ["Logging:LogLevel:AzureFwrMgr"] = builder.Environment.IsDevelopment() ? "Trace" : "Information",

    ["Logging:Console:FormatterName"] = "cli",
    ["Logging:Console:FormatterOptions:SingleLine"] = "True",
    ["Logging:Console:FormatterOptions:IncludeCategory"] = "False",
    ["Logging:Console:FormatterOptions:IncludeEventId"] = "False",
    ["Logging:Console:FormatterOptions:TimestampFormat"] = "yyyy-MM-dd HH:mm:ss ",
});

// configure logging
builder.Logging.AddCliConsole();

// register services
builder.Services.AddTransient<FirewallManager>();

// build and start the host
using var host = builder.Build();
await host.StartAsync();

// prepare the root command
var configFileOption = new Option<string>(name: "--config", aliases: ["-f"]) { Description = "Path to the configuration file", Required = true, };
var interactiveOption = new Option<bool>(name: "--interactive") { Description = "Allow interactive authentication mode (opens a browser for authentication).", };
var dryRunOption = new Option<bool>(name: "--dry-run") { Description = "Test the logic without actually updating the DNS records.", };
var root = new RootCommand("Azure Firewall Rules Manager") { configFileOption, interactiveOption, dryRunOption };
root.SetAction((parseResult, cancellationToken) =>
{
    var configFile = parseResult.GetValue(configFileOption)!;
    var interactive = parseResult.GetValue(interactiveOption);
    var dryRun = parseResult.GetValue(dryRunOption);

    using var scope = host.Services.CreateScope();
    var provider = scope.ServiceProvider;
    var manager = provider.GetRequiredService<FirewallManager>();
    return manager.ExecuteAsync(configFile, interactive, dryRun, cancellationToken);
});

// execute the command
try
{
    return await root.Parse(args).InvokeAsync();
}
finally
{
    // stop the host, this will stop and dispose the services which flushes OpenTelemetry data
    await host.StopAsync();
}
