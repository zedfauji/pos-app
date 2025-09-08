using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MagiDesk.Shared.DTOs.Tables;

namespace MagiDesk.Frontend.Services
{
    public class TablesApiService
    {
        private readonly HttpClient _http;

        public TablesApiService(string baseUrl)
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            _http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"), Timeout = TimeSpan.FromSeconds(15) };
        }

        public async Task<bool> HealthAsync(CancellationToken ct = default)
        {
            try { var res = await _http.GetAsync("health", ct); return res.IsSuccessStatusCode; } catch { return false; }
        }

        public Task<List<TableStatusDto>?> GetTablesAsync(CancellationToken ct = default)
            => _http.GetFromJsonAsync<List<TableStatusDto>>("tables", ct);

        public Task<Dictionary<string, int>?> GetCountsAsync(CancellationToken ct = default)
            => _http.GetFromJsonAsync<Dictionary<string, int>>("tables/counts", ct);

        public Task<TableStatusDto?> GetTableAsync(string label, CancellationToken ct = default)
            => _http.GetFromJsonAsync<TableStatusDto>($"tables/{Uri.EscapeDataString(label)}", ct);

        public Task<HttpResponseMessage> PutTableAsync(string label, TableStatusDto rec, CancellationToken ct = default)
            => _http.PutAsJsonAsync($"tables/{Uri.EscapeDataString(label)}", rec, ct);

        public Task<HttpResponseMessage> UpsertAsync(TableStatusDto rec, CancellationToken ct = default)
            => _http.PostAsJsonAsync("tables/upsert", rec, ct);

        public Task<HttpResponseMessage> BulkUpsertAsync(IEnumerable<TableStatusDto> recs, CancellationToken ct = default)
            => _http.PostAsJsonAsync("tables/bulkUpsert", recs, ct);

        public Task<HttpResponseMessage> SeedAsync(CancellationToken ct = default)
            => _http.PostAsync("tables/seed", content: null, ct);

        public Task<HttpResponseMessage> StartSessionAsync(string label, StartSessionRequest req, CancellationToken ct = default)
            => _http.PostAsJsonAsync($"tables/{Uri.EscapeDataString(label)}/start", req, ct);

        public Task<HttpResponseMessage> StopSessionAsync(string label, CancellationToken ct = default)
            => _http.PostAsync($"tables/{Uri.EscapeDataString(label)}/stop", content: null, ct);

        public Task<List<ItemLine>?> GetSessionItemsAsync(string label, CancellationToken ct = default)
            => _http.GetFromJsonAsync<List<ItemLine>>($"tables/{Uri.EscapeDataString(label)}/items", ct);

        public Task<HttpResponseMessage> ReplaceSessionItemsAsync(string label, List<ItemLine> items, CancellationToken ct = default)
            => _http.PostAsJsonAsync($"tables/{Uri.EscapeDataString(label)}/items", items ?? new(), ct);

        public async Task<decimal?> GetRatePerMinuteAsync(CancellationToken ct = default)
        {
            try
            {
                var res = await _http.GetAsync("settings/rate", ct);
                if (!res.IsSuccessStatusCode) return null;
                var doc = await res.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(cancellationToken: ct);
                if (doc != null && doc.TryGetPropertyValue("ratePerMinute", out var val) && val != null)
                {
                    if (decimal.TryParse(val.ToString(), out var d)) return d;
                }
                return null;
            }
            catch { return null; }
        }

        public async Task<bool> SetRatePerMinuteAsync(decimal rate, CancellationToken ct = default)
        {
            try
            {
                var res = await _http.PutAsync("settings/rate", JsonContent.Create(rate), ct);
                return res.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}
