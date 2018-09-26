using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;

namespace ARIIVC.Scheduler.JsonReps
{
    [BsonIgnoreExtraElements]
    public class ivc_pack_details
    {
        [BsonElement("systemversion")]
        public string systemversion { get; set; }

        [BsonElement("updateid")]
        public string updateid { get; set; }

        [BsonElement("updatedesc")]
        public string updatedesc { get; set; }

        [BsonElement("updatedon")]
        public string updatedon { get; set; }

        [BsonElement("ivccodeversion")]
        public string ivccodeversion { get; set; }

        [BsonElement("testsetname")]
        public string testsetname { get; set; }

        [BsonElement("pack_version")]
        public string pack_version { get; set; }

        [BsonElement("packname")]
        public string packname { get; set; }

        [BsonElement("product")]
        public string product { get; set; }

        [BsonElement("date")]
        public string date { get; set; }

        [BsonElement("file_created")]
        public string file_created { get; set; }

        [BsonElement("last_modified")]
        public string last_modified { get; set; }

        [BsonElement("last_modified_notification")]
        public string last_modified_notification { get; set; }

        [BsonElement("time_mail")]
        public string time_mail { get; set; }

        [BsonElement("user_mail")]
        public string user_mail { get; set; }


        [BsonElement("ringurl")]
        public string RingUrl { get; set; }
    }
}
