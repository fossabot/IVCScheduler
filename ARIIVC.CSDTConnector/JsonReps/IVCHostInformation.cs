using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ARIIVC.CSDTConnector.JsonReps
{
    public class IVCHostInformation
    {
        public string _region_id { get; set; }
        public string _repoid { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public string ip_addr { get; set; }
        public string created_by { get; set; }
        public DateTime created_time { get; set; }
        public string environment { get; set; }
        public string service { get; set; }
        public List<object> environment_details { get; set; }
        public string last_modified_by { get; set; }
        public DateTime last_modified { get; set; }
        public string host_relpri { get; set; }
        public string relpri { get; set; }
        public string path { get; set; }
        public string installstatus { get; set; }
    }
}
