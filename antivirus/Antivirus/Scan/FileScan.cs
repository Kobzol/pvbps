using Antivirus.Crypto;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Antivirus.Scan
{
    public class FileScan
    {
        public int Id { get; set; } = 0;
        public string Path { get; set; }
        public string QuarantinePath { get; set; }
        public QuarantineState QuarantineState { get; set; }
        public byte[] IV { get; set; }
        public long Size { get; set; }
        public Report Report { get; set; }

        [BsonIgnore]
        public int PositiveResults => this.Report.Result.Positives;

        [BsonIgnore]
        public int TotalResults => this.Report.Result.Total;

        [BsonIgnore]
        public string GetVirusTypes => String.Join(Environment.NewLine, this.Report?.Result?.Scans?.Select(scan => $"{scan.Key}: {scan.Value.Result}").Distinct().ToList() ?? new List<string>());

        [BsonIgnore]
        public bool IsScanned => this.Report?.State != ReportState.WaitingForScan && this.Report?.Result?.Scans != null;

        public FileScan()
        {

        }
        public FileScan(string path, Report report)
        {
            this.Path = path;
            this.Report = report;
        }
    }
}
