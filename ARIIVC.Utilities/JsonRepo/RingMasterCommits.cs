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
    public class RingMasterCommits
    {
        [BsonElement("updateId")]
        public string updateId { get; set; }

        [BsonElement("issues")]
        public List<string> issues { get; set; }
 
    }

}
