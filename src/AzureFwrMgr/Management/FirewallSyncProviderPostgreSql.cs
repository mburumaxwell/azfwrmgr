using Azure.ResourceManager.PostgreSql;
using Azure.ResourceManager.PostgreSql.FlexibleServers;

namespace AzureFwrMgr.Management;

internal class FirewallSyncProviderPostgreSql(ILoggerFactory loggerFactory) : AbstractFirewallSyncProvider(loggerFactory)
{
    public async override Task HandleAsync(FirewallSyncContext context, CancellationToken cancellationToken = default)
    {
        await HandleLegacyAsync(context, cancellationToken);
        await HandleFlexibleAsync(context, cancellationToken);
    }

    protected virtual async Task HandleLegacyAsync(FirewallSyncContext context, CancellationToken cancellationToken = default)
    {
        var (subscription, _, dryRun, logger) = context;
        var servers = subscription.GetPostgreSqlServersAsync(cancellationToken: cancellationToken);

        // work on each server
        await foreach (var server in servers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // list all the rules
            var rules = server.GetPostgreSqlFirewallRules().GetAllAsync(cancellationToken);

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
                    if (!network.FirstUsable.Equals(r.Data.StartIPAddress)
                        || !network.LastUsable.Equals(r.Data.EndIPAddress))
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
                            var data = new PostgreSqlFirewallRuleData(network.FirstUsable, network.LastUsable);
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

    protected virtual async Task HandleFlexibleAsync(FirewallSyncContext context, CancellationToken cancellationToken = default)
    {
        var (subscription, _, dryRun, logger) = context;
        var servers = subscription.GetPostgreSqlFlexibleServersAsync(cancellationToken: cancellationToken);

        // work on each server
        await foreach (var server in servers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // list all the rules
            var rules = server.GetPostgreSqlFlexibleServerFirewallRules().GetAllAsync(cancellationToken);

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
                    if (!network.FirstUsable.Equals(r.Data.StartIPAddress)
                        || !network.LastUsable.Equals(r.Data.EndIPAddress))
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
                            var data = new PostgreSqlFlexibleServerFirewallRuleData(network.FirstUsable, network.LastUsable);
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
}
