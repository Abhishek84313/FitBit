namespace FitBit.Api.Settings;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    /// <summary>
    /// Use 127.0.0.1 rather than "localhost": mongod binds IPv4 only, and Windows
    /// resolves "localhost" to ::1 first, producing a 30s server-selection timeout
    /// that looks like the database being down.
    /// </summary>
    public string ConnectionString { get; set; } = "mongodb://127.0.0.1:27017";

    public string DatabaseName { get; set; } = "Fitbit";

    public string CollectionName { get; set; } = "FitnessTracker";
}
