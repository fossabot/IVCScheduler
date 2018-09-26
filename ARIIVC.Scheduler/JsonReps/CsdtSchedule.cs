using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IVCScheduler;

namespace ARIIVC.Scheduler.JsonReps
{

    public class csdtschedule
    {
        public string _region_id { get; set; }
        public string environment { get; set; }
        public string version { get; set; }
        public string date { get; set; }
        public string host_name { get; set; }
        public string host_relpri { get; set; }
        public bool? not_an_event { get; set; }
        public string eventid { get; set; }
        public string startdate { get; set; }
        public string status { get; set; }
        public string enddate { get; set; }
        public string ip_addr { get; set; }
        public string service { get; set; }
        public CustomerEnvDetails envdetails { get; set; }
    }

    public class scheduleList
    {
        public string _region_id { get; set; }
        public string environment { get; set; }
        public string version { get; set; }
        public string date { get; set; }
        public string host_name { get; set; }
        public string host_relpri { get; set; }
        public bool? not_an_event { get; set; }
        public string eventid { get; set; }
        public string startdate { get; set; }
        public string status { get; set; }
        public string enddate { get; set; }
        public string ip_addr { get; set; }
        public string service { get; set; }
        public string envcode { get; set; }
    }
}
