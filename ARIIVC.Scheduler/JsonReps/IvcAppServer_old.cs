using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ARIIVC.Scheduler.JsonReps
{
    [BsonIgnoreExtraElements]
    public class ivc_appserver_old
    {
        [BsonElement("product")]
        public string product { get; set; }

        [BsonElement("packname")]
        public string packname { get; set; }

        [BsonElement("hostname")]
        public string hostname { get; set; }

        [BsonElement("portno")]
        public string portno { get; set; }

        [BsonElement("service")]
        public string service { get; set; }

        [BsonElement("kpath")]
        public string kpath { get; set; }

        [BsonElement("active")]
        public bool active { get; set; }

        [BsonElement("serverurl")]
        public string serverurl { get; set; }

        [BsonElement("runconfig")]
        public string runconfig { get; set; }

        [BsonElement("module")]
        public List<string> module { get; set; }
    }

}
