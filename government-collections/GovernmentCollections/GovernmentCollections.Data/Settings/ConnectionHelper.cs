using StackExchange.Redis;

namespace GovernmentCollections.Data.Settings;

public class ConnectionHelper
{
    private static readonly Lazy<ConnectionMultiplexer> LazyConnection = new(() =>
    {
        var connectionString = Environment.GetEnvironmentVariable("RedisConnection") ?? "localhost:6379";
        return ConnectionMultiplexer.Connect(connectionString);
    });

    public static ConnectionMultiplexer Connection => LazyConnection.Value;
}