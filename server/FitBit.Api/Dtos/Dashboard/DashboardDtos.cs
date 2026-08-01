using FitBit.Api.Dtos.Goals;

namespace FitBit.Api.Dtos.Dashboard;

public sealed record TotalsDto(int Activities, int DurationMinutes, double DistanceKm, int Calories);

public sealed record TrendPoint(
    string DateKey,
    string Weekday,
    int Activities,
    int DurationMinutes,
    double DistanceKm,
    int Calories);

public sealed record DashboardResponse(
    int RangeDays,
    string From,
    string To,
    TotalsDto Totals,
    // Previous = the immediately preceding window of the same length, so the stat
    // tiles can show a delta. A stat tile without a comparison is just a number.
    TotalsDto Previous,
    int CurrentStreakDays,
    IReadOnlyList<TrendPoint> Trend,
    IReadOnlyList<GoalResponse> Goals);
