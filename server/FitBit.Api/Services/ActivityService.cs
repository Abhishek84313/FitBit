using FitBit.Api.Dtos.Activities;
using FitBit.Api.Dtos.Common;
using FitBit.Api.Infrastructure;
using FitBit.Api.Models;
using FitBit.Api.Repositories;

namespace FitBit.Api.Services;

public interface IActivityService
{
    Task<PagedResult<ActivityResponse>> QueryAsync(
        string userId, string? type, string? from, string? to,
        int page, int pageSize, string sort, CancellationToken ct = default);

    Task<ActivityResponse> GetAsync(string id, string userId, CancellationToken ct = default);
    Task<ActivityResponse> CreateAsync(string userId, ActivityCreateRequest request, CancellationToken ct = default);
    Task<ActivityResponse> UpdateAsync(string id, string userId, ActivityUpdateRequest request, CancellationToken ct = default);
    Task DeleteAsync(string id, string userId, CancellationToken ct = default);
}

public sealed class ActivityService : IActivityService
{
    private readonly IActivityRepository _activities;

    public ActivityService(IActivityRepository activities) => _activities = activities;

    public async Task<PagedResult<ActivityResponse>> QueryAsync(
        string userId, string? type, string? from, string? to,
        int page, int pageSize, string sort, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        if (!ActivityRepository.AllowedSorts.Contains(sort)) sort = "date_desc";

        if (!string.IsNullOrWhiteSpace(type) && !ActivityTypes.IsValid(type))
            throw ApiException.BadRequest($"Unknown activity type '{type}'.");

        var fromKey = NormalizeOptionalKey(from, nameof(from));
        var toKey = NormalizeOptionalKey(to, nameof(to));

        var (items, total) = await _activities.QueryAsync(
            new ActivityFilter(userId, type, fromKey, toKey), sort, page, pageSize, ct);

        return new PagedResult<ActivityResponse>(items.Select(ToResponse).ToList(), total, page, pageSize);
    }

    public async Task<ActivityResponse> GetAsync(string id, string userId, CancellationToken ct = default)
    {
        // 404 rather than 403 for another user's document: a 403 would confirm
        // that the id exists.
        var activity = await _activities.GetByIdAsync(id, userId, ct) ?? throw ApiException.NotFound("Activity not found.");
        return ToResponse(activity);
    }

    public async Task<ActivityResponse> CreateAsync(string userId, ActivityCreateRequest request, CancellationToken ct = default)
    {
        var date = ParseDate(request.Date);
        Validate(request);

        var activity = new ActivityDocument
        {
            UserId = userId,                       // from the token, never from the payload
            Type = request.Type,
            DateKey = DateKey.From(date),
            DateUtc = DateKey.ToUtcInstant(date),
            DurationMinutes = request.DurationMinutes,
            DistanceKm = request.DistanceKm,
            Calories = request.Calories,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
        };

        await _activities.CreateAsync(activity, ct);
        return ToResponse(activity);
    }

    public async Task<ActivityResponse> UpdateAsync(
        string id, string userId, ActivityUpdateRequest request, CancellationToken ct = default)
    {
        var existing = await _activities.GetByIdAsync(id, userId, ct)
                       ?? throw ApiException.NotFound("Activity not found.");

        var date = ParseDate(request.Date);
        Validate(request);

        existing.Type = request.Type;
        existing.DateKey = DateKey.From(date);
        existing.DateUtc = DateKey.ToUtcInstant(date);
        existing.DurationMinutes = request.DurationMinutes;
        existing.DistanceKm = request.DistanceKm;
        existing.Calories = request.Calories;
        existing.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        var updated = await _activities.ReplaceAsync(existing, ct)
                      ?? throw ApiException.NotFound("Activity not found.");
        return ToResponse(updated);
    }

    public async Task DeleteAsync(string id, string userId, CancellationToken ct = default)
    {
        if (!await _activities.DeleteAsync(id, userId, ct))
            throw ApiException.NotFound("Activity not found.");
    }

    private static void Validate(ActivityCreateRequest request)
    {
        if (!ActivityTypes.IsValid(request.Type))
            throw ApiException.BadRequest(
                $"Activity type must be one of: {string.Join(", ", ActivityTypes.All)}.");
    }

    private static DateOnly ParseDate(string value)
    {
        if (!DateKey.TryParse(value, out var date))
            throw ApiException.BadRequest("Date must be in yyyy-MM-dd format.");
        return date;
    }

    private static string? NormalizeOptionalKey(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!DateKey.TryParse(value, out var date))
            throw ApiException.BadRequest($"'{field}' must be in yyyy-MM-dd format.");
        return DateKey.From(date);
    }

    internal static ActivityResponse ToResponse(ActivityDocument a) =>
        new(a.Id, a.Type, a.DateKey, a.DateKey, a.DurationMinutes, a.DistanceKm, a.Calories,
            a.Notes, a.CreatedAtUtc, a.UpdatedAtUtc);
}
