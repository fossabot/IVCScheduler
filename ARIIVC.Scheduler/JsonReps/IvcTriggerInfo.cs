using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    [BsonIgnoreExtraElements]
    public class ivc_trigger_info
    {
        public string RingRelease { get; set; }
        public string SystemVersion { get; set; }
        public string RunCompleted { get; set; }
        public string DBRefresh { get; set; }
        public string PatchInstall { get; set; }
        public string PackSetup { get; set; }
        public string PackName { get; set; }
        public string Date { get; set; }
    }

}
