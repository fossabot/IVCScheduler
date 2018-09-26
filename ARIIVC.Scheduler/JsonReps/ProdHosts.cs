using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    public class prod_hosts
    {
        public string ip_addr { get; set; }
        public string service { get; set; }
        public string name { get; set; }
        public string version { get; set; }
        public string envcode { get; set; }
        public string relid { get; set; }
        public string status { get; set; }
        public string loginuser { get; set; }
        public string runstatus { get; set; }
        public string buildnum { get; set; }
        public string triggered { get; set; }
        public string rundate { get; set; }
        public string release_priority { get; set; }
    }
}
