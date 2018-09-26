using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    public class esPost
    {
        public int passed { get; set; }
        //public int crm { get; set; }
        //public int vehicles { get; set; }
        //public int aftersales { get; set; }
        //public int accounts { get; set; }
        //public int environments { get; set; }
        public int failed { get; set; }
        public int blocked { get; set; }
        public int norun { get; set; }
        public string timestamp { get; set; }
        public string testsetname { get; set; }
    }
}
