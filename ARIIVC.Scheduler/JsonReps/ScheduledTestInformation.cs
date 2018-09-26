using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    public class ScheduledTestInformation
    {
        public List<testsuite> tests;
        public testpackinformation appinfo;
        public releaseinformation releaseinfo;
        public string product { get; set; }
        public string packname { get; set; }
        public string group { get; set; }
        public string name { get; set; }
        public string frequency { get; set; }
        public string time { get; set; }
        public string appserver { get; set; }
        public string subscribers { get; set; }
        public string concurrent { get; set; }
        public string owner { get; set; }
        public string createdtime { get; set; }

        public ScheduledTestInformation()
        {
            appinfo = new testpackinformation();
            tests = new List<testsuite>();
            releaseinfo = new releaseinformation();
        }
    }
}
