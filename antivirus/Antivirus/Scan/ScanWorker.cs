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

                    FileScan dbScan = this.database.GetScan(path, hash);
                    if (dbScan == null)
                    {
                        this.database.InsertScan(scan);
                        this.OnScanCreated(scan);
                        dbScan = scan;
                    }

                    if (dbScan.State != FileState.Scanned)
                    {
                        this.ScanAndReport(dbScan);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
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
