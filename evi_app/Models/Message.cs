using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVI_App.Models
{
    public class Message
    {
        public long Id { get; set; }
        public string Xml { get; set; }
        public string UserId { get; set; }
        public string FiscalTaxId { get; set; }
        public bool Processed { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
