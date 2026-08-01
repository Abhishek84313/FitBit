using System.ComponentModel.DataAnnotations;

namespace FitBit.Api.Dtos.Activities;

public class ActivityCreateRequest
{
    [Required, MaxLength(40)]
    public string Type { get; set; } = default!;

    /// <summary>The user's local calendar day, "yyyy-MM-dd".</summary>
    [Required]
    public string Date { get; set; } = default!;

    [Range(1, 1440)]
    public int DurationMinutes { get; set; }

    [Range(0, 1000)]
    public double? DistanceKm { get; set; }

    [Range(0, 20000)]
    public int? Calories { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>Full replace rather than PATCH — simpler, and the form posts every field anyway.</summary>
public sealed class ActivityUpdateRequest : ActivityCreateRequest;

public sealed record ActivityResponse(
    string Id,
    string Type,
    string Date,
    string DateKey,
    int DurationMinutes,
    double? DistanceKm,
    int? Calories,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
