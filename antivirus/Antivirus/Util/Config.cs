namespace Antivirus.Util
{
    public class Config
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }

        public string DatabaseFile { get; set; }

        public string QuarantinePath { get; set; }
        public string QuarantineKey { get; set; }

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
