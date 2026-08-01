using FitBit.Api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FitBit.Api.Data;

/// <summary>Creates the collection's indexes at startup. Idempotent.</summary>
public sealed class MongoIndexInitializer : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(IServiceProvider services, ILogger<MongoIndexInitializer> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FitnessTrackerContext>();
        var collection = context.GetCollection<BsonDocument>();

        var models = new List<CreateIndexModel<BsonDocument>>
        {
            // The partial filter is mandatory, not an optimisation. A plain
            // unique { email: 1 } treats every activity and goal — which have no
            // email field — as email: null, so the SECOND activity insert fails
            // with E11000 and looks like a bug in the activity code.
            new(new BsonDocument("email", 1),
                new CreateIndexOptions<BsonDocument>
                {
                    Name = "uniq_user_email",
                    Unique = true,
                    PartialFilterExpression = new BsonDocument("docType", DocTypes.User),
                }),

            new(new BsonDocument { { "docType", 1 }, { "userId", 1 }, { "date", -1 } },
                new CreateIndexOptions { Name = "activity_user_date" }),

            new(new BsonDocument { { "docType", 1 }, { "userId", 1 }, { "dateKey", 1 } },
                new CreateIndexOptions { Name = "activity_user_datekey" }),

            new(new BsonDocument { { "docType", 1 }, { "userId", 1 }, { "isActive", 1 } },
                new CreateIndexOptions { Name = "goal_user_active" }),
        };

        try
        {
            await collection.Indexes.CreateManyAsync(models, cancellationToken);
            _logger.LogInformation(
                "Mongo indexes ensured on {Database}.{Collection}", context.DatabaseName, context.CollectionName);
        }
        catch (Exception ex)
        {
            // Don't take the API down if Mongo is briefly unreachable at boot —
            // /api/dev/health is the endpoint that reports the real state.
            _logger.LogError(ex, "Failed to create Mongo indexes. Is mongod running on 127.0.0.1:27017?");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
