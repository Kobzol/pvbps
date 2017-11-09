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
using System.Collections.Generic;

namespace Antivirus.Scan
{
    public class ScanWorker
    {
        public event Action<FileScan> OnScanCreated;
        public event Action<FileScan> OnScanUpdated;
        public event Action<int, FileScan> OnScanReplaced;

        private DatabaseManager database;
        private VirustotalClient client;
        private BlockingCollection<string> queue;
        private CancellationTokenSource token;
        private Hasher hasher = new Hasher();
        private SubscriptionManager manager = new SubscriptionManager();

        List<FileScan> scans;
        public ScanWorker(List<FileScan> scans, DatabaseManager database, VirustotalClient client, BlockingCollection<string> queue, CancellationTokenSource token)
        {
            this.scans = scans;
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

                    Report report = this.database.GetOrInsertReport(hash);

                    FileScan scan = new FileScan(path, report);
                    scan = this.ReconcileScan(scan);

                    if (scan.Report.State != ReportState.Scanned)
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
            var existingScan = this.database.GetScan(scan.Path);
            scan.Size = new FileInfo(scan.Path).Length;

            if (existingScan != null)
            {
                existingScan.Size = scan.Size;
                existingScan.Report = scan.Report;
                this.database.Update(existingScan);
                this.OnScanReplaced?.Invoke(existingScan.Id, existingScan);
                return existingScan;
            }
            else
            {
                this.database.Insert(scan);
                this.OnScanCreated?.Invoke(scan);
                return scan;
            }
        }

        private void ScanAndReport(FileScan scan)
        {
            var uploadSource = Observable.If(
                () => scan.Report.State == ReportState.WaitingForScan,
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
                return this.client.GetFileReport(scan.Report.Hash)
                .SelectMany(report =>
                {
                    if (report?.ResponseCode == "-2" && scan.Report.State == ReportState.WaitingForScan)
                    {
                        scan.Report.State = ReportState.QueuedForAnalysis;
                        this.database.Update(scan.Report);
                        this.OnScanUpdated?.Invoke(scan);
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
                .Subscribe(result => {
                    scan.Report.Result = result;
                    scan.Report.State = ReportState.Scanned;
                    this.database.Update(scan.Report);
                    this.OnScanUpdated?.Invoke(scan);
                });
        }
    }
}
