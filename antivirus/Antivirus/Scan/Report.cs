using Antivirus.Net;

namespace Antivirus.Scan
{
    public class Report
    {
        public int Id { get; set; }
        public string Hash { get; set; }
        public FileReportResult Result { get; set; }
        public ReportState State { get; set; } = ReportState.WaitingForScan;

        public Report()
        {

        }
        public Report(string hash)
        {
            this.Hash = hash;
        }
    }
}
