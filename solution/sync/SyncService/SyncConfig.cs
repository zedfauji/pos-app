using System.Text.Json.Serialization;

namespace MagiDesk.SyncService;

public class SyncConfig
{
    [JsonPropertyName("firebirdPaths")]
    public List<string> FirebirdPaths { get; set; } = new();

    // If firebirdPaths is empty, the service will scan this directory for *.fdb files by default
    [JsonPropertyName("defaultFirebirdDirectory")]
    public string DefaultFirebirdDirectory { get; set; } = @"C:\\Base de datos\\billar";

    [JsonPropertyName("syncIntervalMinutes")]
    public int SyncIntervalMinutes { get; set; } = 5;

    [JsonPropertyName("firestoreProjectId")]
    public string FirestoreProjectId { get; set; } = string.Empty;

    [JsonPropertyName("serviceAccountJsonPath")]
    public string ServiceAccountJsonPath { get; set; } = string.Empty;

    // Name of the timestamp column in tables (e.g., LAST_UPDATED). Default: last_updated
    [JsonPropertyName("lastUpdatedColumnName")]
    public string LastUpdatedColumnName { get; set; } = "last_updated";

    // Optional Firebird credentials (defaults used if empty)
    [JsonPropertyName("firebirdUser")] public string? FirebirdUser { get; set; }
    [JsonPropertyName("firebirdPassword")] public string? FirebirdPassword { get; set; }
}
