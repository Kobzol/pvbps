using Antivirus.DB;
using Antivirus.Net;
using System.Collections.Generic;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using Antivirus.Crypto;
using System.Threading.Tasks;

namespace Antivirus.Scan
{
    public class ScanManager
    {
        public List<FileScan> Scans { get; }

        public event Action<FileScan> OnScanCreated;
        public event Action<FileScan> OnScanUpdated;

        private VirustotalClient client;
        private DatabaseManager database;
        private Quarantine quarantine;

        private BlockingCollection<string> scanQueue = new BlockingCollection<string>();
        private List<ScanWorker> workers = new List<ScanWorker>();
        private object mutex = new object();

        public ScanManager(VirustotalClient client, DatabaseManager database, Quarantine quarantine, List<FileScan> scans)
        {
            this.client = client;
            this.database = database;
            this.quarantine = quarantine;
            this.Scans = scans;

            this.quarantine.OnScanUpdated += scan => this.OnScanUpdated?.Invoke(scan);
        }

        public void Start()
        {
            this.InitializeWorkers();
            this.InsertNotScannedScans(this.Scans);
        }

        public void ScanFiles(List<string> files)
        {
            foreach (var file in files)
            {
                this.scanQueue.Add(file);
            }
        }

        public async Task LockInQuarantine(FileScan scan)
        {
            await this.quarantine.LockToQuarantine(scan);
            this.database.Update(scan);
            this.database.Persist();
        }
        public async Task UnlockFromQuarantine(FileScan scan)
        {
            await this.quarantine.UnlockFromQuarantine(scan);
            this.database.Update(scan);
            this.database.Persist();
        }

        public void Remove(FileScan scan)
        {
            this.database.Remove(scan);

            lock (this.mutex)
            {
                this.Scans.Remove(scan);
            }
        }

        public void Shutdown()
        {
            this.workers.ForEach(worker => worker.Cancel());
        }

        private void InitializeWorkers()
        {
            for (int i = 0; i < 4; i++)
            {
                CancellationTokenSource token = new CancellationTokenSource();
                ScanWorker worker = new ScanWorker(this.Scans, this.database, this.client, this.scanQueue, token);
                this.workers.Add(worker);
                worker.OnScanCreated += this.CreateScan;
                worker.OnScanUpdated += this.UpdateScan;
                worker.OnScanReplaced += this.ReplaceScan;
                worker.Start();
            }
        }

        private void ReplaceScan(int id, FileScan scan)
        {
            lock (this.mutex)
            {
                int index = this.Scans.FindIndex(s => s.Id == id);
                if (index != -1)
                {
                    this.Scans[index] = scan;
                }
            }
            this.OnScanUpdated?.Invoke(scan);
        }

        private void InsertNotScannedScans(List<FileScan> scans)
        {
            this.ScanFiles(scans
                .Where(scan => scan.Report.State != ReportState.Scanned)
                .Select(scan => scan.Path).ToList());
        }

        private void CreateScan(FileScan scan)
        {
            lock (this.mutex)
            {
                this.Scans.Add(scan);
            }

            this.OnScanCreated?.Invoke(scan);
        }
        private void UpdateScan(FileScan scan)
        {
            this.OnScanUpdated?.Invoke(scan);
        }
    }
}
