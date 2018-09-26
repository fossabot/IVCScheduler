using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{

    public class ReleaseScheduler
    {
        public string _region_id { get; set; }
        public string _repoid { get; set; }
        public string environment { get; set; }
        public string version { get; set; }
        public string date { get; set; }
        public string datetime { get; set; }
        public string created_by { get; set; }
        public string created_on { get; set; }
        public string host_name { get; set; }
        public string host_ip_addr { get; set; }
        public string host_service { get; set; }
        public string host_relpri { get; set; }
        public string status { get; set; }
    }
}
