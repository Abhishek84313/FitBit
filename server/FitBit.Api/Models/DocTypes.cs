namespace FitBit.Api.Models;

/// <summary>
/// Users, activities and goals all share the single `FitnessTracker` collection.
/// This field is what tells them apart, and filtering on it is a security
/// boundary, not a convenience — see <see cref="Repositories.MongoRepositoryBase{T}"/>.
/// </summary>
public static class DocTypes
{
    public const string User = "user";
    public const string Activity = "activity";
    public const string Goal = "goal";
}
