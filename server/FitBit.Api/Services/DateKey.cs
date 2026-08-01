using System.Globalization;

namespace FitBit.Api.Services;

/// <summary>
/// Calendar days are handled as "yyyy-MM-dd" strings throughout. They sort
/// lexicographically, compare without a timezone, and survive the round trip
/// through BSON's UTC-normalised dates unchanged — which DateTime.Date does not.
/// </summary>
public static class DateKey
{
    public const string Format = "yyyy-MM-dd";

    public static string From(DateOnly date) => date.ToString(Format, CultureInfo.InvariantCulture);

    public static DateOnly Parse(string dateKey) =>
        DateOnly.ParseExact(dateKey, Format, CultureInfo.InvariantCulture);

    public static bool TryParse(string? dateKey, out DateOnly date) =>
        DateOnly.TryParseExact(dateKey, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>
    /// The UTC instant stored alongside the key, pinned to midday so that a
    /// display-side timezone shift can never move it across a day boundary.
    /// Kind is set explicitly — the driver treats Unspecified inconsistently.
    /// </summary>
    public static DateTime ToUtcInstant(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(12, 0)), DateTimeKind.Utc);

    public static string Weekday(DateOnly date) =>
        date.DayOfWeek.ToString()[..3];

    /// <summary>Inclusive list of keys from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public static IEnumerable<DateOnly> Range(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1)) yield return d;
    }
}
