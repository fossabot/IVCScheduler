using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    public class testsuite
    {
        public string product { get; set; }
        public string packname { get; set; }
        public string group { get; set; }
        public string name { get; set; }
        public string module { get; set; }
        public string submodule { get; set; }
        public string test { get; set; }
        public string owner { get; set; }
        public string createdtime { get; set; }
        public List<singletest> qctests;

        public testsuite()
        {

            qctests = new List<singletest>();
        }
    }
}
