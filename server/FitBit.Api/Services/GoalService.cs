using FitBit.Api.Dtos.Goals;
using FitBit.Api.Infrastructure;
using FitBit.Api.Models;
using FitBit.Api.Repositories;

namespace FitBit.Api.Services;

public interface IGoalService
{
    Task<IReadOnlyList<GoalResponse>> ListAsync(string userId, bool activeOnly, CancellationToken ct = default);
    Task<GoalResponse> GetAsync(string id, string userId, CancellationToken ct = default);
    Task<GoalResponse> CreateAsync(string userId, GoalCreateRequest request, CancellationToken ct = default);
    Task<GoalResponse> UpdateAsync(string id, string userId, GoalUpdateRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, string userId, CancellationToken ct = default);

    /// <summary>Shared with the dashboard so both views compute progress identically.</summary>
    IReadOnlyList<GoalResponse> Project(
        IEnumerable<GoalDocument> goals, IReadOnlyCollection<ActivityDocument> activities, DateOnly today);

    (DateOnly Start, DateOnly End) ResolveWindow(GoalDocument goal, DateOnly today);
}

public sealed class GoalService : IGoalService
{
    private readonly IGoalRepository _goals;
    private readonly IActivityRepository _activities;

    public GoalService(IGoalRepository goals, IActivityRepository activities)
    {
        _goals = goals;
        _activities = activities;
    }

    public async Task<IReadOnlyList<GoalResponse>> ListAsync(string userId, bool activeOnly, CancellationToken ct = default)
    {
        var goals = await _goals.GetForUserAsync(userId, activeOnly, ct);
        return await ProjectWithActivitiesAsync(userId, goals, ct);
    }

    public async Task<GoalResponse> GetAsync(string id, string userId, CancellationToken ct = default)
    {
        var goal = await _goals.GetByIdAsync(id, userId, ct) ?? throw ApiException.NotFound("Goal not found.");
        return (await ProjectWithActivitiesAsync(userId, [goal], ct))[0];
    }

    public async Task<GoalResponse> CreateAsync(string userId, GoalCreateRequest request, CancellationToken ct = default)
    {
        var goal = new GoalDocument { UserId = userId };
        Apply(goal, request);
        await _goals.CreateAsync(goal, ct);
        return (await ProjectWithActivitiesAsync(userId, [goal], ct))[0];
    }

    public async Task<GoalResponse> UpdateAsync(
        string id, string userId, GoalUpdateRequest request, CancellationToken ct = default)
    {
        var goal = await _goals.GetByIdAsync(id, userId, ct) ?? throw ApiException.NotFound("Goal not found.");
        Apply(goal, request);

        var updated = await _goals.ReplaceAsync(goal, ct) ?? throw ApiException.NotFound("Goal not found.");
        return (await ProjectWithActivitiesAsync(userId, [updated], ct))[0];
    }

    public async Task DeleteAsync(string id, string userId, CancellationToken ct = default)
    {
        if (!await _goals.DeleteAsync(id, userId, ct))
            throw ApiException.NotFound("Goal not found.");
    }

    /// <summary>
    /// Fetches the union of every goal's window in one query, then filters per goal
    /// in memory — one Mongo round-trip regardless of how many goals there are.
    /// </summary>
    private async Task<IReadOnlyList<GoalResponse>> ProjectWithActivitiesAsync(
        string userId, IReadOnlyList<GoalDocument> goals, CancellationToken ct)
    {
        if (goals.Count == 0) return [];

        var today = DateOnly.FromDateTime(DateTime.Now);
        var windows = goals.Select(g => ResolveWindow(g, today)).ToList();
        var earliest = windows.Min(w => w.Start);
        var latest = windows.Max(w => w.End);

        var activities = await _activities.GetInRangeAsync(
            userId, DateKey.From(earliest), DateKey.From(latest), ct);

        return Project(goals, activities, today);
    }

    public IReadOnlyList<GoalResponse> Project(
        IEnumerable<GoalDocument> goals, IReadOnlyCollection<ActivityDocument> activities, DateOnly today)
    {
        var result = new List<GoalResponse>();

        foreach (var goal in goals)
        {
            var (start, end) = ResolveWindow(goal, today);
            var startKey = DateKey.From(start);
            var endKey = DateKey.From(end);

            // String comparison on "yyyy-MM-dd" — ordinal ordering matches calendar
            // ordering, and no timezone can shift it.
            var inWindow = activities
                .Where(a => string.CompareOrdinal(a.DateKey, startKey) >= 0
                         && string.CompareOrdinal(a.DateKey, endKey) <= 0)
                .ToList();

            var current = goal.Metric switch
            {
                GoalMetrics.Sessions => inWindow.Count,
                GoalMetrics.DurationMinutes => inWindow.Sum(a => a.DurationMinutes),
                GoalMetrics.DistanceKm => Math.Round(inWindow.Sum(a => a.DistanceKm ?? 0), 2),
                GoalMetrics.Calories => inWindow.Sum(a => a.Calories ?? 0),
                _ => 0d,
            };

            // Guard the divide: a zero target yields Infinity, which serialises to
            // NaN in JSON and renders as "NaN%".
            var percent = goal.TargetValue <= 0
                ? 0d
                : Math.Round(Math.Min(100d, current / goal.TargetValue * 100d), 1);

            result.Add(new GoalResponse(
                goal.Id, goal.Title, goal.Metric, goal.TargetValue, goal.Period,
                goal.StartDate, goal.EndDate, startKey, endKey,
                current,                     // the TRUE value, not clamped — so the UI
                percent,                     // can say "32 / 30 km — achieved"
                Status(goal, current, percent, start, end, today),
                goal.IsActive));
        }

        return result;
    }

    private static string Status(
        GoalDocument goal, double current, double percent, DateOnly start, DateOnly end, DateOnly today)
    {
        if (goal.TargetValue > 0 && current >= goal.TargetValue) return GoalStatuses.Achieved;
        if (today > end) return GoalStatuses.Missed;

        var totalDays = end.DayNumber - start.DayNumber + 1;
        var elapsedDays = Math.Clamp(today.DayNumber - start.DayNumber + 1, 0, totalDays);
        var expectedPercent = totalDays <= 0 ? 0d : elapsedDays / (double)totalDays * 100d;

        return percent + 20d < expectedPercent ? GoalStatuses.AtRisk : GoalStatuses.OnTrack;
    }

    public (DateOnly Start, DateOnly End) ResolveWindow(GoalDocument goal, DateOnly today)
    {
        var declaredStart = DateKey.TryParse(goal.StartDate, out var s) ? s : today;

        return goal.Period switch
        {
            // Recurring periods track the *current* period, so a daily goal resets
            // each day rather than accumulating since it was created.
            GoalPeriods.Daily => (today, today),
            GoalPeriods.Weekly => WeekOf(today),
            GoalPeriods.Monthly => (new DateOnly(today.Year, today.Month, 1),
                                    new DateOnly(today.Year, today.Month,
                                                 DateTime.DaysInMonth(today.Year, today.Month))),
            _ => (declaredStart,
                  DateKey.TryParse(goal.EndDate, out var e) ? e : today),
        };
    }

    /// <summary>Monday-start week containing <paramref name="day"/>.</summary>
    private static (DateOnly Start, DateOnly End) WeekOf(DateOnly day)
    {
        var offset = ((int)day.DayOfWeek + 6) % 7;   // Monday = 0
        var start = day.AddDays(-offset);
        return (start, start.AddDays(6));
    }

    private static void Apply(GoalDocument goal, GoalCreateRequest request)
    {
        if (!GoalMetrics.IsValid(request.Metric))
            throw ApiException.BadRequest($"Metric must be one of: {string.Join(", ", GoalMetrics.All)}.");

        if (!GoalPeriods.IsValid(request.Period))
            throw ApiException.BadRequest($"Period must be one of: {string.Join(", ", GoalPeriods.All)}.");

        if (!DateKey.TryParse(request.StartDate, out var start))
            throw ApiException.BadRequest("StartDate must be in yyyy-MM-dd format.");

        DateOnly? end = null;
        if (!string.IsNullOrWhiteSpace(request.EndDate))
        {
            if (!DateKey.TryParse(request.EndDate, out var parsed))
                throw ApiException.BadRequest("EndDate must be in yyyy-MM-dd format.");
            end = parsed;
        }

        if (request.Period == GoalPeriods.Custom && end is null)
            throw ApiException.BadRequest("A custom-period goal requires an end date.");

        if (end is not null && end < start)
            throw ApiException.BadRequest("EndDate cannot be before StartDate.");

        if (request.TargetValue <= 0)
            throw ApiException.BadRequest("TargetValue must be greater than zero.");

        goal.Title = request.Title.Trim();
        goal.Metric = request.Metric;
        goal.TargetValue = request.TargetValue;
        goal.Period = request.Period;
        goal.StartDate = DateKey.From(start);
        goal.EndDate = end is null ? null : DateKey.From(end.Value);
        goal.IsActive = request.IsActive;
    }
}
