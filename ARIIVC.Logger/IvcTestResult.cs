using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ARIIVC.Logger
{
    [BsonIgnoreExtraElements]
    public class IvcTestResult
    {

        [BsonElement("name")] public string Name { get; set; }

        [BsonElement("summary")] public string Summary { get; set; }

        [BsonElement("testid")] public string Testid { get; set; }

        [BsonElement("description")] public string Description { get; set; }

        [BsonElement("status")] public string Status { get; set; }

        [BsonElement("testsetid")] public string Testsetid { get; set; }

        [BsonElement("duration")] public string Duration { get; set; }

        [BsonElement("host")] public string Host { get; set; }

        [BsonElement("counter")] public Int16 Counter { get; set; }

        [BsonElement("success")] public string Success { get; set; }

        [BsonElement("author")] public string Author { get; set; }

        [BsonElement("created")] public string Created { get; set; }

        [BsonElement("runner")] public string Runner { get; set; }

        [BsonElement("F2US")] public string F2Us { get; set; }

        [BsonElement("IVUS")] public string Ivus { get; set; }

        [BsonElement("module")] public string Module { get; set; }

        [BsonElement("submodule")] public string Submodule { get; set; }

        [BsonElement("suitename")] public string Suitename { get; set; }

        [BsonElement("executionid")] public string Executionid { get; set; }

        [BsonElement("testsetname")] public string Testsetname { get; set; }

        [BsonElement("packname")] public string Packname { get; set; }

        [BsonElement("teststarttime")] public string TestStartTime { get; set; }

        [BsonElement("testendtime")] public string TestEndtime { get; set; }
    }
}
