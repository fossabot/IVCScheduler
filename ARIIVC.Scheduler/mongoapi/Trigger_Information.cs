using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Scheduler.JsonReps;
using ARIIVC.Utilities.JsonRepo;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace ARIIVC.Scheduler
{
    public class TriggerInformation
    {
        private IMongoCollection<ivc_trigger_info> _collection;

        public TriggerInformation(IMongoDatabase _database, string collectionName)
        {
            _collection = _database.GetCollection<ivc_trigger_info>("trigger_information");
        }

        public void AddRecord(ivc_trigger_info record)
        {
            _collection.InsertOne(record);
        }
    }
}
