using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace ARIIVC.Utilities.JsonRepo
{    
    public class ReleaseQA
    {      
        [JsonProperty("QAREF")]
        public string qaref { get; set; }
        [JsonProperty("ISSUEDETAILS")]
        public string issuedetails { get; set; }
        [JsonProperty("UpdateID")]
        public string updateId { get; set; }
    }

}
