using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.ResourceManager;
using AzureFwrMgr.Management;
using SC = AzureFwrMgr.AzureFwrMgrSerializerContext;

namespace AzureFwrMgr;

internal class FirewallManager(ILoggerFactory loggerFactory)
{
    private readonly ILogger logger = loggerFactory.CreateLogger<FirewallManager>();

    public async Task<int> ExecuteAsync(string configFile, bool interactive, bool dryRun, CancellationToken cancellationToken)
    {
        if (!File.Exists(configFile))
        {
            logger.LogError("Config file '{ConfigFile}' not found", configFile);
            return -1;
        }

        FirewallManagerConfig? config;
        try
        {
            await using var stream = File.OpenRead(configFile);
            config = await JsonSerializer.DeserializeAsync(stream, SC.Default.FirewallManagerConfig, cancellationToken);
        }
        catch (JsonException je)
        {
            logger.LogError(je, "Config file contains invalid JSON");
            return -1;
        }

        if (config is null)
        {
            logger.LogError("Deserialized config is null which is unexpected!");
            return -1;
        }

        return await ExecuteAsync(config, interactive, dryRun, cancellationToken);
    }

    public async Task<int> ExecuteAsync(FirewallManagerConfig config, bool interactive, bool dryRun, CancellationToken cancellationToken)
    {
        // prepare client and credential
        var credential = new DefaultAzureCredential(includeInteractiveCredentials: interactive);
        var client = new ArmClient(credential);

        // prepare providers
        var providers = new List<IFirewallSyncProvider>();
        if (config.CosmosForPostgreSql) providers.Add(new FirewallSyncProviderCosmosForPostgreSqlClusters(loggerFactory));
        if (config.MongoCluster) providers.Add(new FirewallSyncProviderMongoCluster(loggerFactory));
        if (config.PostgreSql) providers.Add(new FirewallSyncProviderPostgreSql(loggerFactory));
        if (config.Sql) providers.Add(new FirewallSyncProviderSql(loggerFactory));

        // initialize the rules (convert FQDNs to known IPs)
        logger.LogTrace("Initializing IPs to use for firewall rules ...");
        var known = await GetKnownRulesAsync(config, cancellationToken);
        logger.LogInformation("Known Firewall Rules:\r\n{Rules}\r\n",
                              string.Join("\r\n", known.Select(kvp => $"{kvp.Name}: {kvp.Network}")));

        // check for duplicate names
        var duplicateNames = known.GroupBy(r => r.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateNames.Count > 0)
        {
            logger.LogError("Duplicate names are not allowed: {DuplicateNames}", duplicateNames);
            return -1;
        }

        logger.LogDebug("Fetching subscriptions ...");
        var subscriptions = config.Subscriptions ?? [];
        var subs = client.GetSubscriptions().GetAllAsync(cancellationToken);
        await foreach (var sub in subs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // if we have a list of subscriptions to check, skip the ones not in the list
            if (subscriptions.Count > 0
                && !subscriptions.Contains(sub.Data.SubscriptionId, StringComparer.OrdinalIgnoreCase)
                && !subscriptions.Contains(sub.Data.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                logger.LogDebug("Skipping subscription '{SubscriptionName}' ...", sub.Data.DisplayName); // no subscription ID for security reasons
                continue;
            }

            // create context and work through each provider
            var context = new FirewallSyncContext(sub, known, dryRun, logger);
            logger.LogDebug("Checking firewall rules in '{SubscriptionName}' ...", sub.Data.DisplayName); // no subscription ID for security reasons
            foreach (var prov in providers)
            {
                await prov.HandleAsync(context, cancellationToken);
            }
        }

        logger.LogInformation("Finished");
        return 0;
    }

    internal virtual async Task<List<KnownFirewallRuleIp>> GetKnownRulesAsync(FirewallManagerConfig config, CancellationToken cancellationToken)
    {
        var results = new List<KnownFirewallRuleIp>(config.KnownNetworks ?? []);

        var separator = config.Separator ?? "-";
        var fqdns = config.KnownFqdns ?? [];
        foreach (var (name, fqdn) in fqdns)
        {
            var networks = await GetNetworksAsync(fqdn, cancellationToken);
            if (networks is null) continue;
            if (networks.Count == 1)
            {
                results.Add(new KnownFirewallRuleIp(name, networks[0]));
                continue;
            }

            foreach (var (index, net) in networks.Index())
            {
                results.Add(new KnownFirewallRuleIp($"{name}{separator}{index}", net));
            }
        }

        return results;
    }
    internal virtual async Task<IReadOnlyList<IPNetwork2>?> GetNetworksAsync(string fqdn, CancellationToken cancellationToken)
    {
        IPHostEntry? entry;
        try
        {
            entry = await Dns.GetHostEntryAsync(fqdn, cancellationToken);
        }
        catch (System.Net.Sockets.SocketException se)
        {
            logger.LogError(se, "Unable to resolve FQDN {Fqdn}", fqdn);
            return null;
        }
        return GetNetworks(entry);
    }

    internal virtual IReadOnlyList<IPNetwork2> GetNetworks(IPHostEntry entry)
    {
        var networks = new List<IPNetwork2>();
        foreach (var address in entry.AddressList)
        {
            // host‐routes: /32 for IPv4, /128 for IPv6
            var addr = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
            var prefix = addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
            networks.Add(IPNetwork2.Parse($"{addr}/{prefix}"));
        }
        var reduced = IPNetwork2.Supernet([.. networks]);
        return [.. reduced];
    }
}

/// <summary>
/// 
/// </summary>
/// <param name="KnownFqdns">Known FQDNs that can be used to resolve IPs then networks.</param>
/// <param name="KnownNetworks">Known IP networks. Used to skip DNS resolution.</param>
/// <param name="Subscriptions">Name or ID of subscriptions allowed. If none are provided, all subscriptions are checked.</param>
public record FirewallManagerConfig(
    [property: JsonPropertyName("fqdns")] List<KnownFirewallRuleFqdn>? KnownFqdns,
    [property: JsonPropertyName("networks")] List<KnownFirewallRuleIp>? KnownNetworks,
    [property: JsonPropertyName("subscriptions")] List<string>? Subscriptions,
    [property: JsonPropertyName("separator")] string Separator = "-",
    [property: JsonPropertyName("cosmosForPostgreSql")] bool CosmosForPostgreSql = true,
    [property: JsonPropertyName("mongoCluster")] bool MongoCluster = true,
    [property: JsonPropertyName("postgres")] bool PostgreSql = true,
    [property: JsonPropertyName("sql")] bool Sql = true);

public record KnownFirewallRuleFqdn(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("fqdn")] string Fqdn);

public record KnownFirewallRuleIp(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("network")] IPNetwork2 Network);
