using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ThinkMine.Services
{
    public class AnalyticsService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _clientId;

        public AnalyticsService(string clientId)
        {
            _clientId = clientId;
        }

        public async Task TrackLaunch()
        {
            if (Config.GaMeasurementId.Contains("XXXX")) return; // Don't track if not configured

            try
            {
                var payload = new
                {
                    client_id = _clientId,
                    events = new[]
                    {
                        new
                        {
                            name = "app_launch",
                            @params = new
                            {
                                app_name = "ThinkMine",
                                app_version = "1.0.0", // Could get from Assembly
                                platform = "Windows"
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await _httpClient.PostAsync(Config.GaEndpoint, content);
            }
            catch
            {
                // Fail silently (analytics should not break app)
            }
        }
    }
}
