using FitBit.Api.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FitBit.Api.Data;

/// <summary>
/// Single entry point to the one shared collection. Repositories obtain their
/// typed view through <see cref="GetCollection{T}"/>; nothing else should call
/// GetCollection directly.
/// </summary>
public sealed class FitnessTrackerContext
{
    private readonly IMongoDatabase _database;

    public FitnessTrackerContext(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var settings = options.Value;
        _database = client.GetDatabase(settings.DatabaseName);
        DatabaseName = settings.DatabaseName;
        CollectionName = settings.CollectionName;
    }

    public string DatabaseName { get; }

    public string CollectionName { get; }

    public IMongoCollection<T> GetCollection<T>() => _database.GetCollection<T>(CollectionName);

    public Task<BsonDocument> PingAsync(CancellationToken ct = default) =>
        _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
}
