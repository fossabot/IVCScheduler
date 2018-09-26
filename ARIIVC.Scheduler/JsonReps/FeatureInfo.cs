using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace ARIIVC.Scheduler
{
    public class FeatureInfo
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("productdescription")]
        [JsonProperty("productdescription")]
        public string ProductDescription { get; set; }

        [BsonElement("drivemodule")]
        [JsonProperty("drivemodule")]
        public string DriveModule { get; set; }

        [BsonElement("tab")]
        [JsonProperty("tab")]
        public string Tab { get; set; }

        [BsonElement("enabled")]
        [JsonProperty("enabled")]
        public string Enabled { get; set; }

        [BsonElement("pack")]
        [JsonProperty("pack")]
        public string Pack { get; set; }
    }
}
