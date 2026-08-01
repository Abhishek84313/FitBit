using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FitBit.Api.Models;

[BsonIgnoreExtraElements]
public sealed class UserDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("docType")]
    public string DocType { get; set; } = DocTypes.User;

    /// <summary>Always stored lower-cased; the unique index is case-sensitive.</summary>
    [BsonElement("email")]
    public string Email { get; set; } = default!;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = default!;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = default!;

    [BsonElement("createdAt")]
    public DateTime CreatedAtUtc { get; set; }
}
