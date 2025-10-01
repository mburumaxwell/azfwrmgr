using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AzureFwrMgr.Tests;

public class FirewallManagerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void GetNetworks_Works()
    {
        var entry = new IPHostEntry
        {
            HostName = "contoso.com",
            AddressList = [
                IPAddress.Parse("1.2.3.3"),
                IPAddress.Parse("1.2.3.4"),
                IPAddress.Parse("1.2.3.5").MapToIPv6(),
                IPAddress.Parse("5.2.3.5"),
                IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
            ],
            Aliases = [],
        };

        var manager = GetFirewallManager();
        var networks = manager.GetNetworks(entry);
        Assert.NotNull(networks);
        Assert.Equal([
            IPNetwork2.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334/128"),
            IPNetwork2.Parse("5.2.3.5/32"),
            IPNetwork2.Parse("1.2.3.4/31"),
            IPNetwork2.Parse("1.2.3.3/32"),
        ], networks);
    }

    [Fact]
    public async Task GetNetworksAsync_Works()
    {
        var manager = GetFirewallManager();
        var networks = await manager.GetNetworksAsync("maxwellweru.com", TestContext.Current.CancellationToken);
        Assert.NotNull(networks);
        Assert.Equal([IPNetwork2.Parse("216.198.79.1/32")], networks);

        networks = await manager.GetNetworksAsync("api64.ipify.org", TestContext.Current.CancellationToken);
        Assert.NotNull(networks);
        Assert.Equal([
            IPNetwork2.Parse("104.237.62.213/32"),
            IPNetwork2.Parse("173.231.16.77/32"),
            IPNetwork2.Parse("2607:f2d8:1:3c::3/128"),
            IPNetwork2.Parse("2607:f2d8:4010:51::5/128"),
        ], networks);
    }

    [Fact]
    public async Task GetKnownRulesAsync_Works()
    {
        var knownNetwork = new KnownFirewallRuleIp("MY_HOME", IPNetwork2.Parse("1.2.3.4/32"));
        var knownFqdn = new KnownFirewallRuleFqdn("MAX_HOME", "maxwellweru.com");

        // uses a network only
        var manager = GetFirewallManager();
        var config = new FirewallManagerConfig([], [knownNetwork], []);
        var rules = await manager.GetKnownRulesAsync(config, TestContext.Current.CancellationToken);
        var rule = Assert.Single(rules);
        Assert.Equal("MY_HOME", rule.Name);
        Assert.Equal(IPNetwork2.Parse("1.2.3.4/32"), rule.Network);

        // uses an FQDN only
        config = new FirewallManagerConfig([knownFqdn], [], []);
        rules = await manager.GetKnownRulesAsync(config, TestContext.Current.CancellationToken);
        rule = Assert.Single(rules);
        Assert.Equal("MAX_HOME", rule.Name);
        Assert.Equal(IPNetwork2.Parse("216.198.79.1/32"), rule.Network);

        // // two fqdns, two networks
        var knownNetwork2 = new KnownFirewallRuleIp("WAIYAKI_WAY", IPNetwork2.Parse("196.201.214.255/32"));
        var knownFqdn2 = new KnownFirewallRuleFqdn("YOUR_HOME", "api64.ipify.org");
        config = new FirewallManagerConfig([knownFqdn, knownFqdn2], [knownNetwork, knownNetwork2], [], "_");
        rules = await manager.GetKnownRulesAsync(config, TestContext.Current.CancellationToken);
        Assert.Equal(7, rules.Count);
        Assert.Equal("MY_HOME", rules[0].Name);
        Assert.Equal(IPNetwork2.Parse("1.2.3.4/32"), rules[0].Network);
        Assert.Equal("WAIYAKI_WAY", rules[1].Name);
        Assert.Equal(IPNetwork2.Parse("196.201.214.255/32"), rules[1].Network);
        Assert.Equal("MAX_HOME", rules[2].Name);
        Assert.Equal(IPNetwork2.Parse("216.198.79.1/32"), rules[2].Network);
        Assert.Equal("YOUR_HOME_0", rules[3].Name);
        Assert.Equal(IPNetwork2.Parse("104.237.62.213/32"), rules[3].Network);
        Assert.Equal("YOUR_HOME_1", rules[4].Name);
        Assert.Equal(IPNetwork2.Parse("173.231.16.77/32"), rules[4].Network);
        Assert.Equal("YOUR_HOME_2", rules[5].Name);
        Assert.Equal(IPNetwork2.Parse("2607:f2d8:1:3c::3/128"), rules[5].Network);
        Assert.Equal("YOUR_HOME_3", rules[6].Name);
        Assert.Equal(IPNetwork2.Parse("2607:f2d8:4010:51::5/128"), rules[6].Network);
    }

    private CustomFirewallManager GetFirewallManager()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddXUnit(outputHelper));
        services.AddTransient<FirewallManager, CustomFirewallManager>();
        var root = services.BuildServiceProvider();

        using var scope = root.CreateScope();
        var provider = scope.ServiceProvider;
        return Assert.IsType<CustomFirewallManager>(provider.GetRequiredService<FirewallManager>());
    }

    class CustomFirewallManager(ILoggerFactory loggerFactory) : FirewallManager(loggerFactory)
    {
    }
}
