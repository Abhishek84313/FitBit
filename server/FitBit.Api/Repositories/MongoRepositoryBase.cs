using FitBit.Api.Data;
using MongoDB.Driver;

namespace FitBit.Api.Repositories;

/// <summary>
/// The single-collection design's security boundary.
///
/// Users, activities and goals share one collection, so a query for activities
/// that forgets to filter on docType returns user documents too. The driver does
/// NOT help here: its discriminator machinery is tied to OfType&lt;TDerived&gt;(),
/// not to the collection's generic parameter, so
/// GetCollection&lt;ActivityDocument&gt;(...).Find(Empty) issues a bare find({}).
/// With [BsonIgnoreExtraElements] — which is required, since three shapes share
/// the collection — a user document then deserializes *successfully* into a
/// half-populated ActivityDocument, carrying its passwordHash along.
///
/// So the filter cannot live at call sites. <see cref="_collection"/> is private
/// and no member exposes it or an IFindFluent/IQueryable over it; every read and
/// write below routes through <see cref="Scoped"/>. There is no "forgot the
/// filter" path, and Scoped is one grep away for review.
/// </summary>
public abstract class MongoRepositoryBase<T>
{
    private readonly IMongoCollection<T> _collection;

    protected MongoRepositoryBase(FitnessTrackerContext context)
    {
        _collection = context.GetCollection<T>();
    }

    /// <summary>The docType value this repository is allowed to see.</summary>
    protected abstract string DocType { get; }

    protected FilterDefinition<T> Scoped(FilterDefinition<T>? extra = null)
    {
        var docType = Builders<T>.Filter.Eq("docType", DocType);
        return extra is null ? docType : Builders<T>.Filter.And(docType, extra);
    }

    protected Task<List<T>> FindAsync(
        FilterDefinition<T>? filter = null,
        SortDefinition<T>? sort = null,
        int? skip = null,
        int? limit = null,
        CancellationToken ct = default)
    {
        var find = _collection.Find(Scoped(filter));
        if (sort is not null) find = find.Sort(sort);
        if (skip is not null) find = find.Skip(skip);
        if (limit is not null) find = find.Limit(limit);
        return find.ToListAsync(ct);
    }

    protected Task<T?> FindOneAsync(FilterDefinition<T> filter, CancellationToken ct = default) =>
        _collection.Find(Scoped(filter)).FirstOrDefaultAsync(ct)!;

    protected Task<long> CountAsync(FilterDefinition<T>? filter = null, CancellationToken ct = default) =>
        _collection.CountDocumentsAsync(Scoped(filter), cancellationToken: ct);

    protected Task InsertAsync(T document, CancellationToken ct = default) =>
        _collection.InsertOneAsync(document, cancellationToken: ct);

    protected Task InsertManyAsync(IEnumerable<T> documents, CancellationToken ct = default) =>
        _collection.InsertManyAsync(documents, cancellationToken: ct);

    protected Task<T?> FindOneAndReplaceAsync(FilterDefinition<T> filter, T replacement, CancellationToken ct = default) =>
        _collection.FindOneAndReplaceAsync(
            Scoped(filter),
            replacement,
            new FindOneAndReplaceOptions<T> { ReturnDocument = ReturnDocument.After },
            ct)!;

    protected Task<DeleteResult> DeleteOneAsync(FilterDefinition<T> filter, CancellationToken ct = default) =>
        _collection.DeleteOneAsync(Scoped(filter), ct);

    /// <summary>
    /// Scoped to this repository's docType. Callers must still supply an owner
    /// filter — there is no "delete everything" convenience on purpose.
    /// </summary>
    protected Task<DeleteResult> DeleteManyAsync(FilterDefinition<T> filter, CancellationToken ct = default) =>
        _collection.DeleteManyAsync(Scoped(filter), ct);
}
