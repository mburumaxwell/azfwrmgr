using Azure.ResourceManager.CosmosDBForPostgreSql;

namespace AzureFwrMgr.Management;

internal class FirewallSyncProviderCosmosForPostgreSqlClusters(ILoggerFactory loggerFactory) : AbstractFirewallSyncProvider(loggerFactory)
{
    public async override Task HandleAsync(FirewallSyncContext context, CancellationToken cancellationToken = default)
    {
        var (subscription, _, dryRun, logger) = context;
        var clusters = subscription.GetCosmosDBForPostgreSqlClustersAsync(cancellationToken: cancellationToken);

        // work on each server
        await foreach (var server in clusters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // list all the rules
            var rules = server.GetCosmosDBForPostgreSqlFirewallRules().GetAllAsync(cancellationToken);

            var fqdn = server.Data.ServerNames[0].FullyQualifiedDomainName;
            logger.LogDebug("Working on {ServerFQDN}", fqdn);

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
                    if (!network.FirstUsable.Equals(r.Data.StartIPAddress) || !network.LastUsable.Equals(r.Data.EndIPAddress))
                    {
                        if (dryRun)
                        {
                            logger.LogInformation("Updating rule '{RuleName}' in {ServerFQDN} to {IPNetwork} (dry run)",
                                                  r.Data.Name,
                                                  fqdn,
                                                  network);
                        }
                        else
                        {
                            logger.LogInformation("Updating rule '{RuleName}' in {ServerFQDN} to {IPNetwork}",
                                                  r.Data.Name,
                                                  fqdn,
                                                  network);
                            var data = new CosmosDBForPostgreSqlFirewallRuleData(network.FirstUsable, network.LastUsable);
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
                                          fqdn);
                }
                else
                {
                    logger.LogInformation("Removing rule '{RuleName}' ({StartIPAddress} - {EndIPAddress}) in {ServerFQDN}",
                                          r.Data.Name,
                                          r.Data.StartIPAddress,
                                          r.Data.EndIPAddress,
                                          fqdn);
                    await r.DeleteAsync(Azure.WaitUntil.Completed, cancellationToken);
                }
            }
        }
    }
}
