using System;
using System.Collections.Generic;
using MongoDB.Driver;
using System.Linq;
using MongoDB.Bson;
using Newtonsoft.Json;
using ARIIVC.Utilities.JsonRepo;

namespace ARIIVC.Utilities
{
    public class MongoConnector
    {
        private static IMongoClient _client;
        private static IMongoDatabase _database;   
        private static IMongoCollection<ivc_appserver> _ivcappserver;
        private static IMongoCollection<RingMasterServer> _ringMasterInfo;
        private static IMongoCollection<RingMasterCommits> _ringmastercommits;
        public MongoConnector(string database,string mongoUrl=null)
        {
            if (string.IsNullOrEmpty(mongoUrl))
                _client = _client ?? new MongoClient("mongodb://c05drddrv969.dslab.ad.adp.com:27017");
            else
                _client = _client ?? new MongoClient(mongoUrl);
            _database = _database ?? _client.GetDatabase(database);
            _ivcappserver = _ivcappserver ?? _database.GetCollection<ivc_appserver>("appservers");
            _ringMasterInfo= _ringMasterInfo ?? _database.GetCollection<RingMasterServer>("ringmaster_server");
            _ringmastercommits = _ringmastercommits ?? _database.GetCollection<RingMasterCommits>("ringmaster_commits");
            
        }
        public void UpdateAppServers(List<IVCHostDeployementProgress> appservers)
        {
            Console.WriteLine("*************Active slaves are: ");
            foreach (var slave in appservers)
            {
                Console.WriteLine(slave);
                string param = "{$set:{'active':true }}";
                string filter = "{'service': {$regex : '" + slave.service + "'}}";
                BsonDocument filterdoc = BsonDocument.Parse(filter);
                BsonDocument document = BsonDocument.Parse(param);
                _ivcappserver.UpdateOne(filterdoc, document);
            }

            //var appstoupdate = Builders<ivc_appserver>.Filter.Where(t => appservers.Any(t2 => t2.service == t.service));
            //var updateDefinition = Builders<ivc_appserver>.Update.AddToSet("active", true);
            //_ivcappserver.UpdateMany(appstoupdate, updateDefinition);
        }
        public string getRelaseQaAPI(string pack)
        {            
            List<RingMasterServer> ringmasterdetails = _ringMasterInfo.Find(t => t.packname == pack).ToList();           
            return ringmasterdetails[0].serverurl;
        }
        public void updateIssueDetails(string _updateid,List<string> _issues)
        {
            _ringmastercommits.InsertOne(new RingMasterCommits
            {
                updateId = _updateid,
                issues = _issues
            });
        }
    }
}
