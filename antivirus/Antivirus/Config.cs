namespace Antivirus
{
    public class Config
    {
        public string ApiUrl { get; } = "https://www.virustotal.com/vtapi/v2/";
        public string ApiKey { get; } = "769684866799bd6896a479fa9aa1310fb231d59a3cd39a22c8a1845afa394f1d";

        public string DatabaseFile { get; } = "scans.db";

        public string QuarantinePath { get; } = "D:/quarantine";
        public string QuarantineKey { get; } = "PVBPS_2017";

        public Config()
        {

        }
        public Config(string apiUrl, string apiKey, string databaseFile, string quarantinePath, string quarantineKey)
        {
            this.ApiUrl = apiUrl;
            this.ApiKey = apiKey;
            this.DatabaseFile = databaseFile;
            this.QuarantinePath = quarantinePath;
            this.QuarantineKey = quarantineKey;
        }
    }
}
