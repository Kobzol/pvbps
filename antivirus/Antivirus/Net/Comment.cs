using System;
using System.Globalization;

namespace Antivirus.Net
{
    public class CommentData
    {
        public string Date { get; set; }
        public string Comment { get; set; }

        public DateTime ParsedDate
        {
            get
            {
                return DateTime.ParseExact(this.Date, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            }
        }
    }
}
