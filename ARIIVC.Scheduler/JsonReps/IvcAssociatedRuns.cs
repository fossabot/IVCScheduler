using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    [BsonIgnoreExtraElements]
    public class ivc_assoc_runs
    {
        public string systemversion { get; set; }
        public string updateid { get; set; }
        public string updatedesc { get; set; }
        public string updatedon { get; set; }
        public string ivccodeversion { get; set; }
        public string testsetname { get; set; }
        public string pack_version { get; set; }
        public string testsetid { get; set; }
        public string packname { get; set; }
        public string product { get; set; }
        public string date { get; set; }
        public string file_created { get; set; }
        public string last_modified { get; set; }
        public string last_modified_notification { get; set; }
        public string status { get; set; }
        public string time_mail { get; set; }
    }


}
