using FitBit.Api.Dtos.Dashboard;
using FitBit.Api.Infrastructure;
using FitBit.Api.Models;
using FitBit.Api.Repositories;

namespace FitBit.Api.Services;

public interface IDashboardService
{
    Task<DashboardResponse> GetSummaryAsync(string userId, int days, CancellationToken ct = default);
}

/// <summary>
/// Aggregates in memory rather than with a $group pipeline. Three reasons, in order:
/// the trend must be zero-filled to exactly `days` entries and $group only returns
/// days that have documents; a hand-built pipeline over a polymorphic collection
/// would reach around MongoRepositoryBase.Scoped(); and a 7-day window is ~20
/// documents, so this is microseconds of LINQ.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private static readonly int[] AllowedRanges = [7, 14, 30];

    private readonly IActivityRepository _activities;
    private readonly IGoalRepository _goals;
    private readonly IGoalService _goalService;

    public DashboardService(IActivityRepository activities, IGoalRepository goals, IGoalService goalService)
    {
        _activities = activities;
        _goals = goals;
        _goalService = goalService;
    }

    public async Task<DashboardResponse> GetSummaryAsync(string userId, int days, CancellationToken ct = default)
    {
        if (!AllowedRanges.Contains(days))
            throw ApiException.BadRequest($"days must be one of: {string.Join(", ", AllowedRanges)}.");

        var today = DateOnly.FromDateTime(DateTime.Now);
        var from = today.AddDays(-(days - 1));
        var previousFrom = from.AddDays(-days);
        var previousTo = from.AddDays(-1);

        var goalDocs = await _goals.GetForUserAsync(userId, activeOnly: true, ct);

        // One query wide enough to cover the trend window, the comparison window,
        // and every goal window, so the whole dashboard costs two round-trips.
        var earliest = previousFrom;
        var latest = today;
        foreach (var goal in goalDocs)
        {
            var (start, end) = _goalService.ResolveWindow(goal, today);
            if (start < earliest) earliest = start;
            if (end > latest) latest = end;
        }

        var activities = await _activities.GetInRangeAsync(
            userId, DateKey.From(earliest), DateKey.From(latest), ct);

        var byDay = activities.ToLookup(a => a.DateKey);

        var trend = DateKey.Range(from, today).Select(day =>
        {
            var key = DateKey.From(day);
            var forDay = byDay[key];      // an empty ILookup group, never null
            return new TrendPoint(
                key,
                DateKey.Weekday(day),
                forDay.Count(),
                forDay.Sum(a => a.DurationMinutes),
                Math.Round(forDay.Sum(a => a.DistanceKm ?? 0), 1),
                forDay.Sum(a => a.Calories ?? 0));
        }).ToList();

        var totals = Totals(Between(activities, from, today));
        var previous = Totals(Between(activities, previousFrom, previousTo));

        return new DashboardResponse(
            RangeDays: days,
            From: DateKey.From(from),
            To: DateKey.From(today),
            Totals: totals,
            Previous: previous,
            CurrentStreakDays: Streak(byDay, today),
            Trend: trend,
            Goals: _goalService.Project(goalDocs, activities, today));
    }

    private static IReadOnlyCollection<ActivityDocument> Between(
        IEnumerable<ActivityDocument> activities, DateOnly from, DateOnly to)
    {
        var fromKey = DateKey.From(from);
        var toKey = DateKey.From(to);
        return activities
            .Where(a => string.CompareOrdinal(a.DateKey, fromKey) >= 0
                     && string.CompareOrdinal(a.DateKey, toKey) <= 0)
            .ToList();
    }

    private static TotalsDto Totals(IReadOnlyCollection<ActivityDocument> activities) =>
        new(activities.Count,
            activities.Sum(a => a.DurationMinutes),
            Math.Round(activities.Sum(a => a.DistanceKm ?? 0), 1),
            activities.Sum(a => a.Calories ?? 0));

    /// <summary>
    /// Consecutive days ending today that have at least one activity. Today not
    /// being logged yet doesn't break the streak — we start counting from
    /// yesterday in that case, so the number doesn't drop to 0 every morning.
    /// </summary>
    private static int Streak(ILookup<string, ActivityDocument> byDay, DateOnly today)
    {
        var cursor = byDay[DateKey.From(today)].Any() ? today : today.AddDays(-1);
        var streak = 0;

        while (byDay[DateKey.From(cursor)].Any())
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}
