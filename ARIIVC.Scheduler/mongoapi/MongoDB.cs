using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Scheduler.JsonReps;
using ARIIVC.Scheduler.mongoapi;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace ARIIVC.Scheduler
{
    public class MongoDriver
    {
        private string _mongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
        private IMongoClient _client;
        private IMongoDatabase _database;

        public Results Results;
        public AppServers Servers;
        public Releases Releases;
        public AssociatedRuns AssociatedRuns;
        public TriggerInformation TriggerInfo;
        public ZephyrTests zephyrTests;

        public MongoDriver()
        {
            _client = new MongoClient(_mongoUrl);
            _database = _client.GetDatabase("ivc");
            Results = new Results(_database, "results");
            Servers = new AppServers(_database, "appservers");
            Releases = new Releases(_database, "releases");
            AssociatedRuns = new AssociatedRuns(_database, "assocruns");
            zephyrTests = new ZephyrTests(_database, "qctests");
            TriggerInfo = new TriggerInformation(_database, "trigger_information");
        }
        public MongoDriver(string mongoUrl)
        {
            _mongoUrl = mongoUrl;
            _client = new MongoClient(_mongoUrl);
            _database = _client.GetDatabase("ivc");
            Results = new Results(_database, "results");
            Servers = new AppServers(_database, "appservers");
            Releases = new Releases(_database, "releases");
            AssociatedRuns = new AssociatedRuns(_database, "assocruns");
            zephyrTests = new ZephyrTests(_database, "qctests");
        }

        
    }
}
