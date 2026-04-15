using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MadinaEnterprises.Modules.Models;

namespace MadinaEnterprises.Modules.Services
{
    /// <summary>
    /// Talks to Supabase over the PostgREST HTTP API.
    ///
    /// Column naming convention used in Supabase: <c>snake_case</c>.
    /// See Resources/Raw/supabase_schema.sql for the exact schema this service expects.
    ///
    /// The service is intentionally forgiving — every network call is wrapped in a try/catch
    /// so the app keeps working offline. Errors are stored on <see cref="LastError"/> for
    /// UI display but never thrown to callers.
    /// </summary>
    public class CloudSyncService
    {
        private readonly HttpClient _http;

        public bool IsEnabled => CloudConfig.IsConfigured;
        public string? LastError { get; private set; }
        public DateTime? LastSyncedAt { get; private set; }

        public CloudSyncService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        private void ApplyAuthHeaders(HttpRequestMessage req)
        {
            req.Headers.Remove("apikey");
            req.Headers.Add("apikey", CloudConfig.AnonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CloudConfig.AnonKey);
        }

        private string TableUrl(string table) => $"{CloudConfig.Url}/rest/v1/{table}";

        // ---------------- Write path: upsert + delete ----------------

        public Task UpsertAsync(string table, Dictionary<string, object?> row, string primaryKey,
                                CancellationToken ct = default)
            => UpsertManyAsync(table, new[] { row }, primaryKey, ct);

        public async Task UpsertManyAsync(string table, IEnumerable<Dictionary<string, object?>> rows,
                                          string primaryKey, CancellationToken ct = default)
        {
            if (!IsEnabled) return;
            try
            {
                var url = $"{TableUrl(table)}?on_conflict={primaryKey}";
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                ApplyAuthHeaders(req);
                req.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");

                var json = JsonSerializer.Serialize(rows);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var resp = await _http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync(ct);
                    LastError = $"Upsert {table} failed ({(int)resp.StatusCode}): {body}";
                }
            }
            catch (Exception ex)
            {
                LastError = $"Upsert {table} threw: {ex.Message}";
            }
        }

        public async Task DeleteAsync(string table, string primaryKey, string id,
                                      CancellationToken ct = default)
        {
            if (!IsEnabled) return;
            try
            {
                var url = $"{TableUrl(table)}?{primaryKey}=eq.{Uri.EscapeDataString(id)}";
                using var req = new HttpRequestMessage(HttpMethod.Delete, url);
                ApplyAuthHeaders(req);
                using var resp = await _http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync(ct);
                    LastError = $"Delete {table} failed ({(int)resp.StatusCode}): {body}";
                }
            }
            catch (Exception ex)
            {
                LastError = $"Delete {table} threw: {ex.Message}";
            }
        }

        // ---------------- Read path: fetch-all used during pull ----------------

        public async Task<List<JsonElement>?> FetchAllAsync(string table, CancellationToken ct = default)
        {
            if (!IsEnabled) return null;
            try
            {
                var url = $"{TableUrl(table)}?select=*";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyAuthHeaders(req);
                using var resp = await _http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync(ct);
                    LastError = $"Fetch {table} failed ({(int)resp.StatusCode}): {body}";
                    return null;
                }

                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;
                var list = new List<JsonElement>();
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    list.Add(el.Clone());
                }
                return list;
            }
            catch (Exception ex)
            {
                LastError = $"Fetch {table} threw: {ex.Message}";
                return null;
            }
        }

        public void MarkSynced() => LastSyncedAt = DateTime.Now;

        // ---------------- Model -> row dictionary helpers ----------------
        // Keep these here so the shape lives in one place and matches supabase_schema.sql.

        public static Dictionary<string, object?> Row(Ginners g) => new()
        {
            ["ginner_id"] = g.GinnerID,
            ["ginner_name"] = g.GinnerName,
            ["contact"] = g.Contact,
            ["iban"] = g.IBAN,
            ["address"] = g.Address,
            ["ntn"] = g.NTN,
            ["stn"] = g.STN,
            ["bank_address"] = g.BankAddress,
            ["contact_person"] = g.ContactPerson,
            ["station"] = g.Station,
        };

        public static Dictionary<string, object?> Row(Mills m) => new()
        {
            ["mill_id"] = m.MillID,
            ["mill_name"] = m.MillName,
            ["address"] = m.Address,
            ["owner_name"] = m.OwnerName,
        };

        public static Dictionary<string, object?> Row(Contracts c) => new()
        {
            ["contract_id"] = c.ContractID,
            ["ginner_id"] = c.GinnerID,
            ["mill_id"] = c.MillID,
            ["total_bales"] = c.TotalBales,
            ["price_per_batch"] = c.PricePerBatch,
            ["total_amount"] = c.TotalAmount,
            ["commission_percentage"] = c.CommissionPercentage,
            ["date_created"] = c.DateCreated.ToString("yyyy-MM-dd"),
            ["delivery_notes"] = c.DeliveryNotes,
            ["payment_notes"] = c.PaymentNotes,
            ["description"] = c.Description,
        };

        public static Dictionary<string, object?> Row(Deliveries d) => new()
        {
            ["delivery_id"] = d.DeliveryID,
            ["contract_id"] = d.ContractID,
            ["amount"] = d.Amount,
            ["total_bales"] = d.TotalBales,
            ["factory_weight"] = d.FactoryWeight,
            ["mill_weight"] = d.MillWeight,
            ["truck_number"] = d.TruckNumber,
            ["driver_contact"] = d.DriverContact,
            ["departure_date"] = d.DepartureDate.ToString("yyyy-MM-dd"),
            ["delivery_date"] = d.DeliveryDate.ToString("yyyy-MM-dd"),
        };

        public static Dictionary<string, object?> Row(Payment p) => new()
        {
            ["payment_id"] = p.PaymentID,
            ["contract_id"] = p.ContractID,
            ["total_amount"] = p.TotalAmount,
            ["amount_paid"] = p.AmountPaid,
            ["total_bales"] = p.TotalBales,
            ["date"] = p.Date.ToString("yyyy-MM-dd"),
            ["transaction_id"] = p.TransactionID,
        };

        // ---------------- JsonElement -> model helpers used while pulling ----------------

        public static string S(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null) return string.Empty;
            return p.ValueKind == JsonValueKind.String ? p.GetString() ?? string.Empty : p.ToString();
        }

        public static string? SNullable(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null) return null;
            return p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
        }

        public static int I(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null) return 0;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var v)) return v;
            return int.TryParse(p.ToString(), out var parsed) ? parsed : 0;
        }

        public static double D(JsonElement el, string name)
        {
            if (!el.TryGetProperty(name, out var p) || p.ValueKind == JsonValueKind.Null) return 0;
            if (p.ValueKind == JsonValueKind.Number && p.TryGetDouble(out var v)) return v;
            return double.TryParse(p.ToString(), out var parsed) ? parsed : 0;
        }

        public static DateTime Dt(JsonElement el, string name)
        {
            var s = SNullable(el, name);
            if (string.IsNullOrWhiteSpace(s)) return DateTime.MinValue;
            return DateTime.TryParse(s, out var dt) ? dt : DateTime.MinValue;
        }
    }
}
