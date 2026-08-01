using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FitBit.Api.Models;

[BsonIgnoreExtraElements]
public sealed class ActivityDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("docType")]
    public string DocType { get; set; } = DocTypes.Activity;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = default!;

    [BsonElement("type")]
    public string Type { get; set; } = default!;

    /// <summary>UTC instant — used for sorting and range queries only.</summary>
    [BsonElement("date")]
    public DateTime DateUtc { get; set; }

    /// <summary>
    /// The "yyyy-MM-dd" string of the calendar day the user meant. All bucketing,
    /// grouping and goal-window comparison uses this, never DateUtc.Date: in IST
    /// (UTC+5:30) an activity logged before 5:30am local falls on the previous UTC
    /// day and would jump a column in the trend chart.
    /// </summary>
    [BsonElement("dateKey")]
    public string DateKey { get; set; } = default!;

    [BsonElement("durationMinutes")]
    public int DurationMinutes { get; set; }

    [BsonElement("distanceKm")]
    public double? DistanceKm { get; set; }

    [BsonElement("calories")]
    public int? Calories { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAtUtc { get; set; }
}
