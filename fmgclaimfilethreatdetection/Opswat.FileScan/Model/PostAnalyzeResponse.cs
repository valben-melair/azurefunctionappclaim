using System;
using System.Collections.Generic;
using System.Text;

namespace FMGClaimFile.Upload
{
    public class PostAnalyzeResponse
    {
        public string data_id { get; set; }
        public int inqueue { get; set; }
        public string queue_priority { get; set; }
        public string sha1 { get; set; }
        public string sha256 { get; set; }
        public string status { get; set; }
    }
}
