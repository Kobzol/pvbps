using Antivirus.Crypto;
using Antivirus.DB;
using Antivirus.Net;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Antivirus.Util;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace Antivirus.Scan
{
    public class ScanWorker
    {
        public event Action<FileScan> OnScanCreated;
        public event Action<FileScan> OnScanUpdated;

        private DatabaseManager database;
        private VirustotalClient client;
        private BlockingCollection<string> queue;
        private CancellationTokenSource token;
        private Hasher hasher = new Hasher();
        private SubscriptionManager manager = new SubscriptionManager();

        public ScanWorker(DatabaseManager database, VirustotalClient client, BlockingCollection<string> queue, CancellationTokenSource token)
        {
            this.database = database;
            this.client = client;
            this.queue = queue;
            this.token = token;
        }

        public void Cancel()
        {
            this.token.Cancel();
            this.manager.Dispose();
        }

        public void Start()
        {
            Task.Factory.StartNew(data => this.HandleScans(), TaskCreationOptions.LongRunning, this.token.Token);
        }

        private void HandleScans()
        {
            while (!this.token.Token.IsCancellationRequested)
            {
                try
                {
                    string path = this.queue.Take(this.token.Token);
                    string hash = this.hasher.HashSha256(path);

                    FileScan scan = new FileScan(path, hash);
                    scan.Size = new FileInfo(path).Length;
                    scan = this.ReconcileScan(scan);

                    if (scan.State != FileState.Scanned)
                    {
                        this.ScanAndReport(scan);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }

        private FileScan ReconcileScan(FileScan scan)
        {
            var scans = this.database.GetScansByHash(scan.Hash);
            var existingScan = scans.Where(s => s.Path == scan.Path).ToList();

            if (existingScan.Count > 0)
            {
                return existingScan[0];
            }

            if (scans.Count > 0) // copy scan report and state
            {
                scan.Report = scans[0].Report;
                scan.State = scans[0].State;
            }

            this.database.InsertScan(scan);
            this.OnScanCreated(scan);

            return scan;
        }

        private void ScanAndReport(FileScan scan)
        {
            var uploadSource = Observable.If(
                () => scan.State == FileState.WaitingForScan,
                this.client.UploadFile(scan.Path)
                    .SubscribeOn(Scheduler.Default)
                    .Catch((Exception ex) => {
                        return Observable.Throw<FileScanResult>(ex).DelaySubscription(TimeSpan.FromSeconds(30));
                    })
                    .Retry(),
                Observable.Return(new FileScanResult())
            );

            var reportSource = uploadSource.SelectMany(result =>
            {
                return this.client.GetFileReport(scan.Hash)
                .SelectMany(report =>
                {
                    if (report?.ResponseCode == "-2" && scan.State == FileState.WaitingForScan)
                    {
                        scan.State = FileState.QueuedForAnalysis;
                        this.OnScanUpdated(scan);
                    }

                    if (report == null || report.Scans == null)
                    {
                        return Observable.Throw<FileReportResult>(new Exception("Not ready yet"));
                    }
                    return Observable.Return(report);
                })
                .Catch((Exception ex) =>
                {
                    return Observable.Throw<FileReportResult>(ex).DelaySubscription(TimeSpan.FromSeconds(30));
                })
                .Retry();
            });
            this.manager += reportSource
                .Subscribe(report => {
                    scan.Report = report;
                    scan.State = FileState.Scanned;
                    this.database.UpdateScan(scan);
                    this.OnScanUpdated(scan);
                });
        }
    }
}
