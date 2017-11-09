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
        private MemoryStream memory;
        private string path;
        private LiteDatabase database;

        private LiteCollection<FileScan> scans;

        public DatabaseManager(string path)
        {
            this.memory = new MemoryStream();
            this.path = path;

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
            this.scans = this.database.GetCollection<FileScan>("filescan");
            this.scans.EnsureIndex(scan => scan.UniqueHash);
            this.scans.EnsureIndex(scan => scan.Hash);
        }

        public FileScan GetScan(string path, string hash)
        {
            var unique = this.CreateUniqueHash(path, hash);
            return this.scans.FindOne(scan => scan.UniqueHash == unique);
        }
        public FileScan GetScanByHash(string hash)
        {
            return this.scans.FindOne(scan => scan.Hash == hash);
        }

        public List<FileScan> GetScans()
        {
             return this.scans.FindAll().ToList();
        }
        public void InsertScan(FileScan scan)
        {
            var unique = this.CreateUniqueHash(scan.Path, scan.Hash);
            scan.UniqueHash = unique;
            this.scans.Insert(scan);
        }
        public void UpdateScan(FileScan scan)
        {
            this.scans.Update(scan);
        }

        public void Persist()
        {
            File.WriteAllBytes(this.path, this.memory.ToArray());
        }

        public void Remove(FileScan scan)
        {
            this.scans.Delete(scan.Id);
        }

        private string CreateUniqueHash(string path, string hash)
        {
            return $"{path}#{hash}";
        }
    }
}
