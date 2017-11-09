using Antivirus.Scan;
using LiteDB;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace Antivirus.DB
{
    public class DatabaseManager
    {
        public string Path { get; }

        private MemoryStream memory;
        public LiteDatabase database;

        public LiteCollection<FileScan> scans;
        public LiteCollection<Report> reports;

        public object Mutex { get; } = new object();

        public DatabaseManager(string path)
        {
            this.memory = new MemoryStream();
            this.Path = path;

            try
            {
                using (var file = new FileStream(path, System.IO.FileMode.Open))
                {
                    file.CopyTo(this.memory);
                }
            }
            catch (Exception)
            {
                // database does not exist or is corrupted
            }

            this.database = new LiteDatabase(this.memory);
            this.scans = this.database.GetCollection<FileScan>("filescan")
                .Include(scan => scan.Report);
            this.scans.EnsureIndex(scan => scan.Path, true);

            this.reports = this.database.GetCollection<Report>("report");
            this.reports.EnsureIndex(report => report.Hash, true);

            BsonMapper.Global
                .Entity<FileScan>()
                .DbRef(scan => scan.Report, "report");
        }

        public FileScan GetScan(string path)
        {
            return this.scans.FindOne(scan => scan.Path == path);
        }
        public List<FileScan> GetScans()
        {
             return this.scans.FindAll().ToList();
        }
        public void Insert(FileScan scan)
        {
            this.scans.Insert(scan);
        }
        public void Update(FileScan scan)
        {
            this.scans.Update(scan);
        }
        public void Remove(FileScan scan)
        {
            lock (this.Mutex)
            {
                this.scans.Delete(scan.Id);
                if (this.scans.Find(s => s.Report.Id == scan.Report.Id).Count() == 0)
                {
                    this.reports.Delete(scan.Report.Id);
                }
            }
        }

        public Report GetOrInsertReport(string hash)
        {
            lock (this.Mutex)
            {
                var report = this.reports.FindOne(r => r.Hash == hash);
                if (report != null)
                {
                    return report;
                }

                report = new Report(hash);
                this.Insert(report);
                return report;
            }
        }
        public void Insert(Report report)
        {
            this.reports.Insert(report);
        }
        public void Update(Report report)
        {
            this.reports.Update(report);
        }
        public void Remove(Report report)
        {
            this.reports.Insert(report);
        }

        public void Persist()
        {
            File.WriteAllBytes(this.Path, this.memory.ToArray());
        }
    }
}
