using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    public class esRest
    {
        //public int crm { get; set; }
        //public int vehicles { get; set; }
        //public int aftersales { get; set; }
        //public int accounts { get; set; }
        //public int environments { get; set; }
        public int passed { get; set; }
        public int failed { get; set; }
        public int blocked { get; set; }
        public int norun { get; set; }
        public int total { get; set; }
        public string date { get; set; }
        public string time { get; set; }
        public string status { get; set; }
        public string updateid { get; set; }
        public string updatedesc { get; set; }
        public string systemversion { get; set; }
        public string filecreated { get; set; }
        public string testsetname { get; set; }
        public string lastmodified { get; set; }
        public string url { get; set; }
    }
}
