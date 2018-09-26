using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Scheduler.JsonReps;
using MongoDB.Driver;

namespace ARIIVC.Scheduler
{
    public class AssociatedRuns
    {
        private IMongoCollection<ivc_assoc_runs> _collection;

        public AssociatedRuns(IMongoDatabase database, string collectionname)
        {
            _collection = database.GetCollection<ivc_assoc_runs>(collectionname);
        }

        public HashSet<string> GetTestSetNamesFilteredByDate(string pack, DateTime tillDate)
        {
            Console.WriteLine("Getting TestSetNames for pack : " + pack + " till date : " + tillDate);
            var query = Builders<ivc_assoc_runs>.Filter.Where(t => t.packname.Equals(pack));
            List<ivc_assoc_runs> AssociatedRunsDetails = _collection.FindSync(query).ToList();
            HashSet<string> filteredTestSetNames = new HashSet<string>();
            foreach (var singleDetail in AssociatedRunsDetails)
            {
                string dateString = singleDetail.date;

                DateTime testSetDate = new DateTime(Convert.ToInt32(dateString.Split('-')[2]),
                    Convert.ToInt32(dateString.Split('-')[1]), Convert.ToInt32(dateString.Split('-')[0]));

                if (testSetDate.CompareTo(tillDate) <= 0)
                {
                    filteredTestSetNames.Add(singleDetail.testsetname);
                    Console.WriteLine("Adding TestSetName to delete tests : " + singleDetail.testsetname);
                }
            }
            return filteredTestSetNames;
        }

        public void DeleteAssociatedRuns(string pack, HashSet<string> testSetNames)
        {
            var query = Builders<ivc_assoc_runs>.Filter.Where(t => testSetNames.Contains(t.testsetname));
            _collection.DeleteMany(query);
            Console.WriteLine("Deleting " + testSetNames.Count + " Associated Runs for " + pack + " in assocruns table");

        }

        public ivc_assoc_runs GetAssociateRun(string testsetname)
        {
            var query = Builders<ivc_assoc_runs>.Filter.Where(t => t.testsetname.Equals(testsetname));
            List<ivc_assoc_runs> AssociatedRunsDetails = _collection.Find(query).ToList();
            var updateDefinition = Builders<ivc_assoc_runs>.Update.SetOnInsert("status", "processed");

            if (AssociatedRunsDetails.Count > 1)
            {
                return null;
            }
            else
            {
                return AssociatedRunsDetails[0];                
            }

        }
    }
}
