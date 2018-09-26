using ARIIVC.Utilities.JsonRepo;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Utilities
{
    public class AcceleratorMongo
    {
        public string Version { get; set; }

        public string WorkflowName { get; set; }
        public string LastExecuted { get; set; }
        public List<string> Functions { get; set; }

        public Tracker StatusTracker { get; set; }
        public LatestRun LatestRundata { get; set; }
        private static IMongoClient _client;
        private static IMongoDatabase _database;
        public IMongoCollection<Workflow> _workflows;
        private static IMongoCollection<Workflow> _workflows_backup;
        private static IMongoCollection<Tracker> _tracker;
        public IMongoCollection<LatestRun> _lastrun;
        private static IMongoCollection<ProfileData> _profiledatas;
        private static IMongoCollection<ProfileData> _processed;
        public IMongoCollection<Workflow> _workflowstest;


        public AcceleratorMongo(string mongoUrl = null, string workflows = "workflows", string lastrun = "lastupdated")
        {
            if (string.IsNullOrEmpty(mongoUrl))
                _client = _client ?? new MongoClient("mongodb://c05drddrv969.dslab.ad.adp.com:27017");
            else
                _client = _client ?? new MongoClient(mongoUrl);

            _database = _database ?? _client.GetDatabase("accelerator");
            _workflows = _workflows ?? _database.GetCollection<Workflow>(workflows);
            _workflows_backup = _workflows_backup ?? _database.GetCollection<Workflow>("workflows_backup");
            _tracker = _tracker ?? _database.GetCollection<Tracker>("tracker");
            _lastrun = _lastrun ?? _database.GetCollection<LatestRun>(lastrun);
            _profiledatas = _profiledatas ?? _database.GetCollection<ProfileData>("profiledatas");
            _processed = _processed ?? _database.GetCollection<ProfileData>("processed");
            _workflowstest = _workflowstest ?? _database.GetCollection<Workflow>("workflows_test");
        }

        public void UpdateTracker()
        {
            UpdateOptions options = new UpdateOptions { IsUpsert = true };

            var statusTrackerToUpdate = Builders<Tracker>.Filter.Where(
                t => t.WorkflowName == WorkflowName && t.Version == Version);
            var statusTrackerUpdated = Builders<Tracker>.Update.Set("status", StatusTracker.Status);
            statusTrackerUpdated.Set("error", StatusTracker.Error);
            _tracker.UpdateOne(statusTrackerToUpdate, statusTrackerUpdated, options);

        }


        public List<Tracker> GetWorkflowsWithError()
        {
            var errorWorkflows = _tracker.Find(t => t.Error != null).ToList();
            return errorWorkflows;
        }

        public List<Tracker> GetUnprocessedWorkflows()
        {
            var unprocessedWorkflows = _tracker.Find(t =>
                t.Status == "notprocessed" &&
                (t.Error == null || t.Error == string.Empty) &&
                t.Version == Version).ToList();
            return unprocessedWorkflows;
        }

        public void UpdateWorkflows_Longer()
        {
            var updateDefinition = Builders<Workflow>.Update.AddToSetEach(p => p.Workflows, new List<string> { WorkflowName });
            foreach (var function in Functions)
            {
                var workflowsToUpdate = Builders<Workflow>.Filter.Where(t => t.FunctionName == function && t.Version == Version);
                var functionRecords = _workflows.Find(t => t.FunctionName.Equals(function) && t.Version == Version).ToList();

                if (functionRecords == null || functionRecords.Count == 0)
                {
                    Console.WriteLine("Mongo Record NOT FOUND for {0} ({1}) adding new one", function, Version);
                    Workflow addNewOne = new Workflow();
                    addNewOne.FunctionName = function;
                    addNewOne.Version = Version;
                    addNewOne.Workflows = new List<string> { WorkflowName };
                    _workflows.InsertOne(addNewOne);
                }
                else
                {
                    Console.WriteLine("Mongo Record FOUND for {0} ({1} : {2}) updating it", function, Version, functionRecords.Count);
                    foreach (Workflow tmp in functionRecords)
                    {
                        _workflows.UpdateOne(workflowsToUpdate, updateDefinition);
                    }
                }
            }
        }

        public void UpdateWorkflows()
        {

            Console.WriteLine(DateTime.Now.ToShortTimeString());
            List<Workflow> allFunctionsForVersion = _workflows.Find(t => t.Version == Version).ToList();
            List<Workflow> FunctionsWithWorkflow = allFunctionsForVersion.FindAll(t => t.Workflows.Contains(WorkflowName));
            List<string> allFunctions = allFunctionsForVersion.Select(o => o.FunctionName).ToList();
            List<string> recordsWithTest = FunctionsWithWorkflow.Select(o => o.FunctionName).ToList();
            Console.WriteLine(DateTime.Now.ToShortTimeString());

            Console.WriteLine("Total Version records : {0}", allFunctions.Count);
            Console.WriteLine("Total Version records that contains this test : {0}", FunctionsWithWorkflow.Count);
            Console.WriteLine("Total records from the latest file : {0}", Functions.Count);


            var recordsToAddFunction = Functions.FindAll(t => !allFunctions.Contains(t));
            var recordsToUpdateWorkflow = Functions.Except(recordsWithTest).ToList();
            var recordsToUpdateWorkflow1 = Functions.FindAll(t => !recordsWithTest.Contains(t));
            var recordsToDeleteWorkflow = recordsWithTest.FindAll(t => !Functions.Contains(t));
            var recordsThatAlreadyHaveLeftAlone = Functions.FindAll(t => recordsWithTest.Contains(t));

            Console.WriteLine("Functions that are not there, need to Add new Record : {0} : {1}", WorkflowName, recordsToAddFunction.Count);
            Console.WriteLine("Functions that will be added thhis workflow          : {0} : {1} : {2}", WorkflowName, recordsToUpdateWorkflow.Count, recordsToUpdateWorkflow1.Count);
            Console.WriteLine("Functions that will be have workflow removed         : {0} : {1}", WorkflowName, recordsToDeleteWorkflow.Count);
            Console.WriteLine("Functions that doesn't need updation                 : {0} : {1}", WorkflowName, recordsThatAlreadyHaveLeftAlone.Count);

            List<Workflow> listToAdd = new List<Workflow>();



            //This function name is not there, so add it
            foreach (string func1 in recordsToAddFunction)
            {

                Workflow addNewOne = new Workflow();
                addNewOne.FunctionName = func1;
                addNewOne.Version = Version;
                List<string> tests = new List<string>();
                tests.Add(WorkflowName);
                addNewOne.Workflows = tests;
                listToAdd.Add(addNewOne);

            }

            #region Section that adds new functions identified for this test
            if (listToAdd.Count > 0)
            {
                Console.WriteLine("Mongo Record NOT FOUND for {0} ({1})records. Inserted them now for {2}", listToAdd.Count, Version, WorkflowName);
                foreach (string tmp in recordsToAddFunction)
                {
                    Console.WriteLine("Added Function : {0}", tmp);
                }
                _workflows.InsertMany(listToAdd);
            }
            #endregion

            #region Section that updates functions identified for this test
            var workflowsToUpdate1 = Builders<Workflow>.Filter.Where(t => Functions.Contains(t.FunctionName) && t.Version == Version);
            var updateDefinition = Builders<Workflow>.Update.AddToSet("workflows", WorkflowName);
            _workflows.UpdateMany(workflowsToUpdate1, updateDefinition);
            Console.WriteLine("Mongo FOUND for {0} ({1})records. Updated them now for {2}", recordsToUpdateWorkflow.Count, Version, WorkflowName);
            foreach (string tmp in recordsToUpdateWorkflow)
            {
                Console.WriteLine("Updated Function : {0}", tmp);
            }
            #endregion

            #region Section that removes functions identified for this test
            var workflowsToRemove = Builders<Workflow>.Filter.Where(t => (!Functions.Contains(t.FunctionName)) && t.Version == Version);
            var updateRemoveDefinition = Builders<Workflow>.Update.Pull("workflows", WorkflowName);
            _workflows.UpdateMany(workflowsToRemove, updateRemoveDefinition);
            Console.WriteLine("Mongo FOUND for {0} ({1})records. Removed test from them them now {2}", recordsToDeleteWorkflow.Count, Version, WorkflowName);
            foreach (string tmp in recordsToDeleteWorkflow)
            {
                Console.WriteLine("Updated Function : {0}", tmp);
            }
            #endregion
        }

        public List<string> GetImpactedTestsForFunction(string functionName)
        {
            List<string> impactedTests = new List<string>();
            var tests = _workflows.Find(t => t.FunctionName.Contains(functionName) && t.Version == Version).ToList();
            tests.ForEach(t => impactedTests.AddRange(t.Workflows));
            System.Console.WriteLine("Impacted : {0} -- {1}", functionName, string.Join(",", impactedTests));
            return impactedTests;
        }
        //public void UpdateLatestRun()
        //{
        //    // Delete existing records for that Testcase
        //    var testcasestoDelete = Builders<LatestRun>.Filter.Where(t => t.WorkflowName == WorkflowName && t.Version == Version);
        //    if (testcasestoDelete != null)
        //    {
        //        _lastrun.DeleteOne(testcasestoDelete);
        //    }
        //    LatestRun testcasestoAdd = new LatestRun();
        //    _lastrun.InsertOne(new LatestRun
        //    {
        //        Version = Version,
        //        WorkflowName = WorkflowName,
        //        LastExecuted = LastExecuted
        //    });
        //}

        public void UpdateLatestRun(string version, string workFlowName)
        {

            string timestamp = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
            UpdateOptions updateOptions = new UpdateOptions { IsUpsert = true };
            var updateDefinition = Builders<LatestRun>.Update.Set("lastexecuted", timestamp);
            var filterDefinition = Builders<LatestRun>.Filter.Where(t => t.Version.ToLower() == version.ToLower() && t.WorkflowName.ToLower().Contains(workFlowName.ToLower()));

            try
            {
                _lastrun.UpdateOne(filterDefinition, updateDefinition, updateOptions);
            }
            catch (Exception excp)
            {
                Console.WriteLine("Exception occurred in UpdateLatestRun", excp);
            }
        }

        public void UpdateLatestRun()
        {
            // Delete existing records for that Testcase
            var testcasestoDelete = Builders<LatestRun>.Filter.Where(t => t.WorkflowName == WorkflowName && t.Version == Version);
            if (testcasestoDelete != null)
            {
                _lastrun.DeleteOne(testcasestoDelete);
            }

            _lastrun.InsertOne(new LatestRun
            {
                Version = Version,
                WorkflowName = WorkflowName,
                LastExecuted = LastExecuted
            });
        }

        public List<ProfileData> getProfilerDataForMaster(bool removeDuplicates = true)
        {
            List<ProfileData> allprofileDatas = _profiledatas.Find(t => t.Hash != null && t.branch == "master").ToList();

            if (removeDuplicates)
            {
                List<ProfileData> profileDatas = new List<ProfileData>();
                List<string> processedCommits = new List<string>();

                foreach (ProfileData tmp in allprofileDatas)
                {
                    if (!processedCommits.Contains(tmp.Hash))
                    {
                        processedCommits.Add(tmp.Hash);
                        profileDatas.Add(tmp);
                    }
                }

                return profileDatas;
            }

            return allprofileDatas;
        }

        public List<ProfileData> getProfilerDataForRR(string rrid, bool removeDuplicates = true)
        {
            List<ProfileData> allprofileDatas = _profiledatas.Find(t => t.Hash != null && t.branch == rrid).ToList();
            if (removeDuplicates)
            {
                List<ProfileData> profileDatas = new List<ProfileData>();
                List<string> processedCommits = new List<string>();

                foreach (ProfileData tmp in allprofileDatas)
                {
                    if (!processedCommits.Contains(tmp.Hash))
                    {
                        processedCommits.Add(tmp.Hash);
                        profileDatas.Add(tmp);
                    }
                }

                return profileDatas;
            }

            return allprofileDatas;
        }

        public List<ProfileData> getProfilerDataForPreprod(bool removeDuplicates = true)
        {
            List<ProfileData> allprofileDatas = _profiledatas.Find(t => t.Hash != null && t.branch == "preprod").ToList();

            if (removeDuplicates)
            {
                List<ProfileData> profileDatas = new List<ProfileData>();
                List<string> processedCommits = new List<string>();

                foreach (ProfileData tmp in allprofileDatas)
                {
                    if (!processedCommits.Contains(tmp.Hash))
                    {
                        processedCommits.Add(tmp.Hash);
                        profileDatas.Add(tmp);
                    }
                }

                return profileDatas;
            }

            return allprofileDatas;
        }

        public List<ProfileData> MoveToProcessed(string testSetName, string branch)
        {
            var profileDataToDelete = Builders<ProfileData>.Filter.Where(
              t => t.Hash != null && t.branch == branch);

            List<ProfileData> allprofileDatas = _profiledatas.Find(t => t.Hash != null && t.branch == branch).ToList();
            List<ProfileData> profileDatas = new List<ProfileData>();

            foreach (ProfileData tmp in allprofileDatas)
            {
                tmp.TestSetName = testSetName;
                profileDatas.Add(tmp);
            }

            _processed.InsertMany(profileDatas);
            _profiledatas.DeleteMany(profileDataToDelete);

            return profileDatas;

        }

        public List<LatestRun> getLastRunStatus(string version)
        {
            return _lastrun.Find(t => t.Version == version).ToList();
        }
        public void AddLastRuns(List<LatestRun> runs)
        {
            _lastrun.InsertMany(runs);
        }

        public List<Workflow> GetWorkflowsForVersion(string version)
        {
            return _workflows.Find(t => t.Version == version).ToList();
        }

        public void InsertNewWorkFlow(Workflow newOne)
        {
            _workflows.InsertOne(newOne);
        }

        public void DeleteManyLastRun(FilterDefinition<LatestRun> removeDefinition)
        {
            _lastrun.DeleteMany(removeDefinition);
        }

        public void InsertManyLastRun(List<LatestRun> listToAdd)
        {
            _lastrun.InsertMany(listToAdd);
        }

        public void WorkFlowsChangeVersionCodeCut()
        {
            //Take backup, delete and insert all from workflows
            var allFilter = Builders<Workflow>.Filter.Empty;
            List<Workflow> AllRecords = _workflows.Find(allFilter).ToList();
            _workflows_backup.DeleteMany(allFilter);
            _workflows_backup.InsertMany(AllRecords);

            //Now start the transition process
            var liveFilter = Builders<Workflow>.Filter.Where(t => t.Version.ToLower() == "live");
            var pilotFiler = Builders<Workflow>.Filter.Where(t => t.Version.ToLower() == "pilot");
            var mtFilter = Builders<Workflow>.Filter.Where(t => t.Version.ToLower() == "mt");

            var liveUpdateDef = Builders<Workflow>.Update.Set("version", "Live");
            //delete the live version
            DeleteResult delete = _workflows.DeleteMany(liveFilter);

            //update the pilot records to Live
            UpdateResult update = _workflows.UpdateMany(pilotFiler, liveUpdateDef);

            //Delete the pilot records
            var pilotDelete = _workflows.DeleteMany(pilotFiler);

            List<Workflow> allMT = AllRecords.FindAll(t => t.Version.ToLower() == "mt");
            List<Workflow> toBeCreated = new List<Workflow>();

            foreach (Workflow tmp in allMT)
            {
                Workflow addNew = new Workflow()
                {
                    Workflows = tmp.Workflows,
                    Version = "Pilot",
                    FunctionName = tmp.FunctionName
                };

                toBeCreated.Add(addNew);
            }

            _workflows.InsertMany(toBeCreated);


            Console.WriteLine("No of records deleted for LIVE : {0}", delete.DeletedCount);
            Console.WriteLine("No of records matched and updated to LIVE : {0} : {1}", update.MatchedCount, update.ModifiedCount);
            Console.WriteLine("No of records deleted for Pilot : {0}", pilotDelete.DeletedCount);
            Console.WriteLine("No of records inserted for Pilot from MT : {0}", allMT.Count);
        }
        public void AddBuildNumber(List<String> hash, string rpmversion)
        {
            Console.WriteLine("*************Deployed Commits are: ");
            foreach (var commitid in hash)
            {
                Console.WriteLine(hash);
                string param = "{$set:{'rpmversion':'" + rpmversion + "'}}";
                string filter = "{'hash': {$regex : '" + commitid + "'}}";
                BsonDocument filterdoc = BsonDocument.Parse(filter);
                BsonDocument document = BsonDocument.Parse(param);
                _profiledatas.UpdateOne(filterdoc, document);
            }

        }
        public void UpdateBuildVersion(int versionBuildFrom, int versionBuildTo)
        {
            List<string> commit_ids = new List<string>();// CSDTConnector.CSDTConnector.CommitList(versionBuildFrom, versionBuildTo);
            List<string> allCommits = new List<string>();
            AcceleratorMongo acm = new AcceleratorMongo();
            List<ProfileData> profileDatas = acm.getProfilerDataForMaster();
            profileDatas.RemoveAll(tt => tt.Tests.Count == 0);
            foreach (ProfileData tmp in profileDatas)
            {
                allCommits.Add(tmp.Hash.Substring(0, 7));
            }
            var deployedcommits = allCommits.Intersect(commit_ids).ToList();
            if (deployedcommits.Count == 0)
            {
                Console.WriteLine("**********Zero Commits deployed*********");
            }
            else
            {
                AddBuildNumber(deployedcommits, versionBuildTo.ToString());
            }
        }
    }
}

