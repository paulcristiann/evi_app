using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace evi_app.Models
{
    public class UploadedCertificate
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string FileName { get; set; }
        public DateTime Timestamp { get; set; }
        public string FilePath { get; set; }
    }
}
