using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using System.Collections.Generic;

namespace ARIIVC.Utilities.JsonRepo
{
    public class Workflow
    {
        //[BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public ObjectId _id { get; set; }

        [BsonElement("version")]
        public string Version;

        [BsonElement("workflows")]
        public List<string> Workflows;

        [BsonElement("function")]
        public string FunctionName;
    }
}
