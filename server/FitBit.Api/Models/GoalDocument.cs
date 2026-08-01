using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FitBit.Api.Models;

[BsonIgnoreExtraElements]
public sealed class GoalDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("docType")]
    public string DocType { get; set; } = DocTypes.Goal;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = default!;

    [BsonElement("title")]
    public string Title { get; set; } = default!;

    /// <summary>One of <see cref="GoalMetrics"/>.</summary>
    [BsonElement("metric")]
    public string Metric { get; set; } = default!;

    [BsonElement("targetValue")]
    public double TargetValue { get; set; }

    /// <summary>One of <see cref="GoalPeriods"/>.</summary>
    [BsonElement("period")]
    public string Period { get; set; } = default!;

    /// <summary>"yyyy-MM-dd" — windows are compared as strings, like ActivityDocument.DateKey.</summary>
    [BsonElement("startDate")]
    public string StartDate { get; set; } = default!;

    [BsonElement("endDate")]
    public string? EndDate { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAtUtc { get; set; }
}

public static class GoalMetrics
{
    public const string DurationMinutes = "durationMinutes";
    public const string DistanceKm = "distanceKm";
    public const string Calories = "calories";
    public const string Sessions = "sessions";

    public static readonly string[] All = [DurationMinutes, DistanceKm, Calories, Sessions];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}

public static class GoalPeriods
{
    public const string Daily = "daily";
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";
    public const string Custom = "custom";

    public static readonly string[] All = [Daily, Weekly, Monthly, Custom];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}

public static class GoalStatuses
{
    public const string OnTrack = "onTrack";
    public const string AtRisk = "atRisk";
    public const string Achieved = "achieved";
    public const string Missed = "missed";
}

public static class ActivityTypes
{
    /// <summary>
    /// Deliberately a static list rather than a Distinct() over the collection: a
    /// distinct query leaves the filter dropdown empty on a fresh database and makes
    /// the options shift as data changes.
    /// </summary>
    public static readonly string[] All =
    [
        "Running", "Walking", "Cycling", "Swimming", "Strength", "Yoga", "Other"
    ];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
