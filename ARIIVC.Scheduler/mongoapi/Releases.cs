using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Scheduler.JsonReps;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MongoDB.Driver.Core.Operations;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace ARIIVC.Scheduler
{
    public class Releases
    {
        private IMongoCollection<ivc_pack_details> _collection;

        public Releases(IMongoDatabase _database, string collectionname)
        {
            _collection = _database.GetCollection<ivc_pack_details>(collectionname);
        }

        public ivc_pack_details GetPackDetails(string testSetName)
        {
            var query = Builders<ivc_pack_details>.Filter.Where(t => t.testsetname == testSetName);
            List<ivc_pack_details> resultList = _collection.FindSync(query).ToList();

            if (resultList.Count > 0)
                return resultList[0];
            else
                return null;
        }

        public string GetTestSetName(string product, string packName)
        {
            var query = Builders<ivc_pack_details>.Filter.Where(t => t.product.ToLower() == product.ToLower() && t.packname.ToLower() == packName.ToLower());
            List<ivc_pack_details> releaseInformation = _collection.FindSync(query).ToList();

            return releaseInformation.First().testsetname;

        }

        public ivc_pack_details GetPackInformation(string ivcpack)
        {
            var query = Builders<ivc_pack_details>.Filter.Where(t => t.packname == ivcpack);
            List<ivc_pack_details> resultList = _collection.FindSync(query).ToList();

            if (resultList.Count > 0)
                return resultList[0];
            else
                return null;
        }
    }
}
