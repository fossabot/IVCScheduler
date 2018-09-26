using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ARIIVC.Utilities.JsonRepo
{
    [BsonIgnoreExtraElements]
    public class LatestRun
    {
        [BsonElement("version")]
        public string Version { get; set; }

        [BsonElement("workflow")]
        public string WorkflowName { get; set; }

        [BsonElement("lastexecuted")]
        public string LastExecuted { get; set; }
    }
}
