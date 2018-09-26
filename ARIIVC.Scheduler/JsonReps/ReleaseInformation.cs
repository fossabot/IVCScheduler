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
    public class releaseinformation
    {
        [BsonElement("systemversion")]
        public string systemversion { get; set; }

        [BsonElement("alternateid")]
        public string alternateid { get; set; }

        [BsonElement("updateid")]
        public string updateid { get; set; }

        [BsonElement("updatedesc")]
        public string updatedesc { get; set; }

        [BsonElement("updatedon")]
        public string updatedon { get; set; }

        [BsonElement("updatedby")]
        public string updatedby { get; set; }

        [BsonElement("ivccodeversion")]
        public string ivccodeversion { get; set; }

        [BsonElement("testsetname")]
        public string testsetname { get; set; }

        [BsonElement("pack_version")]
        public string pack_version { get; set; }

        [BsonElement("testsetid")]
        public string testsetid { get; set; }

        [BsonElement("packname")]
        public string packname { get; set; }

        [BsonElement("product")]
        public string product { get; set; }

        [BsonElement("date")]
        public string date { get; set; }

    }

}
