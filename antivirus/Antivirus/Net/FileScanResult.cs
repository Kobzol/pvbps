using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Antivirus.Net
{
    public class FileScanResult
    {
        public string Permalink { get; set; }
        public string Resource { get; set; }
        public string ResponseCode { get; set; }
        public string ScanId { get; set; }
        public string VerboseMsg { get; set; }
        public string Sha256 { get; set; }
    }
}
