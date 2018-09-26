using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Scheduler.JsonReps;
using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using zephyrapi;
using ARIIVC.Utilities;
using ARIIVC.Utilities.JsonRepo;

namespace ARIIVC.Scheduler
{

    public class ExecutionBaseData
    {
        public string testname { get; set; }
        public Int32 averageDuration { get; set; }
        public Int32 history { get; set; }

    }

    public class DashboardConnector
    {

        public static string dashboardserver = "gbhpdslivcweb01.dsi.ad.adp.com";
        public string mongoRest = "http://" + dashboardserver + "/ivc/rest/";

        public void GetExecutionSnapshot(string ivcpacks)
        {
            string[] splitArray = ivcpacks.Split(',');
            foreach (string ivcpack in splitArray)
            {
                var url = "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc/dynamic/getTestResultsOfPack.php?pack=" + ivcpack;
                RestCall call = new RestCall() {Url = url};
                string jsonoutput = call.Get();
                esRest getObj = JsonConvert.DeserializeObject<esRest>(jsonoutput);
                esPost epost = new esPost();
                epost.passed = getObj.passed;
                epost.norun = getObj.norun;
                epost.failed = getObj.failed;
                epost.blocked = getObj.blocked;
                epost.testsetname = getObj.testsetname;
                epost.timestamp = DateTime.Now.ToString("HH:mm");

                call = new RestCall()
                {
                    Url = mongoRest + "results/update_execution_trend"
                };
                call.Post(JsonConvert.SerializeObject(epost));
            }
        }

        private dynamic GetReleaseInformation(string product, string packname)
        {
            string relUrl = mongoRest + "releases/get?query={" + "\"product\":\"" +
                            product + "\",\"packname\":\"" + packname + "\"}";
            RestCall call = new RestCall() {Url = relUrl};
            string relOutput = call.Get();
            dynamic th1 = Newtonsoft.Json.Linq.JValue.Parse(relOutput);
            return th1;
        }

        public string GetLastInstalledRpmNumber(string packname)
        {
            ivc_pack_details releaseInformation = JsonConvert.DeserializeObject<ivc_pack_details>(GetReleaseInformation("Drive", packname).ToString());            
            return releaseInformation.updatedesc;
        }

        public releaseinformation GetReleaseInformationForProductAndPack(string product, string packname)
        {
            string releaseInfoURL =  mongoRest + "releases/get?query={" + "\"product\":\"" + product + "\",\"packname\":\"" + packname + "\"}";
            RestCall call = new RestCall() {Url = releaseInfoURL};
            string releaseInfoResponse = call.Get();
            releaseinformation releaseInfo = JsonConvert.DeserializeObject<releaseinformation>(releaseInfoResponse);
            return releaseInfo;
        }
        public ivc_recent_releases GetRecentReleaseInfoForProductPackRelid(string product, string packname,string relid)
        {
            string releaseInfoURL = mongoRest + "releases/recent?query={" + "\"product\":\"" + product + "\",\"packname\":\"" + packname + "\",\"relid\":\"" + relid + "\"}";
            RestCall call = new RestCall() { Url = releaseInfoURL };
            string releaseInfoResponse = call.Get();
            ivc_recent_releases releaseInfo = JsonConvert.DeserializeObject<ivc_recent_releases>(releaseInfoResponse);
            return releaseInfo;
        }

        public releaseinformation GetReleaseInformationFromTestSetName(string tstsetname)
        {
            string releaseInfoURL = mongoRest + "releases/get?query={" + "\"testsetname\":\"" + tstsetname + "\"}";
            RestCall call = new RestCall() { Url = releaseInfoURL };
            string releaseInfoResponse = call.Get();
            releaseinformation releaseInfo = JsonConvert.DeserializeObject<releaseinformation>(releaseInfoResponse);
            return releaseInfo;
        }

        public releaseinformation GetReleaseInformationFromCodeVersion(string codeversion)
        {
            string releaseInfoURL = mongoRest + "releases/get?query={" + "\"ivccodeversion\":\"" + codeversion + "\"}";
            RestCall call = new RestCall() { Url = releaseInfoURL };
            string releaseInfoResponse = call.Get();
            releaseinformation releaseInfo = JsonConvert.DeserializeObject<releaseinformation>(releaseInfoResponse);
            return releaseInfo;
        }

        public string GetPackVersion(string product, string packname)
        {
            dynamic th1 = GetReleaseInformation(product, packname);
            string version = th1["ivccodeversion"];
            return version;
        }

        public string GetPackSystemVersion(string product, string packname)
        {
            dynamic th1 = GetReleaseInformation(product, packname);
            string version = th1["systemversion"];
            return version;
        }

        public List<ExecutionBaseData> GetBaseData(string packname)
        {
            Dictionary<string, string> queryparams = new Dictionary<string, string>();
            queryparams.Add("product", "Drive");
            if (packname == "MT")
            {
                queryparams.Add("packname", "Pilot");
            }
            else
            {
                queryparams.Add("packname", packname);
            }

            string req_url = mongoRest + "releases/get?query=" +
                             JsonConvert.SerializeObject(queryparams);
            RestCall
                call = new RestCall() {Url = req_url};
            string jsonOutput = call.Get();
            ivc_pack_details targetPack = JsonConvert.DeserializeObject<ivc_pack_details>(jsonOutput);

            return GetAverageBase(targetPack.systemversion, targetPack.packname);
        }

        public List<ivc_test_result> GetBaseData_Old(string packname)
        {
            Dictionary<string, string> queryparams = new Dictionary<string, string>();
            queryparams.Add("product", "Drive");
            if (packname == "MT")
            {
                queryparams.Add("packname", "Pilot");
            }
            else
            {
                queryparams.Add("packname", packname);
            }

            string req_url = mongoRest + "releases/get?query=" +
                             JsonConvert.SerializeObject(queryparams);
            RestCall
                call = new RestCall() { Url = req_url };
            string jsonOutput = call.Get();
            ivc_pack_details targetPack = JsonConvert.DeserializeObject<ivc_pack_details>(jsonOutput);

            return GetAverageCounter_Old(targetPack.systemversion, targetPack.packname).FindAll(t => t.suitename != null);
        }

        public List<ExecutionBaseData> GetAverageBase(string sysversion, string packname)
        {
            StringBuilder output = new StringBuilder();
            List<string> lastsets = GetLastExecutions(sysversion, packname);
            List<ivc_test_result> results = new List<ivc_test_result>();
            foreach (string tmpExecutionInstance in lastsets)
            {
                Dictionary<string, string> qparams = new Dictionary<string, string>();
                qparams.Add("testsetname", tmpExecutionInstance);
                string suiteDataURL = mongoRest + "results/get?query=" +
                                      JsonConvert.SerializeObject(qparams);
                RestCall call = new RestCall() { Url = suiteDataURL };
                results.AddRange(JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get()));
            }


            int suitenameMissing = results.RemoveAll(t => string.IsNullOrEmpty(t.suitename));
            int lessDuration = results.RemoveAll(t => Convert.ToInt16(t.duration) < 2);
            int failedTests = results.RemoveAll(t => t.status.ToLower() != "passed");

            Dictionary<string, List<ivc_test_result>> testsBySuite = results.GroupBy(t => t.name).ToDictionary(tt => tt.Key, tt => tt.ToList());
            List<ExecutionBaseData> baseData = new List<ExecutionBaseData>();

            foreach (string ivctest in testsBySuite.Keys)
            {

                ExecutionBaseData singleTest = new ExecutionBaseData();
                singleTest.testname = ivctest;                
                List<string> allDurations = testsBySuite[ivctest].Select(t => t.duration).ToList();
                singleTest.averageDuration = Convert.ToInt32(allDurations.ConvertAll(p => int.Parse(p)).ToList().Average());
                singleTest.history = 0;
                baseData.Add(singleTest);
            }

            return baseData;

        }

        public List<ivc_test_result> GetAverageCounter_Old(string sysversion, string packname)
        {
            StringBuilder output = new StringBuilder();
            List<string> lastsets = GetLastExecutions(sysversion, packname);
            List<ivc_test_result> results = new List<ivc_test_result>();
            foreach (string tmpExecutionInstance in lastsets)
            {
                Dictionary<string, string> qparams = new Dictionary<string, string>();
                qparams.Add("testsetname", tmpExecutionInstance);
                string suiteDataURL = mongoRest + "results/get?query=" +
                                      JsonConvert.SerializeObject(qparams);
                RestCall call = new RestCall() { Url = suiteDataURL };
                results.AddRange(JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get()));
            }

            return results;
        }

        public List<string> GetLastExecutions(string sysversion, string ivcpack, int counter = 5)
        {
            List<string> lasttestsets = new List<string>();
            Dictionary<string, string> query = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(sysversion))
            {
                query.Add("packname", ivcpack);
            }
            else
            {
                query.Add("systemversion", sysversion);
            }
            string url = string.Format("{0}runs/get?query={1}", mongoRest, JsonConvert.SerializeObject(query));
            RestCall call = new RestCall() {Url = url};
            List<ivc_assoc_runs> associatedruns =
                JsonConvert.DeserializeObject<List<ivc_assoc_runs>>(call.Get())
                    .OrderByDescending(t => t.file_created)
                    .ToList();

            if (associatedruns.Count < counter)
            {
                string lastPilotVersion = GetPackSystemVersion("Drive", "Live");
                query = new Dictionary<string, string>();
                query.Add("systemversion", lastPilotVersion);
                url = string.Format("{0}runs/get?query={1}",mongoRest, JsonConvert.SerializeObject(query));
                call.Url = url;
                List<ivc_assoc_runs> associatedruns_added =
                    JsonConvert.DeserializeObject<List<ivc_assoc_runs>>(call.Get())
                        .OrderByDescending(t => t.file_created)
                        .ToList();
                associatedruns.AddRange(associatedruns_added.Take(counter - associatedruns.Count));
            }

            associatedruns.RemoveAll(t => t.testsetname.ToLower().Contains("releasetesting"));
            int iterator = 0;
            for (iterator = 0; iterator < counter; iterator++)
            {
                if (associatedruns.Count == iterator)
                {
                    Console.WriteLine("Last " + iterator + " executions found!!!!!!!!!!!!!!!!");
                    break;
                }
                else
                {
                    Console.WriteLine("iterator : " + iterator);
                    lasttestsets.Add(associatedruns[iterator].testsetname);
                }
            }
            return lasttestsets;
        }

        public bool GetLatestTestCaseStatus(string packname, string testcase)
        {

            string testsetname = GetLastTestSetForPack("Drive", packname);
            Dictionary<string, string> query = new Dictionary<string, string>();
            query.Add("testsetname", testsetname);
            query.Add("name", testcase);
            string url = string.Format("{0}/results/get?query={1}", mongoRest, JsonConvert.SerializeObject(query));
            RestCall call = new RestCall() { Url = url };
            //string output = call.Get();
            List<ivc_test_result> testcaseresults = JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get());
            if (testcaseresults != null && testcaseresults.Count > 0)
            {
                if (testcaseresults.First().status == "Passed")
                    return true;
            }
            return false;

        }

        public string GetTestsetNameForPack(string product, string packname)
        {
            string testsetname = "";
            Dictionary<string, string> queryparams = new Dictionary<string, string>();
            queryparams.Add("product", product);
            queryparams.Add("packname", packname);
            string req_url = mongoRest + "releases/get?query=" +
                             JsonConvert.SerializeObject(queryparams);
            RestCall call = new RestCall() {Url = req_url};
            string jsonOutput = call.Get();
            ivc_pack_details targetPack = JsonConvert.DeserializeObject<ivc_pack_details>(jsonOutput);
            if (targetPack != null)
            {
                testsetname = targetPack.testsetname;
            }
            return testsetname;
        }
        public string GetLastTestSetForPack(string product, string packname)
        {
            
            string testsetname = "";
            Dictionary<string, string> queryparams = new Dictionary<string, string>();
            queryparams.Add("product", product);
            queryparams.Add("packname", packname);
            string req_url = mongoRest + "releases/get?query=" +
                             JsonConvert.SerializeObject(queryparams);
            RestCall call = new RestCall() { Url = req_url };
            string jsonOutput = call.Get();
            ivc_pack_details targetPack1 = JsonConvert.DeserializeObject<ivc_pack_details>(jsonOutput);
            if (targetPack1 != null)
            {
                testsetname = targetPack1.testsetname;
            }

            if (packname.ToUpper() == "MT")
            {
                queryparams = new Dictionary<string, string>();
                queryparams.Add("product", product);
                queryparams.Add("packname", "preprod");
                req_url = mongoRest + "releases/get?query=" +  JsonConvert.SerializeObject(queryparams);
                call = new RestCall() { Url = req_url };
                jsonOutput = call.Get();
                ivc_pack_details targetPack2 = JsonConvert.DeserializeObject<ivc_pack_details>(jsonOutput);
                testsetname = DateTime.Parse(targetPack1.file_created) > DateTime.Parse(targetPack2.file_created) ? targetPack1.testsetname : targetPack2.testsetname;
            }

            return testsetname;
        }


        public ivc_pack_details GetPackDetails(string testsetname)
        {
            //MongoDriver mongoAPI = new MongoDriver();
            //return mongoAPI.Releases.GetPackDetails(testsetname);
            Dictionary<string, string> queryparams = new Dictionary<string, string>();
            queryparams.Add("testsetname", testsetname);
            string req_url = mongoRest + "releases/get?query=" +
                             JsonConvert.SerializeObject(queryparams);
            RestCall call = new RestCall() { Url = req_url };
            string jsonOutput = call.Get();
            ivc_pack_details targetPack = JsonConvert.DeserializeObject<ivc_pack_details>(jsonOutput);
            if (targetPack != null)
            {
                return targetPack;
            }
            else
            {
                return null;
            }
        }

        public List<string> GetAppServerListForPackSetup(string product, string pack, string config="Core")
        {
            List<string> appserverlist = new List<string>();
            string appurl =
                mongoRest + "scheduler/appservers?query={\"active\":true," +
                "\"runconfig\":\"" + config + "\"" + "," + "\"packname\":\"" + pack + "\" }";
            RestCall call = new RestCall() {Url = appurl};
            string app_output = call.Get();
            dynamic app_th = Newtonsoft.Json.Linq.JValue.Parse(app_output);
            foreach (var appserver1 in app_th)
            {
                appserverlist.Add(Convert.ToString(appserver1["hostname"]));
            }
            return appserverlist;
        }

        public List<ivc_appserver> GetAllAppServersForPackSetup(string product, string pack)
        {
            
            string appurl =
                mongoRest + "scheduler/appservers?query={\"active\":true," +
                "\"packname\":\"" + pack + "\" }";
            RestCall call = new RestCall() { Url = appurl };
            string app_output = call.Get();
            return JsonConvert.DeserializeObject<List<ivc_appserver>>(app_output);
            
        }
        public List<ivc_appserver> GetAllAppServersForPackSetup(string pack)
        {          
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            queryParams.Add("packname", pack);
            string arg = JsonConvert.SerializeObject(queryParams);
            var appurl = string.Format("{0}/scheduler/appservers?query={1}", mongoRest, arg);

            RestCall call = new RestCall() { Url = appurl };
            string app_output = call.Get();
            return JsonConvert.DeserializeObject<List<ivc_appserver>>(app_output);
        }

        public ivc_appserver GetServerByHostName(string hostname)
        {

            string appurl =
                 mongoRest + "scheduler/appservers?query={\"active\":true," +
                "\"hostname\":\"" + hostname + "\" }";
            RestCall call = new RestCall() { Url = appurl };
            string app_output = call.Get();
            if (!string.IsNullOrEmpty(app_output))
                return JsonConvert.DeserializeObject<List<ivc_appserver>>(app_output)[0];
            else
                return null;

        }

        public List<ivc_appserver> GetAllAppServersForPackSetup(string product, string pack, string config)
        {

            Dictionary<string, object> queryparams = new Dictionary<string, object>();
            queryparams.Add("active", true);
            queryparams.Add("packname", pack);
            queryparams.Add("product", product);
            queryparams.Add("runconfig", config);

            string appurl =
                mongoRest + "scheduler/appservers?query=" + JsonConvert.SerializeObject(queryparams);
           
            RestCall call = new RestCall() { Url = appurl };
            string app_output = call.Get();
            return JsonConvert.DeserializeObject<List<ivc_appserver>>(app_output);

        }

        public ivc_appserver GetServerKCMLService(string product, string pack, string appserver, bool serveractive = true)
        {
            Dictionary<string, object> queryparams = new Dictionary<string, object>();
            queryparams.Add("active", serveractive);
            queryparams.Add("packname", pack);
            queryparams.Add("product", product);
            queryparams.Add("hostname", appserver);

            string appurl =
               mongoRest + "scheduler/appservers?query=" + JsonConvert.SerializeObject(queryparams);

            RestCall call = new RestCall() { Url = appurl };
            string app_output = call.Get();
            return JsonConvert.DeserializeObject<List<ivc_appserver>>(app_output)[0];
        }

        public List<testpacket> GetConvertedRunlastTests(string product, string pack)
        {
            List<sConfig> items = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"));
            List<testpacket> runLastTests = new List<testpacket>();
            string testsetname = GetTestsetNameForPack(product, pack);
            string getURL = mongoRest + "results/get?query={\"testsetname\":\"" + testsetname + "\",\"status\":{\"$in\":[\"No Run\",\"Failed\"]}}";
            RestCall call = new RestCall() {Url = getURL};
            Dictionary<string, List<ivc_test_result>> resultsBySubmodule =
                JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get())
                    .GroupBy(t => t.submodule)
                    .ToDictionary(tt => tt.Key, tt => tt.ToList());
            foreach (sConfig item in items)
            {
                if (item.rlcon)
                {
                    foreach (ivc_test_result test in resultsBySubmodule[item.submodule])
                    {
                        if (test.name.ToLower().Contains("runlast"))
                        {
                            runLastTests.Add(new testpacket(test.name, test.submodule, Convert.ToInt32(test.counter), Convert.ToInt32(test.avgduration)));
                        }
                    }
                }
            }
            return runLastTests;
        }

        public List<ScheduledTestInformation> GetScheduledTestsforScrumAutomation(string product, string packname, string group, string name)
        {

            string getURL = mongoRest + "scheduler/get?query={\"product\":\"" + product + "\",\"packname\":\"" + packname + "\",\"group\":\"" + group + "\",\"name\":\""+ name +"\"}";
            RestCall call = new RestCall() { Url = getURL };
            return JsonConvert.DeserializeObject<List<ScheduledTestInformation>>(call.Get());
        }

        public List<ScheduledTestInformation> GetScheduledTestsforScrumAutomation(string product, string packname, string group)
        {

            string getURL = mongoRest + "scheduler/get?query={\"product\":\"" + product + "\",\"packname\":\"" + packname + "\",\"group\":\"" + group + "\"}";
            RestCall call = new RestCall() { Url = getURL };
            return JsonConvert.DeserializeObject<List<ScheduledTestInformation>>(call.Get());
        }

        public List<testpacket> GetValidTestsInOrder(string product, string packname, string testsetname)
        {
            List<sConfig> items = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"));
            string getURL = mongoRest + "results/get?query={\"testsetname\":\"" + testsetname + "\",\"status\":{\"$in\":[\"No Run\",\"Failed\"]}}";
            RestCall call = new RestCall() {Url = getURL};
            Dictionary<string, List<ivc_test_result>> resultsBySubmodule = JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get()).GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());
            List<testpacket> nunittests = new List<testpacket>();
            foreach (string tmpsub in resultsBySubmodule.Keys)
            {
                sConfig currentConfiguration = items.Find(t => t.submodule.Equals(tmpsub));
                List<ivc_test_result> subModuleTests = resultsBySubmodule[tmpsub];
                Dictionary<string, List<ivc_test_result>> testsbySubModuleBySuiteName = subModuleTests.GroupBy(t => t.suitename).ToDictionary(tt => tt.Key, tt => tt.ToList());

                foreach (string suite in testsbySubModuleBySuiteName.Keys)
                {
                    if (!suite.ToLower().Contains("runlast"))
                    {
                        List<ivc_test_result> actualTests = testsbySubModuleBySuiteName[suite];
                        int sequencenumber = 0;
                        int time2run = 0;
                        bool addTest = true;
                        foreach (ivc_test_result tmpTest in actualTests)
                        {
                            sequencenumber = sequencenumber + tmpTest.history + tmpTest.counter;
                            time2run = time2run + tmpTest.avgduration;

                            if (tmpTest.counter > 3)
                                addTest = false;
                        }

                        if (addTest)
                            nunittests.Add(new testpacket(suite, tmpsub, sequencenumber, time2run));
                    }
                }


            }

            return nunittests;

        }

        public List<ivc_test_result> GetValidTestsFromDashboard(string product, string packname, string testsetname)
        {
            string getValidTestsUrl = mongoRest + "results/get?query={\"testsetname\":\"" + testsetname + "\",\"status\":{\"$in\":[\"No Run\",\"Failed\"]}}";
            RestCall call = new RestCall() { Url = getValidTestsUrl };
            return JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get());
        }

        public List<ivc_test_result> GetAllTestsFromDashboard(string product, string packname, string testsetname)
        {
            string getValidTestsUrl = mongoRest + "results/get?query={\"testsetname\":\"" + testsetname + "\"}";
            RestCall call = new RestCall() { Url = getValidTestsUrl };
            string jsonOut = call.Get();
            return JsonConvert.DeserializeObject<List<ivc_test_result>>(jsonOut);
        }

        public List<ivc_test_result> GetValidTestsForModuleFromDashboard(string product, string packname, string module)
        {
            string testsetName = GetTestsetNameForPack(product, packname);
            Dictionary<string, object> queryparams = new Dictionary<string, object>();
            queryparams.Add("testsetname", testsetName);
            Dictionary<string, object> statusParams = new Dictionary<string, object>();
            statusParams.Add("$in", new List<string>(){"No Run", "Failed"});
            queryparams.Add("status", statusParams);
            queryparams.Add("module",module);
            string getValidTestsUrl = string.Format("{0}results/get?query={1}", mongoRest, JsonConvert.SerializeObject(queryparams));
            RestCall call = new RestCall() { Url = getValidTestsUrl };
            return JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get());
        }

        public List<ivc_test_result> GetValidTestsForSubModuleFromDashboard(string product, string packname, string submodule)
        {
            string testsetName = GetTestsetNameForPack(product, packname);
            Dictionary<string, object> queryparams = new Dictionary<string, object>();
            queryparams.Add("testsetname", testsetName);
            Dictionary<string, object> statusParams = new Dictionary<string, object>();
            statusParams.Add("$in", new List<string>() { "No Run", "Failed" });
            queryparams.Add("status", statusParams);
            queryparams.Add("submodule", submodule);
            string getValidTestsUrl = string.Format("{0}results/get?query={1}", mongoRest, JsonConvert.SerializeObject(queryparams));
            RestCall call = new RestCall() { Url = getValidTestsUrl };
            return JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get());
        }

        public List<ivc_test_result> GetValidTestsFromDashboard(string product, string packname)
        {
            string testsetName = GetTestsetNameForPack(product, packname);
            Dictionary<string, object> queryparams = new Dictionary<string, object>();
            queryparams.Add("testsetname", testsetName);
            Dictionary<string, object> statusParams = new Dictionary<string, object>();
            statusParams.Add("$in", new List<string>() { "No Run", "Failed" });
            queryparams.Add("status", statusParams);            
            string getValidTestsUrl = string.Format("{0}results/get?query={1}", mongoRest, JsonConvert.SerializeObject(queryparams));
            RestCall call = new RestCall() { Url = getValidTestsUrl };
            return JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get());
        }

        public int GetAverageExecutionTime(string testname, string testsetname)
        {
            string getTestInfoURL = mongoRest + "results/get?query={\"testsetname\":\"" + testsetname + "\",\"suitename\":\"" + testname + "\"}";
            Console.WriteLine("URL is " + getTestInfoURL);
            RestCall call = new RestCall() {Url = getTestInfoURL};
            string testInfoResponse = call.Get();
            List<ivc_test_result> notPassedTests = JsonConvert.DeserializeObject<List<ivc_test_result>>(testInfoResponse);
            return notPassedTests.Sum(t => t.avgduration) * 2;
        }

        public ivc_recent_releases RecentReleases(string product, string pack, string status)
        {
            Dictionary<string, string> queryparams = new Dictionary<string, string>();
            queryparams.Add("product", product);
            queryparams.Add("packname", pack);
            queryparams.Add("status", status);
            string url = string.Format("{0}releases/recent?query={1}", mongoRest, JsonConvert.SerializeObject(queryparams));
            RestCall call = new RestCall() {Url = url};
            string output = call.Get();
            ivc_recent_releases recentPackRelease = JsonConvert.DeserializeObject<ivc_recent_releases>(output);
            return recentPackRelease;
        }
        public ivc_recent_releases RecentReleaseswithVersion(string product, string version, string status,string pack)
        {
            Dictionary<string, string> queryparams = new Dictionary<string, string>();
            queryparams.Add("product", product);
            queryparams.Add("system", version);
            queryparams.Add("status", status);
            queryparams.Add("packname", pack);
            string url = string.Format("{0}releases/recent?query={1}", mongoRest, JsonConvert.SerializeObject(queryparams));
            RestCall call = new RestCall() { Url = url };
            string output = call.Get();
            ivc_recent_releases recentPackRelease = JsonConvert.DeserializeObject<ivc_recent_releases>(output);
            return recentPackRelease;
        }

        public bool CheckInstalledRelease(string product, string pack, string status)
        {
            ivc_recent_releases recentPackRelease = RecentReleases(product, pack, "Installed");
            if (recentPackRelease == null)
            {
                Console.WriteLine("Release is not yet installed");
                return false;
            }
            ivc_pack_details currentPackRelease =
                JsonConvert.DeserializeObject<ivc_pack_details>(GetReleaseInformation(product, pack).ToString());
            if(!CheckPickedRelease(product, pack))
                {
                if (currentPackRelease.updateid.Equals(recentPackRelease.updateid) || currentPackRelease.updateid.Equals(recentPackRelease.relid))
                {
                    Console.WriteLine("There is no new release available");
                    UpdateRecentReleaseStatus(product, pack);
                    return false;
                }
            }
            if (!currentPackRelease.systemversion.Equals(recentPackRelease.system))
            {
                Console.WriteLine("The system version is incorrect");
                UpdateRecentReleaseStatus(product, pack);
                return false;
            }
            return true;
        }
        public bool CheckPickedRelease(string product, string pack)
        {
            ivc_recent_releases recentPackRelease = RecentReleases(product, pack, "Picked");
            Console.WriteLine("Check for Release in Picked Status");
            if (recentPackRelease != null)
                return true;
            else
                return false;
        }
        public string CheckReleaseCreation(string product, string systemversion, string status,string pack)
        {
            string updateID = "";            
            ivc_recent_releases recentPackRelease = RecentReleaseswithVersion(product, systemversion, status, pack);

            if(recentPackRelease != null)
            {
                Console.WriteLine("{0} : {1} : {2} : {3} : {4}", product, pack, systemversion, recentPackRelease.relid, recentPackRelease.updateid);
                updateID = recentPackRelease.relid;
            }
            else
            {
                Console.WriteLine("Not Installed RR found for {0} : {1} : {2}", product, pack, systemversion);
            }
            return updateID;
        }

        public void UpdateRecentReleaseStatus_old(string product, string pack)
        {
            ivc_recent_releases recentPackRelease = RecentReleases(product, pack, "Installed");
            Dictionary<string, object> create = new Dictionary<string, object>();
            string date = recentPackRelease.date;
            create.Add("relid", recentPackRelease.relid);
            create.Add("status", "Triggered");
            create.Add("product", recentPackRelease.product);
            create.Add("packname", recentPackRelease.packname);
            create.Add("system", recentPackRelease.system);
            create.Add("updateid", recentPackRelease.updateid);
            create.Add("date", date);
            create.Add("updatedesc", recentPackRelease.updatedesc);
            string dashboardRestUrl = "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc/rest";
            RestCall call = new RestCall()
            {
                Url = string.Format("{0}/releases/update_recent/?data={1}", dashboardRestUrl, JsonConvert.SerializeObject(create))
            };
            call.Post(create);

        }
        public void UpdateRecentReleaseStatus(string product, string pack)
        {
            ivc_recent_releases recentPackRelease = RecentReleases(product, pack, "Installed");
            if (recentPackRelease != null)
            {
                Dictionary<string, object> create = new Dictionary<string, object>();
                string date = recentPackRelease.date;
                create.Add("relid", recentPackRelease.relid);
                create.Add("status", "Triggered");
                create.Add("product", recentPackRelease.product);
                create.Add("packname", recentPackRelease.packname);
                create.Add("system", recentPackRelease.system);
                create.Add("updateid", recentPackRelease.updateid);
                create.Add("date", date);
                create.Add("updatedesc", recentPackRelease.updatedesc);
                string dashboardRestUrl = "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc/rest";
                RestCall call = new RestCall()
                {
                    Url = string.Format("{0}/releases/update_recent/?data={1}", dashboardRestUrl, JsonConvert.SerializeObject(create))
                };
                call.Post(create);
            }
            else
                UpdateRecentReleaseStatustoTriggered(product, pack);
        }
        public void UpdateRecentReleaseStatustoTriggered(string product, string pack)
        {
            ivc_recent_releases recentPackRelease = RecentReleases(product, pack, "Picked");
            Dictionary<string, object> create = new Dictionary<string, object>();
            string date = recentPackRelease.date;
            create.Add("relid", recentPackRelease.relid);
            create.Add("status", "Triggered");
            create.Add("product", recentPackRelease.product);
            create.Add("packname", recentPackRelease.packname);
            create.Add("system", recentPackRelease.system);
            create.Add("updateid", recentPackRelease.updateid);
            create.Add("date", date);
            create.Add("updatedesc", recentPackRelease.updatedesc);
            string dashboardRestUrl = "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc/rest";
            RestCall call = new RestCall()
            {
                Url = string.Format("{0}/releases/update_recent/?data={1}", dashboardRestUrl, JsonConvert.SerializeObject(create))
            };
            call.Post(create);
        }
        public void UpdateRecentReleaseStatustoPicked(string product, string pack)
        {
            ivc_recent_releases recentPackRelease = RecentReleases(product, pack, "Installed");
            Dictionary<string, object> create = new Dictionary<string, object>();
            string date = recentPackRelease.date;
            create.Add("relid", recentPackRelease.relid);
            create.Add("status", "Picked");
            create.Add("product", recentPackRelease.product);
            create.Add("packname", recentPackRelease.packname);
            create.Add("system", recentPackRelease.system);
            create.Add("updateid", recentPackRelease.updateid);
            create.Add("date", date);
            create.Add("updatedesc", recentPackRelease.updatedesc);
            string dashboardRestUrl = "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc/rest";
            RestCall call = new RestCall()
            {
                Url = string.Format("{0}/releases/update_recent/?data={1}", dashboardRestUrl, JsonConvert.SerializeObject(create))
            };
            call.Post(create);
        }

        public string CodeVersion(string product, string pack)
        {
            string codeVersion = string.Empty;
            string rePack = pack;
            if (pack.ToLower().Contains("mt") || pack.ToLower().Contains("preprod") || pack.ToLower().Contains("profiler"))
                pack = "Pilot";
            
            releaseinformation recentPackRelease = GetReleaseInformationForProductAndPack(product, pack);
            if (recentPackRelease != null)
                codeVersion = recentPackRelease.ivccodeversion;

            if (rePack.ToLower().Contains("mt") || rePack.ToLower().Contains("preprod") || rePack.ToLower().Contains("profiler"))
                codeVersion = Convert.ToString(Convert.ToDouble(codeVersion) + 0.01);

            return codeVersion;
        }

        public List<ivc_test_result> GetTestDetailsbySuiteName(string suitename, string testsetname)
        {
            MongoDriver mongoAPI = new MongoDriver();
            return mongoAPI.Results.GetTestsBySuiteName(suitename, testsetname);

            ////Results results = new Results();
            ////return results.GetTestsBySuiteName(suitename, testsetname);
            //string getValidTestsUrl = mongoRest + "results/get?query={\"testsetname\":\"" + testsetname + "\",\"suitename\":\"" + suitename + "\"}";
            //Console.WriteLine(getValidTestsUrl);
            //RestCall call = new RestCall() { Url = getValidTestsUrl };
            //return JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get());

        }

        public ivc_test_result GetTestDetailsbyTestName(string testName, string testsetname)
        {
            MongoDriver mongoAPI = new MongoDriver();
            List<ivc_test_result> tests = mongoAPI.Results.GetTestsByTestName(testName, testsetname);
            return tests.First();
        }

        public List<ivc_test_result> GetValidTestsForSubModuleFromDashboard(string submodule, string testsetname)
        {
            string getValidTestsUrl = mongoRest + "results/get?query={\"testsetname\":\"" + testsetname + "\",\"submodule\":\"" + submodule + "\",\"status\":{\"$in\":[\"No Run\",\"Failed\"]}}";            
            RestCall call = new RestCall() { Url = getValidTestsUrl };
            return JsonConvert.DeserializeObject<List<ivc_test_result>>(call.Get());

        }

        public string GetSnapShot(string pack)
        {
            RestCall call = new RestCall()
            {
                Url = "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc/dynamic/getTestResultsOfPack.php?pack=" + pack
            };

            return call.Get();
        }

        public void PostSnapShot(string data)
        {
            esRest getObj = JsonConvert.DeserializeObject<esRest>(data);

            esPost epost = new esPost
            {
                passed = getObj.passed,
                norun = getObj.norun,
                failed = getObj.failed,
                blocked = getObj.blocked,
                testsetname = getObj.testsetname,
                timestamp = DateTime.Now.ToString("HH:mm")
            };
            RestCall call = new RestCall
            {
                Url = "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc/rest/results/update_execution_trend"
            };
            call.Post(JsonConvert.SerializeObject(epost));

        }
    }
}
