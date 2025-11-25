namespace ThinkMine.Services
{
    public static class Config
    {
        // Update Checker
        public const string VersionUrl = "https://raw.githubusercontent.com/raiyaancreates/ThinkMine/main/version.json";
        public const string DownloadUrl = "https://github.com/raiyaancreates/ThinkMine/releases/latest";

        // Analytics (Google Analytics 4)
        // TODO: User must replace these with their own GA4 Measurement ID and API Secret
        public const string GaMeasurementId = "G-XXXXXXXXXX"; 
        public const string GaApiSecret = "XXXXXXXXXXXXXXXXXXXXXX";
        public const string GaEndpoint = $"https://www.google-analytics.com/mp/collect?measurement_id={GaMeasurementId}&api_secret={GaApiSecret}";
    }
}
