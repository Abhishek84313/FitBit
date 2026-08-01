using FitBit.Api.Data;
using FitBit.Api.Models;
using MongoDB.Driver;

namespace FitBit.Api.Repositories;

public interface IGoalRepository
{
    Task<List<GoalDocument>> GetForUserAsync(string userId, bool activeOnly, CancellationToken ct = default);
    Task<GoalDocument?> GetByIdAsync(string id, string userId, CancellationToken ct = default);
    Task<GoalDocument> CreateAsync(GoalDocument goal, CancellationToken ct = default);
    Task CreateManyAsync(IEnumerable<GoalDocument> goals, CancellationToken ct = default);
    Task<GoalDocument?> ReplaceAsync(GoalDocument goal, CancellationToken ct = default);
    Task<bool> DeleteAsync(string id, string userId, CancellationToken ct = default);
    Task<long> DeleteAllForUserAsync(string userId, CancellationToken ct = default);
}

public sealed class GoalRepository : MongoRepositoryBase<GoalDocument>, IGoalRepository
{
    private static readonly FilterDefinitionBuilder<GoalDocument> F = Builders<GoalDocument>.Filter;

    public GoalRepository(FitnessTrackerContext context) : base(context) { }

    protected override string DocType => DocTypes.Goal;

    public Task<List<GoalDocument>> GetForUserAsync(string userId, bool activeOnly, CancellationToken ct = default)
    {
        var filter = F.Eq(g => g.UserId, userId);
        if (activeOnly) filter = F.And(filter, F.Eq(g => g.IsActive, true));
        return FindAsync(filter, Builders<GoalDocument>.Sort.Descending(g => g.CreatedAtUtc), ct: ct);
    }

    public Task<GoalDocument?> GetByIdAsync(string id, string userId, CancellationToken ct = default) =>
        FindOneAsync(F.And(F.Eq(g => g.Id, id), F.Eq(g => g.UserId, userId)), ct);

    public async Task<GoalDocument> CreateAsync(GoalDocument goal, CancellationToken ct = default)
    {
        goal.DocType = DocTypes.Goal;
        goal.CreatedAtUtc = goal.UpdatedAtUtc = DateTime.UtcNow;
        await InsertAsync(goal, ct);
        return goal;
    }

    public Task CreateManyAsync(IEnumerable<GoalDocument> goals, CancellationToken ct = default)
    {
        var list = goals.ToList();
        var now = DateTime.UtcNow;
        foreach (var g in list)
        {
            g.DocType = DocTypes.Goal;
            g.CreatedAtUtc = g.UpdatedAtUtc = now;
        }
        return list.Count == 0 ? Task.CompletedTask : InsertManyAsync(list, ct);
    }

    public Task<GoalDocument?> ReplaceAsync(GoalDocument goal, CancellationToken ct = default)
    {
        goal.DocType = DocTypes.Goal;
        goal.UpdatedAtUtc = DateTime.UtcNow;
        return FindOneAndReplaceAsync(
            F.And(F.Eq(g => g.Id, goal.Id), F.Eq(g => g.UserId, goal.UserId)), goal, ct);
    }

    public async Task<bool> DeleteAsync(string id, string userId, CancellationToken ct = default)
    {
        var result = await DeleteOneAsync(F.And(F.Eq(g => g.Id, id), F.Eq(g => g.UserId, userId)), ct);
        return result.DeletedCount > 0;
    }

    public async Task<long> DeleteAllForUserAsync(string userId, CancellationToken ct = default)
    {
        var result = await DeleteManyAsync(F.Eq(g => g.UserId, userId), ct);
        return result.DeletedCount;
    }
}
