using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using SC = AzureFwrMgr.AzureFwrMgrSerializerContext;

namespace AzureFwrMgr.Tests;

public class FirewallManagerConfigTests
{
    [Fact]
    public void Deserialize_Works()
    {
        var json = new JsonObject
        {
            ["fqdns"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "MAXHOME",
                    ["fqdn"] = "office.maxwellweru.io",
                },
            },
            ["networks"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "OTHERHOME",
                    ["network"] = "76.76.21.21/32",
                },
            },
            ["subscriptions"] = new JsonArray { "personal" },
            ["separator"] = "_",
            ["cosmosForPostgreSql"] = false,
            ["mongoCluster"] = false,
            ["postgres"] = false,
            ["sql"] = false,
        };
        var config = JsonSerializer.Deserialize(json, SC.Default.FirewallManagerConfig);
        Assert.NotNull(config);
        Assert.NotNull(config.KnownFqdns);
        var knownFqdn = Assert.Single(config.KnownFqdns);
        Assert.Equal("MAXHOME", knownFqdn.Name);
        Assert.Equal("office.maxwellweru.io", knownFqdn.Fqdn);
        Assert.NotNull(config.KnownNetworks);
        var knownNetwork = Assert.Single(config.KnownNetworks);
        Assert.Equal("OTHERHOME", knownNetwork.Name);
        Assert.Equal(IPNetwork2.Parse("76.76.21.21/32"), knownNetwork.Network);
        Assert.NotNull(config.Subscriptions);
        Assert.Equal("personal", Assert.Single(config.Subscriptions));
        Assert.Equal("_", config.Separator);
        Assert.False(config.CosmosForPostgreSql);
        Assert.False(config.MongoCluster);
        Assert.False(config.PostgreSql);
        Assert.False(config.Sql);
    }

    [Fact]
    public void Deserialize_Works_WithDefaults()
    {
        var json = new JsonObject
        {
            ["fqdns"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "MAXHOME",
                    ["fqdn"] = "office.maxwellweru.io",
                },
            },
            ["networks"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "OTHERHOME",
                    ["network"] = "76.76.21.21/32",
                },
            },
        };
        var config = JsonSerializer.Deserialize(json, SC.Default.FirewallManagerConfig);
        Assert.NotNull(config);
        Assert.NotNull(config);
        Assert.NotNull(config.KnownFqdns);
        var knownFqdn = Assert.Single(config.KnownFqdns);
        Assert.Equal("MAXHOME", knownFqdn.Name);
        Assert.Equal("office.maxwellweru.io", knownFqdn.Fqdn);
        Assert.NotNull(config.KnownNetworks);
        var knownNetwork = Assert.Single(config.KnownNetworks);
        Assert.Equal("OTHERHOME", knownNetwork.Name);
        Assert.Equal(IPNetwork2.Parse("76.76.21.21/32"), knownNetwork.Network);
        Assert.Null(config.Subscriptions);
        Assert.Equal("-", config.Separator);
        Assert.True(config.CosmosForPostgreSql);
        Assert.True(config.MongoCluster);
        Assert.True(config.PostgreSql);
        Assert.True(config.Sql);
    }
}
