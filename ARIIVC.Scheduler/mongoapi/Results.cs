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
    public class Results
    {
        private IMongoCollection<ivc_test_result> _collection;

        public Results(IMongoDatabase _database, string collectionname)
        {
            _collection = _database.GetCollection<ivc_test_result>(collectionname);
        }

        public List<ivc_test_result> GetAllTests(string testSetName)
        {
            var query = Builders<ivc_test_result>.Filter.Where(t => t.testsetname == testSetName);
            List<ivc_test_result> resultList = _collection.FindSync(query).ToList();

            return resultList;
        }

        public List<ivc_test_result> GetTestsBySuiteName(string suite, string testSetName)
        {
            var query = Builders<ivc_test_result>.Filter.Where(t => t.testsetname == testSetName && t.suitename == suite);
            List<ivc_test_result> resultList = _collection.FindSync(query).ToList();

            return resultList;
        }

        public List<ivc_test_result> GetTestsByTestName(string testName, string testSetName)
        {
            var query = Builders<ivc_test_result>.Filter.Where(t => t.testsetname == testSetName && t.name == testName);
            List<ivc_test_result> resultList = _collection.FindSync(query).ToList();

            return resultList;
        }

        public void RemoveTestsByTestSetName(string testSetName)
        {
            var query = Builders<ivc_test_result>.Filter.Where(t => t.testsetname.Equals(testSetName));
            _collection.DeleteMany(query);
            Console.WriteLine("Deleting all test cases for testsetname : " + testSetName);
        }

        public int FindLastSuccessDuration(string testname, string packname)
        {
            try
            {
                var testResult = _collection.Find(t => t.name.Equals(testname) && t.status.ToLower() == "passed" && t.packname == packname).ToList();
                if (testResult != null && testResult.Count > 0)
                {
                    string fixtureDuration = testResult[testResult.Count - 1].FixtureDuration == null ? "0" : testResult[testResult.Count - 1].FixtureDuration;
                    return Convert.ToInt32(testResult[testResult.Count - 1].duration) + Convert.ToInt32(fixtureDuration);
                }
                else
                {
                    return 180;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in FindLastSuccessDuration:  " + ex.Message + ex.StackTrace);
                return 180;
            }
        }       
    }
}
