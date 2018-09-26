using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using zephyrapi;

namespace ARIIVC.Scheduler.mongoapi
{
    public class ZephyrTests
    {
        private IMongoCollection<zTest> _collection;

        public ZephyrTests(IMongoDatabase _database, string collectionName)
        {
            _collection = _database.GetCollection<zTest>(collectionName);
        }

        public void DeleteAllTests()
        {
            var recordsToDelete = Builders<zTest>.Filter.Where(t => t.fields.project.key == "IDRIVE");
            _collection.DeleteMany(recordsToDelete);
        }

        public void InsertLatestTests(List<zTest> zephyrTests)
        {
            DeleteAllTests();
            _collection.InsertMany(zephyrTests);
        }

        public List<zTest> GetRegressionTestsByModule(string codeversion, string pack, string modules, string includeRrt = "false")
        {
            FilterDefinition<zTest> jqlFilter;
            if (pack.ToLower().Equals("mt") || pack.ToLower().Equals("profiler"))
            {
                jqlFilter = Builders<zTest>.Filter.Where(t =>
                    (t.fields.teststatus.value == "Automated" || t.fields.teststatus.value == "Merged") &&
                    t.fields.ScriptID != null && t.fields.SuiteID != null && t.fields.labels.Count > 0 &&
                    t.fields.versions.Count > 0);
            }
            else
            {
                jqlFilter = Builders<zTest>.Filter.Where(t =>
                    t.fields.teststatus.value == "Automated" &&
                    t.fields.ScriptID != null && t.fields.SuiteID != null && t.fields.labels.Count > 0 &&
                    t.fields.versions.Count > 0);
            }

            var searchResults = _collection.FindSync(jqlFilter).ToList();
            List<zTest> finalResults = new List<zTest>();
            foreach (var zephyrTest in searchResults)
            {
                if (Convert.ToDouble(zephyrTest.fields.versions.First().name.Split(' ')[0].Replace("N", "")) <=
                    Convert.ToDouble(codeversion) && modules.Contains(zephyrTest.fields.components.First().name))
                {
                    finalResults.Add(zephyrTest);
                }
            }

            List<string> rrtSubmodules = new List<string>
            {
                "RRT.ReleaseTesting.Accounts",
                "RRT.ReleaseTesting.Aftersales",
                "RRT.ReleaseTesting.Environment",
                "RRT.ReleaseTesting.Vehicles"
            };

            if (includeRrt.ToLower().Equals("true"))
            {
                List<zTest> rrtSearchResult = searchResults.FindAll(t => t.fields.teststatus.value == "Merged");
                foreach (var zephyrTest in rrtSearchResult)
                {
                    if (Convert.ToDouble(zephyrTest.fields.versions.First().name.Split(' ')[0].Replace("N", "")) <=
                        Convert.ToDouble(codeversion) && modules.Contains(zephyrTest.fields.components.First().name) &&
                        string.Join(",", rrtSubmodules).Contains(zephyrTest.fields.labels.First()))
                    {
                        finalResults.Add(zephyrTest);
                    }
                }
            }
            return finalResults;
        }
    }
}
