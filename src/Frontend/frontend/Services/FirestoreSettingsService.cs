using Google.Cloud.Firestore;
using System;
using System.Net;
using System.Threading.Tasks;

namespace MagiDesk.Frontend.Services
{
    public class FirestoreSettingsService
    {
        private readonly string _projectId;
        private FirestoreDb? _db;

        public FirestoreSettingsService(string? projectId = null)
        {
            _projectId = projectId ?? Environment.GetEnvironmentVariable("FIRESTORE_PROJECT_ID") ?? string.Empty;
        }

        private async Task<FirestoreDb?> GetDbAsync()
        {
            if (_db != null) return _db;
            if (string.IsNullOrWhiteSpace(_projectId)) return null;
            try
            {
                _db = await FirestoreDb.CreateAsync(_projectId);
                return _db;
            }
            catch
            {
                return null;
            }
        }

        private static string GetHostKey()
        {
            try
            {
                var host = Dns.GetHostName();
                return string.IsNullOrWhiteSpace(host) ? Environment.MachineName : host;
            }
            catch
            {
                return Environment.MachineName;
            }
        }

        private static string RootCollection => "pos-app-settings";

        public async Task<FrontendSettings?> GetFrontendSettingsAsync()
        {
            var db = await GetDbAsync();
            if (db == null) return null;
            var host = GetHostKey();
            var docRef = db.Collection(RootCollection).Document(host).Collection("frontend").Document("settings");
            var snap = await docRef.GetSnapshotAsync();
            if (!snap.Exists) return null;
            return snap.ConvertTo<FrontendSettings>();
        }

        public async Task<bool> SaveFrontendSettingsAsync(FrontendSettings settings)
        {
            var db = await GetDbAsync();
            if (db == null) return false;
            var host = GetHostKey();
            var docRef = db.Collection(RootCollection).Document(host).Collection("frontend").Document("settings");
            await docRef.SetAsync(settings, SetOptions.Overwrite);
            return true;
        }

        public async Task<BackendSettings?> GetBackendSettingsAsync()
        {
            var db = await GetDbAsync();
            if (db == null) return null;
            var host = GetHostKey();
            var docRef = db.Collection(RootCollection).Document(host).Collection("backend").Document("settings");
            var snap = await docRef.GetSnapshotAsync();
            if (!snap.Exists) return null;
            return snap.ConvertTo<BackendSettings>();
        }

        public async Task<bool> SaveBackendSettingsAsync(BackendSettings settings)
        {
            var db = await GetDbAsync();
            if (db == null) return false;
            var host = GetHostKey();
            var docRef = db.Collection(RootCollection).Document(host).Collection("backend").Document("settings");
            await docRef.SetAsync(settings, SetOptions.Overwrite);
            return true;
        }
    }

    [FirestoreData]
    public class FrontendSettings
    {
        [FirestoreProperty] public string? ApiBaseUrl { get; set; }
        [FirestoreProperty] public string? Theme { get; set; }
        [FirestoreProperty] public decimal? RatePerMinute { get; set; }
    }

    [FirestoreData]
    public class BackendSettings
    {
        [FirestoreProperty] public string? ConnectionString { get; set; }
        [FirestoreProperty] public string? Notes { get; set; }
    }
}
