using System.ComponentModel.DataAnnotations;

namespace FitBit.Api.Dtos.Goals;

public class GoalCreateRequest
{
    [Required, MaxLength(80)]
    public string Title { get; set; } = default!;

    /// <summary>durationMinutes | distanceKm | calories | sessions</summary>
    [Required]
    public string Metric { get; set; } = default!;

    [Range(0.01, 1_000_000)]
    public double TargetValue { get; set; }

    /// <summary>daily | weekly | monthly | custom (custom requires EndDate)</summary>
    [Required]
    public string Period { get; set; } = default!;

    /// <summary>"yyyy-MM-dd"</summary>
    [Required]
    public string StartDate { get; set; } = default!;

    public string? EndDate { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class GoalUpdateRequest : GoalCreateRequest;

/// <summary>
/// CurrentValue / ProgressPercent / Status are computed on read, never stored.
/// Storing them would need invalidation on every activity write and guarantees
/// the dashboard and the goals page eventually disagree.
/// </summary>
public sealed record GoalResponse(
    string Id,
    string Title,
    string Metric,
    double TargetValue,
    string Period,
    string StartDate,
    string? EndDate,
    string WindowStart,
    string WindowEnd,
    double CurrentValue,
    double ProgressPercent,
    string Status,
    bool IsActive);
