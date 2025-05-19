using System.Diagnostics.CodeAnalysis;
using System.Net;
using Azure.ResourceManager.Resources;

namespace AzureFwrMgr.Management;

public interface IFirewallSyncProvider
{
    Task HandleAsync(FirewallSyncContext context, CancellationToken cancellationToken = default);
}

public abstract class AbstractFirewallSyncProvider : IFirewallSyncProvider
{
    protected AbstractFirewallSyncProvider(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
    }

    protected ILogger Logger { get; }

    public abstract Task HandleAsync(FirewallSyncContext context, CancellationToken cancellationToken = default);

    protected static bool SkipRule(string name)
        => name == "AllowAllWindowsAzureIps" || name.StartsWith("AllowAllAzureServicesAndResourcesWithinAzureIps");
}

public sealed record FirewallSyncContext(SubscriptionResource Subscription, List<KnownFirewallRuleIp> Known, bool DryRun, ILogger Logger)
{
    public bool TryGetKnownRule(string? name, [NotNullWhen(true)] out IPNetwork2? address)
    {
        foreach (var rule in Known)
        {
            if (string.Equals(name, rule.Name))
            {
                address = rule.Network;
                return true;
            }
        }

        address = null;
        return false;
    }
}
