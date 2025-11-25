using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace ThinkMine.Services
{
    public class UpdateInfo
    {
        public string Version { get; set; }
        public string Notes { get; set; }
        public string Url { get; set; }
    }

    public class UpdateService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<UpdateInfo> CheckForUpdate()
        {
            try
            {
                var json = await _httpClient.GetStringAsync(Config.VersionUrl);
                var remoteInfo = JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (remoteInfo != null && Version.TryParse(remoteInfo.Version, out var remoteVersion))
                {
                    var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    if (remoteVersion > localVersion)
                    {
                        return remoteInfo;
                    }
                }
            }
            catch
            {
                // Fail silently (network error, etc.)
            }

            return null;
        }
    }
}
