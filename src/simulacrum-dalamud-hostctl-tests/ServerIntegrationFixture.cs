using System.Text.RegularExpressions;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.Runtime.Endpoints;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Simulacrum.Hostctl.Tests;

public partial class ServerIntegrationFixture : IAsyncDisposable
{
    private readonly INetwork _network;

    private readonly IContainer _api;

    private readonly IContainer _db;

    public string Hostname => _api.Hostname;

    public int Port => _api.GetMappedPublicPort(3000);

    public ServerIntegrationFixture()
    {
        // Build the infra stack
        (_network, _db, _api) = BuildStack().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await _db.StopAsync();
        await _api.StopAsync();
        await _network.DisposeAsync();
        GC.SuppressFinalize(this);
    }


    [GeneratedRegex("Nest application successfully started", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex HostctlReady();

    [GeneratedRegex("Initializing DynamoDB Local with the following configuration:", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex DdbLocalReady();

    private static async Task<(INetwork, IContainer, IContainer)> BuildStack(CancellationToken ct = default)
    {
        // Create a Docker bridge network
        var network = new NetworkBuilder()
            .Build();
        await network.CreateAsync(ct);

        // Create the DynamoDB Local container, with a network alias for the API to call it
        var dbNetworkAlias = Guid.NewGuid().ToString("D");
        var db = new ContainerBuilder()
            .WithImage("amazon/dynamodb-local:latest")
            .WithPortBinding(8000, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged(DdbLocalReady())
                .UntilPortIsAvailable(8000))
            .WithNetwork(network)
            .WithNetworkAliases(dbNetworkAlias)
            .Build();
        await db.StartAsync(ct);

        // DDB Local uses the access key ID and region to form database partition names, so that needs to be
        // the same between here and the API
        var credentials = new SessionAWSCredentials("AccessKeyId", "SecretKey", "SessionToken");
        var ddb = new AmazonDynamoDBClient(
            credentials,
            new AmazonDynamoDBConfig
            {
                EndpointProvider = new StaticEndpointProvider($"http://{db.Hostname}:{db.GetMappedPublicPort(8000)}"),
                RegionEndpoint = RegionEndpoint.USEast1,
            });

        // Create the test table
        var req = new CreateTableRequest("Simulacrum",
            [
                new KeySchemaElement("PK", KeyType.HASH),
                new KeySchemaElement("SK", KeyType.RANGE),
            ],
            [
                new AttributeDefinition("PK", ScalarAttributeType.S),
                new AttributeDefinition("SK", ScalarAttributeType.S),
                new AttributeDefinition("LSI1SK", ScalarAttributeType.S),
                new AttributeDefinition("GSI1PK", ScalarAttributeType.S),
                new AttributeDefinition("GSI1SK", ScalarAttributeType.S),
            ],
            new ProvisionedThroughput(10, 5))
        {
            LocalSecondaryIndexes =
            [
                new LocalSecondaryIndex
                {
                    IndexName = "LSI1",
                    KeySchema =
                    [
                        new KeySchemaElement("PK", KeyType.HASH),
                        new KeySchemaElement("LSI1SK", KeyType.RANGE),
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                },
            ],
            GlobalSecondaryIndexes =
            [
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI1",
                    KeySchema =
                    [
                        new KeySchemaElement("GSI1PK", KeyType.HASH),
                        new KeySchemaElement("GSI1SK", KeyType.RANGE),
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                    ProvisionedThroughput = new ProvisionedThroughput(10, 5),
                },
            ],
        };

        await ddb.CreateTableAsync(req, ct);

        // Create the API container, using the resources we initialized earlier
        var api = new ContainerBuilder()
            .WithImage("simulacrum-cloud-api:latest")
            .WithPortBinding(3000, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged(HostctlReady()))
            .WithNetwork(network)
            .WithEnvironment(new Dictionary<string, string>
            {
                { "SIMULACRUM_DDB_ENDPOINT", $"http://{dbNetworkAlias}:8000" },
                { "AWS_REGION", "us-east-1" },
                { "AWS_ACCESS_KEY_ID", credentials.GetCredentials().AccessKey },
                { "AWS_SECRET_ACCESS_KEY", credentials.GetCredentials().SecretKey },
                { "AWS_SESSION_TOKEN", credentials.GetCredentials().Token },
            })
            .Build();
        await api.StartAsync(ct);

        return (network, db, api);
    }
}