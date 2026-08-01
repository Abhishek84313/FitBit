using FitBit.Api.Data;
using FitBit.Api.Models;
using MongoDB.Driver;

namespace FitBit.Api.Repositories;

public sealed record ActivityFilter(
    string UserId,
    string? Type = null,
    string? FromDateKey = null,
    string? ToDateKey = null);

public interface IActivityRepository
{
    Task<(List<ActivityDocument> Items, long Total)> QueryAsync(
        ActivityFilter filter, string sort, int page, int pageSize, CancellationToken ct = default);

    Task<List<ActivityDocument>> GetInRangeAsync(
        string userId, string fromDateKey, string toDateKey, CancellationToken ct = default);

    Task<ActivityDocument?> GetByIdAsync(string id, string userId, CancellationToken ct = default);
    Task<ActivityDocument> CreateAsync(ActivityDocument activity, CancellationToken ct = default);
    Task<ActivityDocument?> ReplaceAsync(ActivityDocument activity, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, string userId, CancellationToken ct = default);
    Task<long> DeleteAllForUserAsync(string userId, CancellationToken ct = default);
    Task CreateManyAsync(IEnumerable<ActivityDocument> activities, CancellationToken ct = default);
}

public sealed class ActivityRepository : MongoRepositoryBase<ActivityDocument>, IActivityRepository
{
    private static readonly FilterDefinitionBuilder<ActivityDocument> F = Builders<ActivityDocument>.Filter;

    public ActivityRepository(FitnessTrackerContext context) : base(context) { }

    protected override string DocType => DocTypes.Activity;

    public async Task<(List<ActivityDocument> Items, long Total)> QueryAsync(
        ActivityFilter filter, string sort, int page, int pageSize, CancellationToken ct = default)
    {
        var mongoFilter = Build(filter);
        var total = await CountAsync(mongoFilter, ct);
        var items = await FindAsync(mongoFilter, SortFor(sort), (page - 1) * pageSize, pageSize, ct);
        return (items, total);
    }

    public Task<List<ActivityDocument>> GetInRangeAsync(
        string userId, string fromDateKey, string toDateKey, CancellationToken ct = default) =>
        FindAsync(Build(new ActivityFilter(userId, FromDateKey: fromDateKey, ToDateKey: toDateKey)),
                  Builders<ActivityDocument>.Sort.Descending(a => a.DateUtc), ct: ct);

    // Ownership lives in the query, not in a post-fetch check: fetch-then-compare
    // is an IDOR waiting to be refactored away. The service turns a null into 404
    // rather than 403 — a 403 would confirm the id exists.
    public Task<ActivityDocument?> GetByIdAsync(string id, string userId, CancellationToken ct = default) =>
        FindOneAsync(F.And(F.Eq(a => a.Id, id), F.Eq(a => a.UserId, userId)), ct);

    public async Task<ActivityDocument> CreateAsync(ActivityDocument activity, CancellationToken ct = default)
    {
        activity.DocType = DocTypes.Activity;
        activity.CreatedAtUtc = activity.UpdatedAtUtc = DateTime.UtcNow;
        await InsertAsync(activity, ct);
        return activity;
    }

    public Task CreateManyAsync(IEnumerable<ActivityDocument> activities, CancellationToken ct = default)
    {
        var list = activities.ToList();
        var now = DateTime.UtcNow;
        foreach (var a in list)
        {
            a.DocType = DocTypes.Activity;
            a.CreatedAtUtc = a.UpdatedAtUtc = now;
        }
        return list.Count == 0 ? Task.CompletedTask : InsertManyAsync(list, ct);
    }

    public Task<ActivityDocument?> ReplaceAsync(ActivityDocument activity, CancellationToken ct = default)
    {
        activity.DocType = DocTypes.Activity;
        activity.UpdatedAtUtc = DateTime.UtcNow;
        return FindOneAndReplaceAsync(
            F.And(F.Eq(a => a.Id, activity.Id), F.Eq(a => a.UserId, activity.UserId)), activity, ct);
    }

    public async Task<bool> DeleteAsync(string id, string userId, CancellationToken ct = default)
    {
        var result = await DeleteOneAsync(F.And(F.Eq(a => a.Id, id), F.Eq(a => a.UserId, userId)), ct);
        return result.DeletedCount > 0;
    }

    public async Task<long> DeleteAllForUserAsync(string userId, CancellationToken ct = default)
    {
        var result = await DeleteManyAsync(F.Eq(a => a.UserId, userId), ct);
        return result.DeletedCount;
    }

    private static FilterDefinition<ActivityDocument> Build(ActivityFilter filter)
    {
        var clauses = new List<FilterDefinition<ActivityDocument>> { F.Eq(a => a.UserId, filter.UserId) };

        if (!string.IsNullOrWhiteSpace(filter.Type))
            clauses.Add(F.Eq(a => a.Type, filter.Type));

        // dateKey is a "yyyy-MM-dd" string, so it is lexicographically ordered and
        // timezone-immune — string range comparison is exactly right here.
        if (!string.IsNullOrWhiteSpace(filter.FromDateKey))
            clauses.Add(F.Gte(a => a.DateKey, filter.FromDateKey));

        if (!string.IsNullOrWhiteSpace(filter.ToDateKey))
            clauses.Add(F.Lte(a => a.DateKey, filter.ToDateKey));

        return F.And(clauses);
    }

    /// <summary>Whitelist — never pass a client string into a Mongo sort spec.</summary>
    private static SortDefinition<ActivityDocument> SortFor(string sort) => sort switch
    {
        "date_asc" => Builders<ActivityDocument>.Sort.Ascending(a => a.DateUtc),
        "duration_desc" => Builders<ActivityDocument>.Sort.Descending(a => a.DurationMinutes),
        "duration_asc" => Builders<ActivityDocument>.Sort.Ascending(a => a.DurationMinutes),
        _ => Builders<ActivityDocument>.Sort.Descending(a => a.DateUtc),
    };

    public static readonly string[] AllowedSorts =
        ["date_desc", "date_asc", "duration_desc", "duration_asc"];
}
