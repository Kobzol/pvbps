using System.Collections.Generic;

namespace Antivirus.Net
{
    public class FileReportResult
    {
        public string Permalink { get; set; }
        public string Resource { get; set; }
        public string ResponseCode { get; set; }
        public string ScanId { get; set; }
        public string VerboseMsg { get; set; }
        public string Sha256 { get; set; }
        public int Positives { get; set; }
        public int Total { get; set; }
        public Dictionary<string, AntivirusResult> Scans { get; set; }
    }
}
