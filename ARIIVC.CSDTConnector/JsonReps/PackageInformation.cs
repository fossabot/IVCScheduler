using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ARIIVC.CSDTConnector.JsonReps
{
    [BsonIgnoreExtraElements]
    public class PackageInformation
    {
        [BsonElement("version")]
        public string version { get; set; }
    }

}
