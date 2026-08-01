using FitBit.Api.Data;
using FitBit.Api.Models;
using MongoDB.Driver;

namespace FitBit.Api.Repositories;

public interface IUserRepository
{
    Task<UserDocument?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<UserDocument?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<UserDocument> CreateAsync(UserDocument user, CancellationToken ct = default);
}

public sealed class UserRepository : MongoRepositoryBase<UserDocument>, IUserRepository
{
    public UserRepository(FitnessTrackerContext context) : base(context) { }

    protected override string DocType => DocTypes.User;

    public Task<UserDocument?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        FindOneAsync(Builders<UserDocument>.Filter.Eq(u => u.Email, Normalize(email)), ct);

    public Task<UserDocument?> GetByIdAsync(string id, CancellationToken ct = default) =>
        FindOneAsync(Builders<UserDocument>.Filter.Eq(u => u.Id, id), ct);

    public async Task<UserDocument> CreateAsync(UserDocument user, CancellationToken ct = default)
    {
        user.DocType = DocTypes.User;
        user.Email = Normalize(user.Email);
        user.CreatedAtUtc = DateTime.UtcNow;
        await InsertAsync(user, ct);
        return user;
    }

    /// <summary>
    /// MongoDB unique indexes are case-sensitive, so normalise on both write and
    /// lookup — otherwise Abhi@x.com and abhi@x.com can both register.
    /// </summary>
    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
