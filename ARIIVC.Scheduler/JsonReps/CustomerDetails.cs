using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{

    public class CustomerDetails
    {
        public string _region_id { get; set; }
        public string _repoid { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public string ip_addr { get; set; }
        public string created_by { get; set; }
        public string created_time { get; set; }
        public string environment { get; set; }
        public string service { get; set; }
        public string relpri { get; set; }
        public CustomerEnvDetails environment_details { get; set; }
    }
    public class TempCustomerDetails
    {
        public string _region_id { get; set; }
        public string _repoid { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public string ip_addr { get; set; }
        public string created_by { get; set; }
        public string created_time { get; set; }
        public string environment { get; set; }
        public string service { get; set; }
        public string relpri { get; set; }
    }
}
