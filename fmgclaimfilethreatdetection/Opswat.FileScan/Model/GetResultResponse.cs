using System;
using System.Collections.Generic;
using System.Text;

namespace FMGClaimFile.Upload.Opswat
{
    public class GetResultResponse
    {
        public string[] additional_info { get; set; }
        public bool appinfo { get; set; }
        public string data_id { get; set; }
        public file_info file_Info { get; set; }
        public int private_processing { get; set; }
        public string rest_version { get; set; }
        public scan_results scan_results { get; set; }
        public int share_file { get; set; }
        public bool stored { get; set; }
        public votes votes { get; set; }
    }

    public class file_info
    {
        public string display_name { get; set; }
        public int file_size { get; set; }
        public string file_type_category { get; set; }
        public string file_type_description { get; set; }
        public string file_type_extension { get; set; }
        public string md5 { get; set; }
        public string sha1 { get; set; }
        public string sha256 { get; set; }
        public string upload_timestamp { get; set; }
    }

    public class scan_results
    {
        public int progress_percentage { get; set; }
        public bool rescan_available { get; set; }
        public string scan_all_result_a { get; set; }
        public int scan_all_result_i { get; set; }
        public class scan_details { }
        public string start_time { get; set; }
        public int total_avs { get; set; }
        public int total_detected_avs { get; set; }
        public int total_time { get; set; }
    }
    public class votes
    {
        public int down { get; set; }
        public int up { get; set; }
    }
}
