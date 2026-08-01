using FitBit.Api.Models;
using FitBit.Api.Repositories;

namespace FitBit.Api.Services;

public interface ISeedService
{
    Task<(int Activities, int Goals)> SeedAsync(string userId, CancellationToken ct = default);
    Task<(long Activities, long Goals)> ResetAsync(string userId, CancellationToken ct = default);
}

/// <summary>Development-only convenience for getting a populated dashboard fast.</summary>
public sealed class SeedService : ISeedService
{
    private readonly IActivityRepository _activities;
    private readonly IGoalRepository _goals;

    public SeedService(IActivityRepository activities, IGoalRepository goals)
    {
        _activities = activities;
        _goals = goals;
    }

    public async Task<(int Activities, int Goals)> SeedAsync(string userId, CancellationToken ct = default)
    {
        var random = new Random(20260801);
        var today = DateOnly.FromDateTime(DateTime.Now);
        var activities = new List<ActivityDocument>();

        // Two deliberately empty days so the chart's zero-fill is visible rather
        // than merely correct.
        var emptyOffsets = new[] { 3, 8 };

        for (var offset = 13; offset >= 0; offset--)
        {
            if (emptyOffsets.Contains(offset)) continue;

            var day = today.AddDays(-offset);
            var perDay = random.Next(1, 3);

            for (var i = 0; i < perDay; i++)
            {
                var type = ActivityTypes.All[random.Next(ActivityTypes.All.Length)];
                var duration = random.Next(20, 91);
                var distance = type is "Running" or "Walking" or "Cycling" or "Swimming"
                    ? Math.Round(duration * (random.NextDouble() * 0.12 + 0.08), 1)
                    : (double?)null;

                activities.Add(new ActivityDocument
                {
                    UserId = userId,
                    Type = type,
                    DateKey = DateKey.From(day),
                    DateUtc = DateKey.ToUtcInstant(day),
                    DurationMinutes = duration,
                    DistanceKm = distance,
                    Calories = duration * random.Next(7, 13),
                    Notes = i == 0 && random.Next(3) == 0 ? "Felt good." : null,
                });
            }
        }

        await _activities.CreateManyAsync(activities, ct);

        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var goals = new List<GoalDocument>
        {
            new()
            {
                UserId = userId, Title = "Move 45 minutes a day", Metric = GoalMetrics.DurationMinutes,
                TargetValue = 45, Period = GoalPeriods.Daily, StartDate = DateKey.From(today),
            },
            new()
            {
                UserId = userId, Title = "Run 25 km this week", Metric = GoalMetrics.DistanceKm,
                TargetValue = 25, Period = GoalPeriods.Weekly, StartDate = DateKey.From(today.AddDays(-6)),
            },
            new()
            {
                UserId = userId, Title = "20 sessions this month", Metric = GoalMetrics.Sessions,
                TargetValue = 20, Period = GoalPeriods.Monthly, StartDate = DateKey.From(monthStart),
            },
        };

        await _goals.CreateManyAsync(goals, ct);

        return (activities.Count, goals.Count);
    }

    /// <summary>Scoped to one user — never a DeleteMany(Empty).</summary>
    public async Task<(long Activities, long Goals)> ResetAsync(string userId, CancellationToken ct = default)
    {
        var activities = await _activities.DeleteAllForUserAsync(userId, ct);
        var goals = await _goals.DeleteAllForUserAsync(userId, ct);
        return (activities, goals);
    }
}
