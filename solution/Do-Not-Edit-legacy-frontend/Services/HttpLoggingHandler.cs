using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Net.Http.Headers;

namespace MagiDesk.Frontend.Services
{
    // Lightweight file logger for HTTP traffic, temporary diagnostic use
    public sealed class HttpLoggingHandler : DelegatingHandler
    {
        private readonly string _logPath;
        private static readonly object _lock = new object();

        public HttpLoggingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk");
            Directory.CreateDirectory(dir);
            _logPath = Path.Combine(dir, "frontend.log");
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var start = DateTimeOffset.Now;
            HttpResponseMessage? response = null;
            string reqLine = $">>> {start:HH:mm:ss} {request.Method} {request.RequestUri}";
            try
            {
                response = await base.SendAsync(request, cancellationToken);
                var end = DateTimeOffset.Now;
                var status = (int)response.StatusCode;
                string resLine = $"<<< {end:HH:mm:ss} {(end-start).TotalMilliseconds:0}ms {status} {request.RequestUri}";

                // Optionally include body for billing-related endpoints
                string bodyLine = string.Empty;
                try
                {
                    var uri = request.RequestUri?.ToString() ?? string.Empty;
                    bool wantsBody = uri.Contains("/bills", StringComparison.OrdinalIgnoreCase)
                                     || uri.Contains("/tables/stop", StringComparison.OrdinalIgnoreCase)
                                     || uri.Contains("/tables/start", StringComparison.OrdinalIgnoreCase);
                    if (wantsBody && response.Content != null)
                    {
                        var media = response.Content.Headers?.ContentType?.MediaType ?? "";
                        if (media.Contains("json", StringComparison.OrdinalIgnoreCase))
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var trunc = json.Length > 4096 ? json.Substring(0, 4096) + "..." : json;
                            bodyLine = "\n" + trunc + "\n";
                            // restore content so caller can still read it
                            response.Content = new StringContent(json, Encoding.UTF8, string.IsNullOrWhiteSpace(media) ? "application/json" : media);
                        }
                    }
                }
                catch { /* ignore */ }

                SafeAppend(reqLine + Environment.NewLine + resLine + bodyLine + Environment.NewLine);
                return response;
            }
            catch (Exception ex)
            {
                var end = DateTimeOffset.Now;
                string resLine = $"xxx {end:HH:mm:ss} {(end-start).TotalMilliseconds:0}ms ERROR {ex.GetType().Name}: {ex.Message}";
                SafeAppend(reqLine + Environment.NewLine + resLine + Environment.NewLine);
                throw;
            }
        }

        private void SafeAppend(string text)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logPath, text);
                }
            }
            catch { /* ignore logging errors */ }
        }
    }
}
