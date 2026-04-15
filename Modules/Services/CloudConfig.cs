using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace MadinaEnterprises.Modules.Services
{
    /// <summary>
    /// Loads the Supabase project URL + anon key that the <see cref="CloudSyncService"/> uses.
    /// Values are read (in order) from:
    ///   1. An override file in the per-user app-data folder: &lt;LocalAppData&gt;/madina/cloudsync.json
    ///      (this lets each device point at its own project without rebuilding).
    ///   2. The MAUI packaged asset Resources/Raw/cloudsync.json (shipped with the app).
    ///   3. Environment variables MADINA_SUPABASE_URL / MADINA_SUPABASE_ANON_KEY.
    /// If no config is found, cloud sync silently stays disabled and the app behaves exactly
    /// as before (local SQLite only).
    /// </summary>
    public static class CloudConfig
    {
        public static string Url { get; private set; } = string.Empty;
        public static string AnonKey { get; private set; } = string.Empty;
        public static bool IsConfigured => !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(AnonKey);

        public static string LocalOverridePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "madina", "cloudsync.json");

        public static async Task LoadAsync()
        {
            // 1. Local override (user-writable, wins over everything else)
            if (TryLoadFromFile(LocalOverridePath))
            {
                return;
            }

            // 2. MAUI packaged asset
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("cloudsync.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();
                ApplyJson(json);
                if (IsConfigured) return;
            }
            catch
            {
                // packaged asset missing or unreadable — fall through
            }

            // 3. Environment variables
            var envUrl = Environment.GetEnvironmentVariable("MADINA_SUPABASE_URL");
            var envKey = Environment.GetEnvironmentVariable("MADINA_SUPABASE_ANON_KEY");
            if (!string.IsNullOrWhiteSpace(envUrl) && !string.IsNullOrWhiteSpace(envKey))
            {
                Url = envUrl.TrimEnd('/');
                AnonKey = envKey;
            }
        }

        private static bool TryLoadFromFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return false;
                ApplyJson(File.ReadAllText(path));
                return IsConfigured;
            }
            catch
            {
                return false;
            }
        }

        private static void ApplyJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            var doc = JsonSerializer.Deserialize<JsonElement>(json);
            if (doc.ValueKind != JsonValueKind.Object) return;

            if (doc.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String)
            {
                var val = u.GetString();
                if (!string.IsNullOrWhiteSpace(val) && !val.Contains("YOUR-PROJECT", StringComparison.OrdinalIgnoreCase))
                {
                    Url = val.TrimEnd('/');
                }
            }
            if (doc.TryGetProperty("anonKey", out var k) && k.ValueKind == JsonValueKind.String)
            {
                var val = k.GetString();
                if (!string.IsNullOrWhiteSpace(val) && !val.Contains("YOUR-ANON-KEY", StringComparison.OrdinalIgnoreCase))
                {
                    AnonKey = val;
                }
            }
        }

        /// <summary>
        /// Writes (or updates) the local override file so the user can configure Supabase
        /// credentials at runtime from the UI without rebuilding the app.
        /// </summary>
        public static async Task SaveOverrideAsync(string url, string anonKey)
        {
            var dir = Path.GetDirectoryName(LocalOverridePath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new { url = url.TrimEnd('/'), anonKey },
                new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(LocalOverridePath, json);
            Url = url.TrimEnd('/');
            AnonKey = anonKey;
        }
    }
}
