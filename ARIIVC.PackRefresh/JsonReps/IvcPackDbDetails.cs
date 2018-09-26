using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{

    public class ivc_pack_db_details
    {

        public string pack { get; set; }

        public string config { get; set; }

        public masterdetails master;

        public List<slavedetails> workers;
    }

    public class masterdetails
    {
        public string server { get; set; }
        public string servicename { get; set; }
        public string pdbname { get; set; }
        
    }

    public class slavedetails
    {
        public string server { get; set; }
        public string servicename { get; set; }
        public string pdbname { get; set; }

    }
}
