using Azure.ResourceManager.MongoCluster;
using Azure.ResourceManager.MongoCluster.Models;

namespace AzureFwrMgr.Management;

internal class FirewallSyncProviderMongoCluster(ILoggerFactory loggerFactory) : AbstractFirewallSyncProvider(loggerFactory)
{
    public override async Task HandleAsync(FirewallSyncContext context, CancellationToken cancellationToken = default)
    {
        var (subscription, _, dryRun, logger) = context;
        var clusters = subscription.GetMongoClustersAsync(cancellationToken: cancellationToken);

        // work on each server
        await foreach (var server in clusters)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // list all the rules
            var rules = server.GetMongoClusterFirewallRules().GetAllAsync(cancellationToken);

            logger.LogDebug("Working on {ServerId}", server.Data.Id);

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
                    if (!network.FirstUsable.ToString().Equals(r.Data.Properties.StartIPAddress)
                        || !network.LastUsable.ToString().Equals(r.Data.Properties.EndIPAddress))
                    {
                        if (dryRun)
                        {
                            logger.LogInformation("Updating rule '{RuleName}' to {IPNetwork} in {ServerId} (dry run)",
                                                  r.Data.Name,
                                                  network,
                                                  server.Data.Id);
                        }
                        else
                        {
                            logger.LogInformation("Updating rule '{RuleName}' to {IPNetwork} in {ServerId}",
                                                  r.Data.Name,
                                                  network,
                                                  server.Data.Id);
                            var props = new MongoClusterFirewallRuleProperties(network.FirstUsable.ToString(), network.LastUsable.ToString());
                            var data = new MongoClusterFirewallRuleData { Properties = props, };
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
                    logger.LogInformation("Removing rule '{RuleName}' ({StartIPAddress} - {EndIPAddress}) in {ServerId} (dry run)",
                                          r.Data.Name,
                                          r.Data.Properties.StartIPAddress,
                                          r.Data.Properties.EndIPAddress,
                                          server.Data.Id);
                }
                else
                {
                    logger.LogInformation("Removing rule '{RuleName}' ({StartIPAddress} - {EndIPAddress}) in {ServerId}",
                                          r.Data.Name,
                                          r.Data.Properties.StartIPAddress,
                                          r.Data.Properties.EndIPAddress,
                                          server.Data.Id);
                    await r.DeleteAsync(Azure.WaitUntil.Completed, cancellationToken);
                }
            }
        }
    }
}
