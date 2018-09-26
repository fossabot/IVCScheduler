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
    public class AppServers
    {
        private IMongoCollection<ivc_appserver> _collection;

        public AppServers(IMongoDatabase _database, string collectionName)
        {
            _collection = _database.GetCollection<ivc_appserver>("appservers");
        }

        public List<ivc_appserver> GetAllServers(string pack)
        {
            var query = Builders<ivc_appserver>.Filter.Where(t => t.packname == pack);
            List<ivc_appserver> resultList = _collection.FindSync(query).ToList();

            return resultList;
        }

        public ivc_appserver GetServerDetails(string hostname)
        {
            var query = Builders<ivc_appserver>.Filter.Where(t => t.hostname == hostname && t.active == true);
            List<ivc_appserver> resultList = _collection.FindSync(query).ToList();

            if (resultList.Count > 0)
                return resultList[0];
            else
                return null;
        }
    }
}
