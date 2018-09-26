using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    public class jenkinsNode
    {
        public string _class { get; set; }
        public string displayName { get; set; }
        public string icon { get; set; }
        public string iconClassName { get; set; }
        public bool idle { get; set; }
        public bool jnlpAgent { get; set; }
        public bool launchSupported { get; set; }
        public bool manualLaunchAllowed { get; set; }
        public int numExecutors { get; set; }
        public bool offline { get; set; }
        public object offlineCause { get; set; }
        public string offlineCauseReason { get; set; }
        public bool temporarilyOffline { get; set; }
    }
}
