using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Scheduler.JsonReps;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ARIIVC.Scheduler
{
    public class ProductFeatureData
    {
        private static string mongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
        private static IMongoClient _client = new MongoClient(mongoUrl);
        private static IMongoDatabase _database = _client.GetDatabase("ivc");
        private static IMongoCollection<FeatureInfo> _product_feature_data = _database.GetCollection<FeatureInfo>("product_feature_data");

        public void DeleteAllRecords(string pack)
        {
            var recordsToDelete = Builders<FeatureInfo>.Filter.Where(t => (t.Tab == "All" || t.Tab == "Pilot") && t.Pack == pack);
            _product_feature_data.DeleteMany(recordsToDelete);
        }

        public void InsertFeatureInfo(ProductFeatureTab productFeatureData, string pack)
        {
            DeleteAllRecords(pack);
            List<ProductFeature> allTabProductFeatures = productFeatureData.All;
            List<ProductFeature> pilotTabProductFeatures = productFeatureData.Pilot;

            List<FeatureInfo> featuresToAdd = new List<FeatureInfo>();
            foreach (ProductFeature productFeature in allTabProductFeatures)
            {
                featuresToAdd.Add(new FeatureInfo
                {
                    ProductDescription = productFeature.ProductDescription,
                    DriveModule = productFeature.DriveModule,
                    Tab = "All",
                    Enabled = productFeature.Enabled,
                    Pack = pack
                });    
            }
            foreach (ProductFeature productFeature in pilotTabProductFeatures)
            {
                featuresToAdd.Add(new FeatureInfo()
                {
                    ProductDescription = productFeature.ProductDescription,
                    DriveModule = productFeature.DriveModule,
                    Tab = "Pilot",
                    Enabled = productFeature.Enabled,
                    Pack = pack
                });
            }
            _product_feature_data.InsertMany(featuresToAdd);
        }

        public Dictionary<string, List<FeatureInfo>> GetFeatureInfo(string pack)
        {
            var recordsToFind = Builders<FeatureInfo>.Filter.Where(t => (t.Tab == "All" || t.Tab == "Pilot") && t.Pack == pack);

            List<FeatureInfo> productFeatures = _product_feature_data.FindSync(recordsToFind).ToList();

            Dictionary<string, List<FeatureInfo>> featureInfo =
                productFeatures.GroupBy(t => t.Tab).ToDictionary(tt => tt.Key, tt => tt.ToList());

            return featureInfo;
        }
    }
}
