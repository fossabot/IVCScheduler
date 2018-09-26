using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{    
    [BsonIgnoreExtraElements]
    public class RingReleaseInfo
    {
        [BsonElement("updateid")]
        public string updateid { get; set; }

        [BsonElement("packname")]
        public string packname { get; set; }

        [BsonElement("product")]
        public string product { get; set; }

    }
}
