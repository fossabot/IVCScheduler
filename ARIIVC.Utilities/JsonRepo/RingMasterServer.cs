using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ARIIVC.Utilities.JsonRepo
{
    [BsonIgnoreExtraElements]
    public class RingMasterServer
    {
        [BsonElement("product")]
        public string product { get; set; }

        [BsonElement("packname")]
        public string packname { get; set; }

        [BsonElement("version")]
        public string version { get; set; }

        [BsonElement("portno")]
        public string portno { get; set; }

        [BsonElement("service")]
        public string service { get; set; }

        [BsonElement("active")]
        public bool active { get; set; }

        [BsonElement("serverurl")]
        public string serverurl { get; set; }      
    }

}
