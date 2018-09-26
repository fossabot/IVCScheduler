using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ARIIVC.Scheduler.JsonReps
{
    [BsonIgnoreExtraElements]
    public class ivc_test_result
    {
        [BsonElement("name")]
        public string name { get; set; }

        [BsonElement("summary")]
        public string summary { get; set; }

        [BsonElement("testid")]
        public string testid { get; set; }

        [BsonElement("description")]
        public string description { get; set; }

        [BsonElement("status")]
        public string status { get; set; }

        [BsonElement("testsetid")]
        public string testsetid { get; set; }

        [BsonElement("duration")]
        public string duration { get; set; }

        [BsonElement("host")]
        public string host { get; set; }

        [BsonElement("success")]
        public string success { get; set; }

        [BsonElement("counter")]
        public int counter { get; set; }

        [BsonElement("history")]
        public int history { get; set; }

        [BsonElement("avgduration")]
        public int avgduration { get; set; }

        [BsonElement("author")]
        public string author { get; set; }

        [BsonElement("created")]
        public string created { get; set; }

        [BsonElement("runner")]
        public string runner { get; set; }

        [BsonElement("F2US")]
        public string F2US { get; set; }

        [BsonElement("IVUS")]
        public string IVUS { get; set; }

        [BsonElement("module")]
        public string module { get; set; }

        [BsonElement("submodule")]
        public string submodule { get; set; }

        [BsonElement("suitename")]
        public string suitename { get; set; }

        [BsonElement("executionid")]
        public string executionid { get; set; }

        [BsonElement("testsetname")]
        public string testsetname { get; set; }

        [BsonElement("packname")]
        public string packname { get; set; }

        [BsonElement("testfixturesetupduration")]
        public string FixtureDuration { get; set; }


    }
}
