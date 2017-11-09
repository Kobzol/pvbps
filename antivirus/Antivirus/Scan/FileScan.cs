using Antivirus.Net;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Antivirus.Scan
{
    public class FileScan
    {
        public int Id { get; set; } = 0;
        public string UniqueHash { get; set; }
        public string Hash { get; set; }
        public string Path { get; set; }
        public string QuarantinePath { get; set; }
        public bool InQuarantine { get; set; }
        public byte[] IV { get; set; }
        public long Size { get; set; }
        public FileReportResult Report { get; set; }
        public FileState State { get; set; }

        [BsonIgnore]
        public int PositiveResults => this.Report.Positives;

        [BsonIgnore]
        public int TotalResults => this.Report.Total;

        [BsonIgnore]
        public string GetVirusTypes => String.Join(Environment.NewLine, this.Report?.Scans?.Select(scan => $"{scan.Key}: {scan.Value.Result}").Distinct().ToList() ?? new List<string>());

        [BsonIgnore]
        public bool IsScanned => this.State != FileState.WaitingForScan && this.Report?.Scans != null;

        public FileScan()
        {

        }
        public FileScan(string path, string hash)
        {
            this.Hash = hash;
            this.Path = path;
        }
    }
}
