using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ARIIVC.Utilities.JsonRepo
{
    public class testprofile
    {
        [JsonProperty("hashes")]
        public List<profiledata> commits { get; set; }

        [JsonProperty("tests")]
        public List<string> automatedtests { get; set; }
        
        [JsonProperty("testsetname")]
        public string Testsetname { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("header")]
        public string Header { get; set; }

        [JsonProperty("teststatus")]
        public string Teststatus { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    public class profiledata
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("hash")]
        public string OtherHash { get; set; }

        [JsonProperty("issue")]
        public string Issue { get; set; }

        [JsonProperty("_id")]
        public string ID { get; set; }

        [JsonProperty("files")]
        public List<SourceFile> files { get; set; }

        [JsonProperty("teststatus")]
        public string TestStatus { get; set; }


    }

    [BsonIgnoreExtraElements]
    public partial class ProfileData
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnore]
        public ObjectId _id { get; set; }
        [BsonElement("issue")]
        public string Issue { get; set; }

        [BsonElement("hash")]
        public string Hash { get; set; }

        [BsonElement("feature")]
        public string Feature { get; set; }

        [BsonElement("files")]
        public List<ApplicationFile> Files { get; set; }

        [BsonElement("author")]
        public string Author { get; set; }

        [BsonElement("date")]
        public string Date { get; set; }

        [BsonElement("tests")]
        public List<string> Tests { get; set; }

        [BsonElement("testsetname")]
        public string TestSetName { get; set; }

        [BsonElement("branch")]
        public string branch { get; set; }
    }

    public class ApplicationFile
    {
        [BsonElement("file")]
        public string filename { get; set; }

        [BsonElement("functions")]
        public List<string> Functions { get; set; }
    }

    public class AutomatedTest
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
    public class SourceFile
    {
        [JsonProperty("functions")]
        public List<string> Functions{ get; set; }

        [JsonProperty("impactedfunctions")]
        public List<ImpactedFunction> ImpactedFunctions { get; set; }


        [JsonProperty("file")]
        public string File { get; set; }
    }

    public class ImpactedFunction
    {
        [JsonProperty("function")]
        public string FunctionName;
        [JsonProperty("workflows")]
        public List<string> Workflows;
    }
    public class ProfileParameters
    {        
        public string version { get; set; }
        public string mongoUrl { get; set; }        
        public string mountPoint { get; set; }        
        public string user { get; set; }
        public string password { get; set; }
        public string pack { get; set; }

    }


}
