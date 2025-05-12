using Azure.ResourceManager.Sql;

namespace AzureFwrMgr.Management;

internal class FirewallSyncProviderSql(ILoggerFactory loggerFactory) : AbstractFirewallSyncProvider(loggerFactory)
{
    public override async Task HandleAsync(FirewallSyncContext context, CancellationToken cancellationToken = default)
    {
        await HandleStandaloneAsync(context, cancellationToken);
        await HandleManagedInstancesAsync(context, cancellationToken);
        await HandleManagedInstancePoolsAsync(context, cancellationToken);
    }

    protected virtual async Task HandleStandaloneAsync(FirewallSyncContext context, CancellationToken cancellationToken = default)
    {
        var (subscription, _, dryRun, logger) = context;
        var servers = subscription.GetSqlServersAsync(cancellationToken: cancellationToken);

        // work on each server
        await foreach (var server in servers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // list all the rules
            var rules = server.GetSqlFirewallRules().GetAllAsync(cancellationToken);

            logger.LogDebug("Working on {ServerFQDN}", server.Data.FullyQualifiedDomainName);

            // check all the rules
            await foreach (var r in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // do not modify access from Azure Services
                if (SkipRule(r.Data.Name)) continue;

                // check if rule is known
                if (context.TryGetKnownRule(r.Data.Name, out var network))
                {
                    // if the IPs do not match, update it
                    if (!string.Equals(network.FirstUsable.ToString(), r.Data.StartIPAddress)
                        || !string.Equals(network.LastUsable.ToString(), r.Data.EndIPAddress))
                    {
                        if (dryRun)
                        {
                            logger.LogInformation("Updating rule '{RuleName}' in {ServerFQDN} to {IPNetwork} (dry run)",
                                                  r.Data.Name,
                                                  server.Data.FullyQualifiedDomainName,
                                                  network);
                        }
                        else
                        {
                            logger.LogInformation("Updating rule '{RuleName}' in {ServerFQDN} to {IPNetwork}",
                                                  r.Data.Name,
                                                  server.Data.FullyQualifiedDomainName,
                                                  network);
                            var data = new SqlFirewallRuleData
                            {
                                Name = r.Data.Name,
                                StartIPAddress = network.FirstUsable.ToString(),
                                EndIPAddress = network.LastUsable.ToString(),
                            };
                            await r.UpdateAsync(Azure.WaitUntil.Completed, data, cancellationToken);
                        }
                    }

                    // nothing more to do for this rule
                    continue;
                }

                // at this point, the rule has been checked and we have
                // established that it should not exist, so remove it
                if (dryRun)
                {
                    logger.LogInformation("Removing rule '{RuleName}' ({StartIPAddress} - {EndIPAddress}) in {ServerFQDN} (dry run)",
                                          r.Data.Name,
                                          r.Data.StartIPAddress,
                                          r.Data.EndIPAddress,
                                          server.Data.FullyQualifiedDomainName);
                }
                else
                {
                    logger.LogInformation("Removing rule '{RuleName}' ({StartIPAddress} - {EndIPAddress}) in {ServerFQDN}",
                                          r.Data.Name,
                                          r.Data.StartIPAddress,
                                          r.Data.EndIPAddress,
                                          server.Data.FullyQualifiedDomainName);
                    await r.DeleteAsync(Azure.WaitUntil.Completed, cancellationToken);
                }
            }
        }
    }

    protected virtual Task HandleManagedInstancesAsync(FirewallSyncContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task HandleManagedInstancePoolsAsync(FirewallSyncContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
