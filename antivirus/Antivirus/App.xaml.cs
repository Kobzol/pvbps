using Antivirus.DB;
using Antivirus.Net;
using Antivirus.Scan;
using System.Windows;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using Antivirus.Crypto;
using Antivirus.Util;
using System.Reactive.Linq;

namespace Antivirus
{
    public partial class App : Application
    {
        private Config config;
        private VirustotalClient client;
        private DatabaseManager database;
        private ScanManager scanManager;
        private Quarantine quarantine;
        private SubscriptionManager subManager = new SubscriptionManager();

        public App()
        {
            this.config = this.ParseConfig("config.json");

            this.client = new VirustotalClient(this.config.ApiUrl, this.config.ApiKey);
            this.database = new DatabaseManager(this.config.DatabaseFile);
            this.quarantine = new Quarantine(this.config.QuarantinePath, this.config.QuarantineKey);
            this.scanManager = new ScanManager(this.client, this.database, this.quarantine, this.database.GetScans());

            this.Exit += this.HandleExit;

            this.SetupDatabasePersist();
        }

        public void InitializeUI(object sender, StartupEventArgs e)
        {
            this.MainWindow = new UI.MainWindow(this.scanManager);
            this.MainWindow.Show();
        }

        private void HandleExit(object sender, ExitEventArgs e)
        {
            this.database.Persist();
            this.scanManager.Shutdown();
            this.subManager.Dispose();
        }

        private void SetupDatabasePersist()
        {
            this.subManager += Observable.Timer(new DateTimeOffset(DateTime.Now.AddSeconds(5)), new TimeSpan(0, 5, 0))
                .Subscribe(value => {
                    this.database.Persist();
                });
        }

        private Config ParseConfig(string path)
        {
            try
            {
                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception)
            {
                return new Config();
            }
        }
    }
}
