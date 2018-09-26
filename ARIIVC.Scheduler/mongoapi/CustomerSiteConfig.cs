using System;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.IdGenerators;
using NUnit.Framework;

namespace ARIIVC.Scheduler
{
    class CustomerSiteConfig
    {
        private static string mongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
        private static IMongoClient _client = new MongoClient(mongoUrl);
        private static IMongoDatabase _database = _client.GetDatabase("ivc");
        private static IMongoCollection<SiteConfig> _siteconfigs = _database.GetCollection<SiteConfig>("siteconfig");
      //  private static IMongoCollection<SiteConfig> _siteconfigs = _database.GetCollection<SiteConfig>("results");


        public static List<SiteConfig> FindFieldValues(string siteName)
        {
            var filter = Builders<SiteConfig>.Filter.Eq("name", siteName);
            var list = _siteconfigs.Find(filter).ToList();
            return list;
        }
        public static void Update(string siteName, string status, string SiteStatus, string Scheduledate)
        {

            var filter = Builders<SiteConfig>.Filter.Eq("name", siteName);
            var update = Builders<SiteConfig>.Update.Set("installationstatus", status).Set("sitestatus", SiteStatus).Set("scheduledate", Scheduledate);

            _siteconfigs.UpdateOne(filter, update);
        }

        
    }

    [BsonIgnoreExtraElements]
    public class SiteConfig
    {

        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        public ObjectId _id { get; set; }

        [BsonElement("installationstatus")]
        public string installed { get; set; }
        [BsonElement("sitelabel")]
        public string SiteLabel { get; set; }
        [BsonElement("name")]
        public string Name { get; set; }
        [BsonElement("sitestatus")]
        public string SiteStatus { get; set; }
        [BsonElement("scheduledate")]
        public string Scheduledate { get; set; }

    }



}
