using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Utilities.JsonRepo
{
    public class IVCHostDeployementProgress
    {
        public string ip_addr { get; set; }
        public string service { get; set; }
        public string version { get; set; }
        public string environment { get; set; }
        public string ivc_deploy_env { get; set; }
        public string schema_refresh { get; set; }
        public List<string> status { get; set; }
        public DateTime start_time { get; set; }
        public DateTime last_update_time { get; set; }
        public int hosts_count { get; set; }
        public string package_type { get; set; }
        public string username { get; set; }
        public bool wait_for_pid { get; set; }
        public object schedule_date { get; set; }
        public bool completed { get; set; }
    }
}
