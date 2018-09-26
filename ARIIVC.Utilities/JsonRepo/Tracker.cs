using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace ARIIVC.Utilities.JsonRepo
{
    public class Tracker
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public ObjectId _id { get; set; }

        [BsonElement("version")]
        public string Version;

        [BsonElement("workflow")]
        public string WorkflowName;

        [BsonElement("acceleratorfiles")]
        public List<string> AcceleratorFiles;

        [BsonElement("status")]
        public string Status;

        [BsonElement("error")]
        public string Error;

    }
}
