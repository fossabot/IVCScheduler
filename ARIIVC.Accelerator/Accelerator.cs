using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;
using System.IO;
using Newtonsoft.Json;
using ARIIVC.Scheduler;
using System.Diagnostics;
using ARIIVC.Scheduler.JsonReps;
using zephyrapi;
using System.Text.RegularExpressions;
using File = System.IO.File;
using System.Configuration;
using MongoDB.Driver;
using System.Net.Mail;
using ARIIVC.Utilities;
using ARIIVC.CSDTConnector.JsonReps;
using ARIIVC.Utilities.JsonRepo;
using MongoDB.Bson;

namespace ARIIVC.Accelerator
{

    public class DMSLiteTests
    {
        public List<string> Execute { get; set; }
        public List<string> Progress { get; set; }
    }

    public class Accelerator
    {
        static readonly string CommitUrl = "http://139.126.80.68:8686/api/v1/profiler";
        static readonly string TestprofileUrl = "http://139.126.80.68:8686/api/v1/profiledata";
        public static string dashboardserver = "gbhpdslivcweb01.dsi.ad.adp.com/";
        public static string mongoRest = "http://" + dashboardserver + "ivc/rest";
        private static readonly string MongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
        public static string jiraUserName = "svc_autoline_ivc";
        public static string jiraPassword = "6xL@tCdw]/";
        public static string deploymentserver = "gbhsremrepo01.gbh.dsi.adp.com";
        public static string csdtRest = "http://" + deploymentserver + "/csdt/rest";

        static int Main(string[] args)
        {
            return Parser.Default
                .ParseArguments<UpdateImpactedTests, ScrumAutomationOptions, TriggerSingleTest, ProcessWorkflowsOptions,
                    UpdateWorkFlows, ProcessSingleTest, CreateDashboardProfilerOptions, ProfilerTriggerTestsOptions,
                    ProfilerSingleTestOptions, WorkFlowsChangeVersion, TestsnotUpdatedwithProfiler,
                    PreProdProfilerCreateDashboardOptions, GetIssuesList,CreateDashboardRingReleaseOptions, MoveTestsettoProcessed, CreateDashboardDMSLiteOptions>(args)
                .MapResult(
                    (UpdateImpactedTests opts) => accelerator_update_impacted_tests(opts),
                    (ScrumAutomationOptions opts) => ScrumAutomation(opts),
                    (TriggerSingleTest opts) => ScrumAutomation_SingleTest(opts),
                    (ProcessWorkflowsOptions opts) => ProcessWorkflows(opts),
                    (UpdateWorkFlows opts) => UpdateWorkFlows(opts),
                    (ProcessSingleTest opts) => ProcessSingleTest(opts),
                    (CreateDashboardProfilerOptions opts) => CreateDashboardProfiler(opts),
                    (PreProdProfilerCreateDashboardOptions opts) => PreProdProfilerCreateDashboard(),
                    (ProfilerTriggerTestsOptions opts) => ProfilerTriggerTests(),
                    (ProfilerSingleTestOptions opts) => ProfilerSingleTest(opts),
                    (WorkFlowsChangeVersion opts) => ChangeWorkFlowVersionCodeCut(),
                    (TestsnotUpdatedwithProfiler opts) => TestsnotUpdatedwithProfiler(opts),
                    (GetIssuesList opts)=> GetIssuesList(opts),
                    (CreateDashboardRingReleaseOptions opts)=> CreateDashboardProfiler_RingRelease(opts),
                    (MoveTestsettoProcessed opts)=> MoveTestsToProcessed(opts),
                    (CreateDashboardDMSLiteOptions opts) => CreateDashboardDMSLite(opts),
                    errs => 1);
        }

        static void accelerator_insert_testprofiledata(testprofile profiletestdata)
        {
            RestCall rest = new RestCall();
            rest.Url = TestprofileUrl;
            var testProfileDataAsString = JsonConvert.SerializeObject(profiletestdata);
            rest.Post(testProfileDataAsString); ;

        }

        static testprofile accelerator_get_scheduled_testprofiledata()
        {
            RestCall rest = new RestCall();
            rest.Url = TestprofileUrl;
            return JsonConvert.DeserializeObject<List<testprofile>>(rest.Get()).Find(t => t.Teststatus.ToLower().Equals("scheduled", StringComparison.InvariantCultureIgnoreCase));

        }

        static void accelerator_update_testprofiledata(string header, string reqBody)
        {
            RestCall rest = new RestCall();
            rest.Url = TestprofileUrl + "/" + header;
            rest.Put(reqBody);

        }

        static void accelerator_update_profiledata(string hash, string reqBody)
        {
            RestCall rest = new RestCall();
            rest.Url = CommitUrl + "/" + hash;
            rest.Put(reqBody);

        }

        void accelerator_delete_profiledata(profiledata commitData)
        {
            RestCall rest = new RestCall();
            rest.Url = CommitUrl;
            rest.Post(JsonConvert.SerializeObject(commitData));
        }

        static List<profiledata> accelerator_get_commitdata()
        {
            RestCall rest = new RestCall();
            rest.Url = CommitUrl;
            var commitData = JsonConvert.DeserializeObject<List<profiledata>>(rest.Get());
            return commitData;
        }

        static profiledata accelerator_get_commitdata(string hash)
        {
            RestCall rest = new RestCall();
            rest.Url = CommitUrl + "/" + hash;
            var commitData = JsonConvert.DeserializeObject<profiledata>(rest.Get());
            return commitData;
        }

        public static void CreateTestProfileData(string version)
        {
            List<profiledata> commitdata = accelerator_get_commitdata();
            foreach (profiledata file in commitdata)
            {
                var sourceFiles = file.files;
                foreach (var sourceFile in sourceFiles)
                {
                    sourceFile.ImpactedFunctions = new List<ImpactedFunction>();
                    var functions = sourceFile.Functions;
                    foreach (var function in functions)
                    {
                        var impactedFunction = new ImpactedFunction()
                        {
                            FunctionName = function,
                            Workflows = GetImpactedTestsForFunction(function, version)
                        };
                        sourceFile.ImpactedFunctions.Add(impactedFunction);
                    }

                }

            }
            testprofile testdata = new testprofile();
            testdata.Header = Stopwatch.GetTimestamp().ToString();
            testdata.Version = version;
            testdata.commits = commitdata;
            testdata.Teststatus = "scheduled";
            accelerator_insert_testprofiledata(testdata);
        }

        public static List<string> GetImpactedTestsForFunction(string methodname, string version)
        {
            AcceleratorMongo acceleratorMongo = new AcceleratorMongo(MongoUrl)
            {
                Version = version
            };
            return acceleratorMongo.GetImpactedTestsForFunction(methodname);
        }

        public static void accelerator_trigger_batch()
        {

            DashboardConnector dc = new DashboardConnector();
            testprofile executionData = accelerator_get_scheduled_testprofiledata();
            List<string> allTests = accelerator_aggregate_tests(executionData);
            string version = executionData.Version;
            releaseinformation rdetails = dc.GetReleaseInformationFromCodeVersion(version);

            //Create Dashboard
            string testsetname = CreateDashboard(rdetails, allTests);
            Dictionary<string, string> bodydata = new Dictionary<string, string>();
            bodydata.Add("testsetname", testsetname);
            accelerator_update_testprofiledata(executionData.Header, JsonConvert.SerializeObject(bodydata));
            TriggerTests(testsetname, executionData.Header);

        }

        static List<string> accelerator_aggregate_tests(testprofile executionData)
        {
            List<string> allTests = new List<string>();
            foreach (profiledata commit in executionData.commits)
            {

                foreach (SourceFile src in commit.files)
                {
                    foreach (ImpactedFunction srcFunctions in src.ImpactedFunctions)
                    {
                        if (srcFunctions.Workflows != null && srcFunctions.Workflows.Count > 0)
                            allTests.AddRange(srcFunctions.Workflows);
                    }
                }
            }

            return allTests;

        }

        static string CreateDashboard(releaseinformation releaseDetails, List<string> allTestsDuplicates)
        {

            DashboardConnector connector = new DashboardConnector();

            List<string> allTests = allTestsDuplicates.Distinct().ToList();

            List<sConfig> items = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties")).FindAll(t => t.sequential.Equals(true));

            // Create test set in MongoDB through Dashboard REST API
            string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + releaseDetails.packname + "-" + DateTime.Now.ToString("hhmmsstt");
            SchedulerLogger.Log(testSetName, "Started creating Dashboard with name" + testSetName);
            RestCall call = new RestCall();

            // Get tests from Zephyr
            zapi zephyrApi = new zapi("svc_autoline_ivc", "6xL@tCdw]/", "https://projects.cdk.com");
            string codeVersion = connector.CodeVersion(releaseDetails.product, releaseDetails.packname);

            //JIRA Connection
            jProject jp = zephyrApi.getProject("IDRIVE");
            jVersion currentVersion = jp.versions.Find(t => t.name.Contains(codeVersion));
            SchedulerLogger.Log(testSetName, "Getting the Current Version from Zephyr");
            string zephyrCodeVersion = currentVersion.name;

            Dictionary<string, object> create = new Dictionary<string, object>();

            //Uncomment this once done with testing 
            foreach (string testname in allTests)
            {
                //get the Zephyr representation of the test
                zTest testToExecute = zephyrApi.searchTestByscriptID(testname.Split('.')[testname.Split('.').Length - 1]);
                SchedulerLogger.Log(testSetName, "Pulling test cases from Zephyr according to Version");

                //List<ivc_test_result> basedata = connector.GetBaseData(releaseDetails.packname);

                if (testToExecute != null)
                {

                    int iterator = 0;

                    #region Make sure SuiteID is properly populated

                    if (string.IsNullOrEmpty(testToExecute.fields.SuiteID))
                    {
                        testToExecute.fields.SuiteID = testToExecute.fields.ScriptID;
                    }

                    #endregion

                    int history = 0;
                    int counter = 0;
                    int avgDuration = 900;


                    create.Clear();
                    create.Add("name", testToExecute.fields.ScriptID);
                    create.Add("summary", testToExecute.fields.summary);
                    create.Add("testid", testToExecute.id);
                    string description = string.Empty;
                    if (!string.IsNullOrEmpty(testToExecute.fields.description))
                        description = Regex.Replace(testToExecute.fields.description, @"[^a-zA-Z0-9 ]", "",
                            RegexOptions.Compiled);
                    create.Add("description", description);
                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");
                    create.Add("host", "Not Coded");
                    create.Add("author", testToExecute.fields.creator != null ? testToExecute.fields.creator.displayName : "ivcauto");
                    create.Add("created", testToExecute.fields.versions[0].name);
                    create.Add("runner", "Default");
                    create.Add("F2US", "9999");
                    create.Add("counter", counter);
                    create.Add("history", history);
                    create.Add("avgduration", avgDuration);
                    create.Add("IVUS", testToExecute.key);
                    create.Add("module", testToExecute.fields.components[0].name);
                    create.Add("submodule", testToExecute.fields.labels[0]);
                    create.Add("logs", new Jenkins().TestScheduled());


                    if (items.Find(t => t.submodule.Equals(testToExecute.fields.labels[0], StringComparison.InvariantCultureIgnoreCase)) != null)
                    {
                        create.Add("success", testToExecute.fields.labels[0]);
                    }
                    else
                    {
                        create.Add("success", "ivctest");
                    }

                    if (testToExecute.fields.ScriptID.ToLower().Contains("runlast"))
                    {
                        create.Add("suitename", "RunLast");
                    }
                    else
                    {
                        var suiteName = testToExecute.fields.SuiteID ?? testToExecute.fields.ScriptID;
                        create.Add("suitename", suiteName);
                    }
                    create.Add("testsetname", testSetName);
                    create.Add("packname", releaseDetails.packname);
                    call = new RestCall() { Url = string.Format("{0}/results/insert/", SchedulerBase.mongoRest) };
                    call.Post(create);
                    iterator++;
                }
            }


            create.Clear();

            #region Update the releases table and recent_releases table
            ivc_recent_releases recentRelease = new ivc_recent_releases();

            create.Add("ivccodeversion", releaseDetails.packname.ToLower().Contains("mt") ? "MT" : codeVersion);
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "Drive-" + releaseDetails.packname);
            create.Add("testsetid", "-1");
            create.Add("packname", releaseDetails.packname);
            create.Add("product", releaseDetails.product);
            create.Add("date", testSetName.Split('_')[0]);
            create.Add("file_created", DateTime.Now.ToString("s"));
            create.Add("last_modified", DateTime.Now.ToString("s"));
            create.Add("last_modified_notification", "");
            call = new RestCall()
            {
                Url = string.Format("{0}/releases/insert/?data={1}", SchedulerBase.mongoRest, JsonConvert.SerializeObject(create))
            };
            call.Post(create);
            SchedulerLogger.Log(testSetName, "Updated the release table with test set name");
            #endregion

            return testSetName;
        }

        static void TriggerTests(string testsetname, string testprofileheader)
        {
            DashboardConnector dc = new DashboardConnector();
            Jenkins jenkins = new Jenkins();
            releaseinformation relDetails = dc.GetReleaseInformationFromTestSetName(testsetname);
            List<ivc_test_result> allTests = dc.GetValidTestsFromDashboard(relDetails.product, relDetails.packname, testsetname);


            foreach (ivc_test_result tmp in allTests)
            {
                sConfig item = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties")).Find(yy => yy.submodule.Equals(tmp.submodule));
                List<ivc_appserver> allAppServers = dc.GetAllAppServersForPackSetup(relDetails.product, relDetails.packname, item.runconfig);

                if (allAppServers != null && allAppServers.Count > 0)
                {
                    RunSingleTest rc = new RunSingleTest();
                    rc.Test = tmp.name;
                    rc.TestSetName = testsetname;
                    rc.AppServer = allAppServers[0].hostname;

                    Dictionary<string, string> buildArguments = new Dictionary<string, string>();
                    buildArguments.Add("test", tmp.name);
                    buildArguments.Add("testset", testsetname);
                    buildArguments.Add("appserver", allAppServers[0].hostname);
                    buildArguments.Add("label", "ivctest1");
                    jenkins.TriggerJob("REGRESSION_NIGHTLY_RUN_SINGLE", buildArguments);

                }

                Dictionary<string, string> bodydata = new Dictionary<string, string>();
                bodydata.Add("teststatus", "triggered");
                accelerator_update_testprofiledata(testprofileheader, JsonConvert.SerializeObject(bodydata));


            }

        }

        static int accelerator_update_impacted_tests(UpdateImpactedTests opts)
        {
            string githash = opts.Hash;

            profiledata commitInfo = accelerator_get_commitdata(opts.Hash);
            List<string> impactedTests = new List<string>();

            foreach (SourceFile sourceFile in commitInfo.files)
            {
                foreach (var function in sourceFile.Functions)
                {
                    {
                        impactedTests.AddRange(GetImpactedTestsForFunction(function, opts.CodeVersion));
                    }
                }
            }

            Dictionary<string, object> bodydata = new Dictionary<string, object>();
            bodydata.Add("tests", impactedTests);
            accelerator_update_profiledata(opts.Hash, JsonConvert.SerializeObject(bodydata));

            return 0;
        }

        public static int ScrumAutomation(ScrumAutomationOptions opts)
        {
            //create a testsetname for dashboard
            string testsetname = DateTime.Now.ToString("dd-MM-yyyy") + "_" + opts.Pack + "-" + DateTime.Now.ToString("hhmmsstt");
            SchedulerLogger.Log(testsetname, "create a testsetname for dashboard");

            DashboardConnector connector = new DashboardConnector();
            //get the list of tests that are added in the scheduler
            List<ScheduledTestInformation> tests = connector.GetScheduledTestsforScrumAutomation(opts.Product, opts.Pack, opts.Group);
            SchedulerLogger.Log(testsetname, "get the list of tests that are added in the scheduler");
            List<string> TestNameList = new List<string>();

            foreach (ScheduledTestInformation singleBlock in tests)
            {
                foreach (var testSuite in singleBlock.tests)
                {
                    foreach (var singleTest in testSuite.qctests)
                    {
                        TestNameList.Add(singleTest.scriptid);
                    }
                }
            }
            SchedulerLogger.Log(testsetname, "Extract the list of Zephyr test names");

            #region Create Dashboard with Zephyr data
            CreateDashboardForScheduledTests(opts.Product, opts.Pack, testsetname, TestNameList);
            #endregion

            #region Trigger job for each tests
            foreach (ScheduledTestInformation singleBlock in tests)
            {
                //call the jenkins job for each test
                foreach (testsuite ts in singleBlock.tests)
                {
                    Jenkins jenkinsSched = new Jenkins("http://139.126.80.68:8080");
                    Dictionary<string, string> jobParams = new Dictionary<string, string>();
                    jobParams.Add("test", ts.test);
                    jobParams.Add("testset", testsetname);
                    jobParams.Add("appserver", singleBlock.appserver);
                    jobParams.Add("label", ts.module.ToLower());
                    jenkinsSched.TriggerJob("REGRESSION_NIGHTLY_RUN_SINGLE", jobParams);
                }

            }
            #endregion

            return 0;
        }

        public static int ScrumAutomation_SingleTest(TriggerSingleTest opts) //string appserver, string pack, string testsetname, string testname, string group, string subscribers)
        {

            DashboardConnector connector = new DashboardConnector();

            //get and unzip the test libraries
            SchedulerBase.ScrumAutomation_GetTestLibraries();

            //get the service name based on appserver
            ivc_appserver serverDetails = connector.GetServerKCMLService("Drive Sprint Teams", opts.Pack, opts.AppServer);

            //update the release information so that it is visible
            SchedulerBase.ScrumAutomation_updateReleaseInformation(opts.TestsetName, opts.Pack);

            //update the config file
            SchedulerBase.ScrumAutomation_UpdateConfigurationFile(opts.AppServer, serverDetails.service, opts.TestsetName, serverDetails.kpath, serverDetails.serverurl);

            //create Nunit Runlist file
            SchedulerBase.CreateNUnitRunList(opts.TestName);

            //Trigger the execution
            SchedulerBase.TriggerExecution(opts.Pack, opts.AppServer, opts.TestsetName, opts.TestName, opts.Group, opts.Subscribers);

            return 0;


        }

        public static int CreateDashboardForScheduledTests(string product, string pack, string testSetName, List<string> testNameList)
        {
            DashboardConnector connector = new DashboardConnector();
            zapi zephyrApi = new zapi(jiraUserName, jiraPassword, "https://projects.cdk.com");
            List<zTest> zephyrTests = zephyrApi.getTestsByScriptIds(testNameList, "Automated");
            SchedulerLogger.Log(testSetName, "Pulling all automated test cases from Zephyr");
            List<ExecutionBaseData> basedata = connector.GetBaseData(pack);
            Dictionary<string, object> create = new Dictionary<string, object>();
            RestCall call = new RestCall();
            int iterator = 0;

            Dictionary<string, List<zTest>> testsBySubModule = zephyrTests.GroupBy(t => t.fields.labels[0]).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in testsBySubModule.Keys)
            {
                SchedulerLogger.Log(testSetName, string.Format("Adding submodule :{0} tests to Dasboard : {1}", submodule, testsBySubModule[submodule].Count));
                foreach (var testToExecute in testsBySubModule[submodule])
                {
                    int history = 0;
                    int counter = 0;
                    int avgDuration = 900;
                    create.Clear();
                    create.Add("name", testToExecute.fields.ScriptID);
                    create.Add("summary", testToExecute.fields.summary);
                    create.Add("testid", testToExecute.id);
                    string description = string.Empty;
                    if (!string.IsNullOrEmpty(testToExecute.fields.description))
                        description = Regex.Replace(testToExecute.fields.description, @"[^a-zA-Z0-9 ]", "",
                            RegexOptions.Compiled);
                    create.Add("description", description);
                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");
                    create.Add("host", "Not Coded");
                    create.Add("author", testToExecute.fields.creator != null ? testToExecute.fields.creator.displayName : "ivcauto");
                    create.Add("created", testToExecute.fields.versions[0].name);
                    create.Add("runner", "Default");
                    create.Add("F2US", "9999");
                    create.Add("counter", counter);
                    create.Add("history", history);
                    create.Add("avgduration", avgDuration);
                    create.Add("IVUS", testToExecute.key);
                    create.Add("module", testToExecute.fields.components[0].name);
                    create.Add("submodule", testToExecute.fields.labels[0]);
                    create.Add("logs", new Jenkins().TestScheduled());
                    create.Add("success", "ivctest");

                    if (testToExecute.fields.ScriptID.ToLower().Contains("runlast"))
                    {
                        create.Add("suitename", "RunLast");
                    }
                    else
                    {
                        var suiteName = testToExecute.fields.SuiteID ?? testToExecute.fields.ScriptID;
                        create.Add("suitename", suiteName);
                    }
                    create.Add("testsetname", testSetName);
                    create.Add("packname", pack);
                    call = new RestCall() { Url = string.Format("{0}/results/insert/", mongoRest) };
                    call.Post(create);
                    iterator++;
                }
            }
            create.Clear();
            create.Add("ivccodeversion", "MT");
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "Drive-" + pack);
            create.Add("testsetid", "-1");
            create.Add("packname", pack);
            create.Add("product", product);
            create.Add("date", testSetName.Split('_')[0]);
            create.Add("file_created", DateTime.Now.ToString("s"));
            create.Add("last_modified", DateTime.Now.ToString("s"));
            create.Add("last_modified_notification", "");
            call = new RestCall()
            {
                Url = string.Format("{0}/releases/insert/?data={1}", mongoRest, JsonConvert.SerializeObject(create))
            };
            call.Post(create);
            SchedulerLogger.Log(testSetName, "Updated the release table with test set name");

            return 0;
        }

        static int ProcessWorkflows(ProcessWorkflowsOptions opts)
        {
            #region Variables            
            string testcasename = "";
            string remotePath = "";
            List<string> unprocessedWorkflows = new List<string>();
            List<ProfileParameters> profileparmeters = JsonConvert.DeserializeObject<List<ProfileParameters>>(File.ReadAllText("ProfileParameters.json"));
            IList<string> filelist = new List<string>();
            #endregion

            List<string> testnotfound = new List<string>();
            List<string> processedfiles = new List<string>();
            List<string> failedtests = new List<string>();


            ProfileParameters _profileparmeters = profileparmeters.Find(t => t.pack.ToUpper().Equals(opts.Version.ToUpper()));

            if (_profileparmeters == null)
            {
                Console.WriteLine("No mount point defined for this version");
                return -1;
            }

            #region get the relevant tests from the test set
            MongoDriver driver = new MongoDriver();
            string testSetName = driver.Releases.GetTestSetName(opts.Product, _profileparmeters.pack);
            Console.WriteLine("Considering test set {0} for Result Validation", testSetName);
            List<ivc_test_result> allTests = driver.Results.GetAllTests(testSetName);
            #endregion

            filelist = Ftp.FileList(_profileparmeters.mountPoint, remotePath, _profileparmeters.user, _profileparmeters.password);

            foreach (var fil in filelist)
            {
                string filename = new FileInfo(fil).Name;
                var tempVar = filename.Split('.').ToList();
                tempVar.RemoveRange(tempVar.Count - 2, 2);
                testcasename = tempVar.Last();
                var currentTest = allTests.Find(t => t.name.ToLower().Equals(testcasename.ToLower()));
                if (currentTest == null)
                {
                    Console.WriteLine("Cannot find the test with name {0} : {1}", testcasename, filename);
                    testnotfound.Add(filename);
                }
                else
                {
                    if (currentTest.status.ToLower() == "passed")
                    {
                        if (!processedfiles.Contains(testcasename))
                        {
                            processedfiles.Add(testcasename);
                            Jenkins jenkins = new Jenkins();
                            Dictionary<string, string> JenkinsParams = new Dictionary<string, string>();
                            JenkinsParams.Add("product", opts.Product);
                            JenkinsParams.Add("version", opts.Version);
                            JenkinsParams.Add("filename", testcasename);
                            jenkins.TriggerJob("Profiler_Update_Workflow", JenkinsParams);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Test status is failed : {0} : {1}", testcasename, filename);
                        failedtests.Add(filename);
                    }
                }
            }

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("+++++++++++++++++++++ TESTS NOT FOUND ++++++++++++++++++++");
            Console.WriteLine("");
            foreach (string tmp in testnotfound)
            {
                Console.WriteLine("{0}", tmp);
            }
            Console.WriteLine("");
            Console.WriteLine("+++++++++++++++++++++ TESTS FAILED ++++++++++++++++++++");
            Console.WriteLine("");
            foreach (string tmp in failedtests)
            {
                Console.WriteLine("{0}", tmp);
            }

            Console.WriteLine("");
            Console.WriteLine("+++++++++++++++++++++ TESTS PROCESSED ++++++++++++++++++++");
            Console.WriteLine("");
            foreach (string tmp in processedfiles)
            {
                Console.WriteLine("{0}", tmp);
            }






            Ftp.DeleteFiles(_profileparmeters.mountPoint, remotePath, failedtests, _profileparmeters.user, _profileparmeters.password);

            return 0;
        }

        static int UpdateWorkFlows(UpdateWorkFlows opts)
        {
            #region Variables            
            string workflowname = "";
            string testcasename = "";
            string remotePath = "";
            List<string> unprocessedWorkflows = new List<string>();
            List<ProfileParameters> profileparmeters = JsonConvert.DeserializeObject<List<ProfileParameters>>(File.ReadAllText("ProfileParameters.json"));
            List<string> filelist = new List<string>();
            #endregion

            List<string> testnotfound = new List<string>();
            List<string> processedfiles = new List<string>();
            List<string> failedtests = new List<string>();
            List<string> parsingerror = new List<string>();

            ProfileParameters _profileparmeters = profileparmeters.Find(t => t.pack.ToUpper().Equals(opts.Version.ToUpper()));

            if (_profileparmeters == null)
            {
                Console.WriteLine("No mount point defined for this version");
                return -1;
            }

            filelist = Ftp.FileList(_profileparmeters.mountPoint, remotePath, _profileparmeters.user, _profileparmeters.password);

            List<string> newFiles = filelist.FindAll(t => (t.Contains("." + opts.FileName + ".")));

            filelist.Add(opts.FileName);
            Ftp.DownloadFiles(_profileparmeters.mountPoint, newFiles, _profileparmeters.user, _profileparmeters.password, Path.Combine(Directory.GetCurrentDirectory()));
            Ftp.DeleteFiles(_profileparmeters.mountPoint, remotePath, newFiles, _profileparmeters.user, _profileparmeters.password);

            if (newFiles.Count > 0)
            {
                string filename = new FileInfo(newFiles[0]).Name;
                var tempVar = filename.Split('.').ToList();
                tempVar.RemoveRange(tempVar.Count - 2, 2);
                testcasename = tempVar.Last();

                Console.WriteLine("Processing : {0} : {1}", filename, testcasename);

                workflowname = string.Join(".", tempVar);
                unprocessedWorkflows.AddRange(newFiles);
                AcceleratorXmlParser parser = new AcceleratorXmlParser(testcasename);
                parser.ProcessXmlFiles(newFiles);

                if (string.IsNullOrEmpty(parser.Error))
                {
                    AcceleratorMongo workflowMongo = new AcceleratorMongo(_profileparmeters.mongoUrl)
                    {
                        Version = opts.Version,
                        WorkflowName = workflowname,
                        Functions = parser.Functions
                    };
                    workflowMongo.UpdateWorkflows();


                    processedfiles.Add(filename);
                }

                UpdateLatestRun(workflowname, _profileparmeters.version);
            }

            return 0;


        }

        static int ProcessSingleTest(ProcessSingleTest opts)
        {
            List<ProfileParameters> profileparmeters = JsonConvert.DeserializeObject<List<ProfileParameters>>(File.ReadAllText("ProfileParameters.json"));
            ProfileParameters _profileparmeters = profileparmeters.Find(t => t.pack.ToUpper().Equals(opts.Version.ToUpper()));

            if (_profileparmeters == null)
            {
                Console.WriteLine("No mount point defined for this version");
                return 1;
            }
            else
            {
                if (opts.TestStatus == "passed")
                {
                    UpdateWorkFlows update = new UpdateWorkFlows();
                    update.Product = opts.Product;
                    update.Version = _profileparmeters.pack;
                    update.FileName = opts.FileName;
                    return UpdateWorkFlows(update);
                }
                else
                {
                    List<string> filelist = new List<string>();
                    List<string> newFiles = filelist.FindAll(t => (t.Contains(opts.FileName)));
                    filelist = Ftp.FileList(_profileparmeters.mountPoint, "", _profileparmeters.user, _profileparmeters.password);
                    filelist.Add(opts.FileName);
                    Ftp.DeleteFiles(_profileparmeters.mountPoint, "", newFiles, _profileparmeters.user, _profileparmeters.password);
                }
            }

            return 0;
        }

        public static void UpdateLatestRun(string workflowName, string version,string lastupdated = "lastupdated")
        {
            try
            {
                string mongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
                string timestamp = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                AcceleratorMongo acceleratorMongo = new AcceleratorMongo(mongoUrl, lastrun: lastupdated)
                {
                    WorkflowName = workflowName,
                    Version = version,
                    LastExecuted = timestamp
                };
                acceleratorMongo.UpdateLatestRun();
            }
            catch (Exception excp)
            {
                Console.WriteLine("Exception occurred in UpdateLatestRun", excp);
            }
        }

        public static List<zTest> getImpactedZephyrTests(List<string> impactedTests, List<zTest> sourceList)
        {
            List<zTest> returnList = new List<zTest>();

            foreach (string oneTest in impactedTests)
            {
                var singleZephyrTest = sourceList.Find(t => t.fields.ScriptID.Equals(oneTest));
                if (singleZephyrTest != null)
                {
                    returnList.Add(singleZephyrTest);
                }
            }

            return returnList;
        }

        public static List<string> GetRelevantTestsToExecute(List<string> impactedTests, List<zTest> sourceList)
        {
            List<zTest> returnList = new List<zTest>();
            List<zTest> tmpList = new List<zTest>();
            List<string> testNamesList = new List<string>();

            foreach (string oneTest in impactedTests)
            {
                var singleZephyrTest = sourceList.Find(t => t.fields.ScriptID.Equals(oneTest.Split('.').Last()));
                if (singleZephyrTest != null)
                {
                    returnList.Add(singleZephyrTest);
                }
            }

            //group by submodule
            Dictionary<string, List<zTest>> impactedTestsbySubModule = returnList.GroupBy(t => t.fields.labels[0]).ToDictionary(tt => tt.Key, tt => tt.ToList());
            Dictionary<string, List<zTest>> testsToExecutebySubmodule = sourceList.GroupBy(t => t.fields.labels[0]).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in impactedTestsbySubModule.Keys)
            {

                List<zTest> fromProfiler = impactedTestsbySubModule[submodule];
                List<zTest> fromZephyr = testsToExecutebySubmodule[submodule];

                if (fromZephyr.Count > 20 && fromProfiler.Count > 10)
                {
                    if ((fromProfiler.Count * 100) / fromZephyr.Count > 25)
                    {
                        int testsToConsider = (int)(fromProfiler.Count * 0.2);
                        tmpList.AddRange(fromProfiler.Take(testsToConsider));
                        //Console.WriteLine(" {0} : {1} : {2} : {3}", submodule, fromProfiler.Count, fromZephyr.Count, testsToConsider);
                    }
                    else
                    {
                        tmpList.AddRange(fromProfiler);
                        //Console.WriteLine(" {0} : {1} : {2} : {3}", submodule, fromProfiler.Count, fromZephyr.Count, fromProfiler.Count);
                    }
                }
                else
                {
                    tmpList.AddRange(fromProfiler);
                    //Console.WriteLine(" {0} : {1} : {2} : {3} - tests less than 20", submodule, fromProfiler.Count, fromZephyr.Count, fromProfiler.Count);
                }

            }


            foreach (zTest tmpTest in tmpList)
            {
                testNamesList.Add(tmpTest.fields.ScriptID);
            }


            Console.WriteLine("Output of the Optimization of test selection  {0} : {1}", impactedTests.Count, testNamesList.Count);

            return testNamesList;
        }

        public static int CreateDashboardProfiler(CreateDashboardProfilerOptions opts)
        {
            string product = "Drive";
            string pack = opts.Pack;

            DashboardConnector connector = new DashboardConnector();
            MongoDriver driver = new MongoDriver();

            string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + pack + "-" +
                                 DateTime.Now.ToString("hhmmsstt");
            SchedulerLogger.Log(testSetName, "Started creating Dashboard with name" + testSetName);
            RestCall call;
            zapi zephyrApi = new zapi("userapi", "userapi123", "https://projects.cdk.com");
            string codeVersion = connector.CodeVersion(product, pack);
            Dictionary<string, object> create = new Dictionary<string, object>();

            AcceleratorMongo acm = new AcceleratorMongo(MongoUrl);
            List<ProfileData> allCommits = acm.getProfilerDataForMaster();
            allCommits.RemoveAll(tt => tt.Tests.Count == 0);

            if (allCommits.Count == 0)
            {
                Console.WriteLine("There are no impacted test against the commits...");
                acm.MoveToProcessed(testSetName, "master");
                return -1;
            }
            
            List<zTest> testsToExecute;
            if (opts.CreateWithoutJiraApi.ToLower().Equals("true"))
            {
                testsToExecute =
                    driver.zephyrTests.GetRegressionTestsByModule(codeVersion, pack, "Aftersales,Vehicles,CRM,Accounts,Environment");
            }
            else
            {
                testsToExecute =
                    zephyrApi.getRegressionTestsByModule(codeVersion, pack, "Aftersales,Vehicles,CRM,Accounts,Environment");
            }

            Dictionary<string, List<ProfileData>> profileDatabyStory =
                allCommits.GroupBy(t => t.Issue).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string jiraStory in profileDatabyStory.Keys.Distinct())
            {
                Dictionary<string, List<ProfileData>> profileDatabyUser = profileDatabyStory[jiraStory]
                    .GroupBy(t => t.Author).ToDictionary(tt => tt.Key, tt => tt.ToList());
                foreach (string jiraUser in profileDatabyUser.Keys.Distinct())
                {
                    Dictionary<string, List<ProfileData>> profileDatabyCommit = profileDatabyUser[jiraUser]
                        .GroupBy(t => t.Hash).ToDictionary(tt => tt.Key, tt => tt.ToList());
                    foreach (string stashCommit in profileDatabyCommit.Keys.Distinct())
                    {
                        List<string> tests = new List<string>();
                        foreach (ProfileData tmp in profileDatabyCommit[stashCommit].GroupBy(t => t.Hash).First())
                        {

                            //Filter out tests if they are in same submodule and huge numbers > 50%
                            List<string> OptimizedTests = GetRelevantTestsToExecute(tmp.Tests, testsToExecute);
                            tests.AddRange(OptimizedTests);
                            foreach (string currentTest in OptimizedTests)
                            {
                                //get the Zephyr representation of the test
                                zTest testToExecute = testsToExecute.Find(t =>
                                    t.fields.ScriptID.Equals(currentTest.Split('.').Last()));
                                if (testToExecute != null)
                                {

                                    int iterator = 0;
                                    //testToExecute.fields.SuiteID = stashCommit;

                                    int history = 0;
                                    int counter = 0;
                                    int avgDuration = 900;

                                    create.Clear();
                                    create.Add("name", testToExecute.fields.ScriptID);
                                    create.Add("summary", testToExecute.fields.summary);
                                    create.Add("testid", testToExecute.id);
                                    string description = string.Empty;
                                    if (!string.IsNullOrEmpty(testToExecute.fields.description))
                                        description = Regex.Replace(testToExecute.fields.description, @"[^a-zA-Z0-9 ]",
                                            "",
                                            RegexOptions.Compiled);
                                    create.Add("description", description);
                                    create.Add("status", "No Run");
                                    create.Add("testsetid", "-1");
                                    create.Add("duration", "0");
                                    create.Add("host", "Not Coded");
                                    create.Add("author", testToExecute.fields.creator != null ? testToExecute.fields.creator.displayName : "ivcauto");
                                    create.Add("created", jiraStory);
                                    create.Add("runner", "Default");

                                    //create.Add("F2US", testToExecute.fields.labels[0]);
                                    create.Add("F2US", stashCommit + string.Format("({0})", jiraUser.Split('<')[0]));

                                    create.Add("counter", counter);
                                    create.Add("history", history);
                                    create.Add("avgduration", avgDuration);
                                    create.Add("IVUS", testToExecute.key);

                                    //create.Add("module", jiraStory);
                                    create.Add("module", testToExecute.fields.components[0].name);

                                    //create.Add("submodule", jiraUser.Split('<')[0]);
                                    create.Add("submodule", testToExecute.fields.labels[0]);


                                    create.Add("logs", new Jenkins().TestScheduled());
                                    create.Add("success", "ivctest");

                                    if (testToExecute.fields.ScriptID.ToLower().Contains("runlast"))
                                    {
                                        create.Add("suitename", "RunLast");
                                    }
                                    else
                                    {
                                        var suiteName = testToExecute.fields.SuiteID ?? testToExecute.fields.ScriptID;
                                        create.Add("suitename", suiteName);
                                    }

                                    create.Add("testsetname", testSetName);
                                    create.Add("packname", pack);
                                    call = new RestCall()
                                    {
                                        Url = string.Format("{0}/results/insert/", SchedulerBase.mongoRest)
                                    };
                                    call.Post(create);
                                    create.Clear();
                                    iterator++;
                                }
                            }
                        }

                        Console.WriteLine("{0} : {1} : {2} : {3}", jiraStory, jiraUser.Split('<')[0], stashCommit,
                            tests.Count);
                    }
                }

            }

            create.Clear();

            #region Get tests of previous dashboard and post them to newly created dashboard

            if (Convert.ToBoolean(opts.AddOldTests))
            {
                string previousTestSetName = connector.GetTestsetNameForPack(product, pack);
                List<ivc_test_result> oldTests = connector.GetValidTestsFromDashboard(product, pack, previousTestSetName);

                Dictionary<string, List<ivc_test_result>> testsBySubModule =
                    oldTests.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());

                foreach (string submodule in testsBySubModule.Keys)
                {
                    SchedulerLogger.Log(testSetName, string.Format("Adding submodule :{0} tests to Dasboard : {1}", submodule, testsBySubModule[submodule].Count));
                    foreach (var testToExecute in testsBySubModule[submodule])
                    {
                        int history = 0;
                        int counter = 0;
                        int avgDuration = 900;

                        create.Clear();
                        create.Add("name", testToExecute.name);
                        create.Add("summary", testToExecute.summary);
                        create.Add("testid", testToExecute.testid);

                        create.Add("description", testToExecute.description);
                        create.Add("status", "No Run");
                        create.Add("testsetid", "-1");
                        create.Add("duration", "0");

                        create.Add("host", "Not Coded");

                        create.Add("author", testToExecute.author != null ? testToExecute.author : "ivcauto");
                        create.Add("created", testToExecute.created);
                        create.Add("runner", "Default");
                        create.Add("F2US", "9999");
                        create.Add("counter", counter);
                        create.Add("history", history);
                        create.Add("avgduration", avgDuration);
                        create.Add("IVUS", testToExecute.IVUS);
                        create.Add("module", testToExecute.module);
                        create.Add("submodule", testToExecute.submodule);
                        create.Add("logs", new Jenkins().TestScheduled());
                        create.Add("success", "ivctest");
                        if (testToExecute.name.ToLower().Contains("runlast"))
                        {
                            create.Add("suitename", "RunLast");
                        }
                        else
                        {
                            var suiteName = testToExecute.suitename ?? testToExecute.name;
                            create.Add("suitename", suiteName);
                        }
                        create.Add("testsetname", testSetName);
                        create.Add("packname", pack);
                        call = new RestCall() { Url = string.Format("{0}/results/insert/", mongoRest) };
                        call.Post(create);
                    }
                }

            }

            #endregion

            create.Clear();

            #region get the count of unique tests

            List<ivc_test_result> allProfilerTests =
                connector.GetAllTestsFromDashboard(product, pack, testSetName);

            List<ivc_test_result> allUniqueTests = new List<ivc_test_result>();

            foreach (var test in allProfilerTests)
            {
                List<ivc_test_result> tempTest = allUniqueTests.FindAll(t => t.name == test.name);
                if (tempTest.Count == 0)
                {
                    allUniqueTests.Add(test);
                }
            }

            Dictionary<string, List<ivc_test_result>> uniqueTestsBySubmodule =
                allUniqueTests.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in uniqueTestsBySubmodule.Keys)
            {
                if (uniqueTestsBySubmodule[submodule].Count > 0)
                {
                    SchedulerLogger.Log(testSetName,
                        string.Format("Added {0} unique test cases for {1} sumbodule",
                            uniqueTestsBySubmodule[submodule].Count, submodule));
                }

            }

            #endregion

            #region Update the releases table and recent_releases table

            ////create.Add("updateid",
            //    GetInstalledRpmNumber(connector.GetAllAppServersForPackSetup("Drive", pack)
            //        .First().hostname));

            create.Add("systemversion", string.Format("{0} Unique Tests", allUniqueTests.Count));
            create.Add("ivccodeversion", "MT");
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "Drive-" + pack);
            create.Add("testsetid", "-1");
            create.Add("packname", pack);
            create.Add("product", product);
            create.Add("date", testSetName.Split('_')[0]);
            create.Add("file_created", DateTime.Now.ToString("s"));
            create.Add("last_modified", DateTime.Now.ToString("s"));
            create.Add("last_modified_notification", "");
            call = new RestCall()
            {
                Url = string.Format("{0}/releases/insert/?data={1}", SchedulerBase.mongoRest,
                    JsonConvert.SerializeObject(create))
            };
            call.Post(create);
            SchedulerLogger.Log(testSetName, "Updated the release table with test set name");

            #endregion

            #region now move the records to processed

            acm.MoveToProcessed(testSetName, "master");

            #endregion

            return 0;
        }

        public static int CreateDashboardProfiler_RingRelease(CreateDashboardRingReleaseOptions opts)
        {
            string product = opts.Product;
            string pack = opts.Pack;

            //define variables
            DashboardConnector connector = new DashboardConnector();
            MongoDriver driver = new MongoDriver();
            RestCall call;
            zapi zephyrApi = new zapi("userapi", "userapi123", "https://projects.cdk.com");

            //identify the pack & code version
            ivc_pack_details targetPack = driver.Releases.GetPackInformation(pack);
            string codeVersion = targetPack.ivccodeversion;

            //create test set name
            string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + pack + "-" +
                                 DateTime.Now.ToString("hhmmsstt");
            SchedulerLogger.Log(testSetName, "Started creating Dashboard with name" + testSetName);
            
            Dictionary<string, object> create = new Dictionary<string, object>();

            AcceleratorMongo acm = new AcceleratorMongo(MongoUrl);
            List<ProfileData> allCommits = acm.getProfilerDataForRR(targetPack.updateid);
            allCommits.RemoveAll(tt => tt.Tests.Count == 0);

            //get the data from Zephyr for further work
            List<zTest> testsToExecute = zephyrApi.getRegressionTestsByModule(codeVersion, pack, "Aftersales,Vehicles,CRM,Accounts,Environment");


            #region Identify and create tests from profiler
            //Check if there are any tests identified related to the commits made
            if (allCommits.Count == 0)
            {
                Console.WriteLine("There are no impacted test against the commits...");
                acm.MoveToProcessed(testSetName, targetPack.updateid);
            }
            else
            {
                Dictionary<string, List<ProfileData>> profileDatabyStory =
                    allCommits.GroupBy(t => t.Issue).ToDictionary(tt => tt.Key, tt => tt.ToList());

                foreach (string jiraStory in profileDatabyStory.Keys.Distinct())
                {
                    Dictionary<string, List<ProfileData>> profileDatabyUser = profileDatabyStory[jiraStory]
                        .GroupBy(t => t.Author).ToDictionary(tt => tt.Key, tt => tt.ToList());
                    foreach (string jiraUser in profileDatabyUser.Keys.Distinct())
                    {
                        Dictionary<string, List<ProfileData>> profileDatabyCommit = profileDatabyUser[jiraUser]
                            .GroupBy(t => t.Hash).ToDictionary(tt => tt.Key, tt => tt.ToList());
                        foreach (string stashCommit in profileDatabyCommit.Keys.Distinct())
                        {
                            List<string> tests = new List<string>();
                            foreach (ProfileData tmp in profileDatabyCommit[stashCommit].GroupBy(t => t.Hash).First())
                            {

                                //Filter out tests if they are in same submodule and huge numbers > 50%
                                List<string> OptimizedTests = GetRelevantTestsToExecute(tmp.Tests, testsToExecute);
                                tests.AddRange(OptimizedTests);
                                foreach (string currentTest in OptimizedTests)
                                {
                                    //get the Zephyr representation of the test
                                    zTest testToExecute = testsToExecute.Find(t =>
                                        t.fields.ScriptID.Equals(currentTest.Split('.').Last()));
                                    if (testToExecute != null)
                                    {

                                        int iterator = 0;
                                        //testToExecute.fields.SuiteID = stashCommit;

                                        int history = 0;
                                        int counter = 0;
                                        int avgDuration = 900;

                                        create.Clear();
                                        create.Add("name", testToExecute.fields.ScriptID);
                                        create.Add("summary", testToExecute.fields.summary);
                                        create.Add("testid", testToExecute.id);
                                        string description = string.Empty;
                                        if (!string.IsNullOrEmpty(testToExecute.fields.description))
                                            description = Regex.Replace(testToExecute.fields.description, @"[^a-zA-Z0-9 ]",
                                                "",
                                                RegexOptions.Compiled);
                                        create.Add("description", description);
                                        create.Add("status", "No Run");
                                        create.Add("testsetid", "-1");
                                        create.Add("duration", "0");
                                        create.Add("host", "Not Coded");
                                        create.Add("author", testToExecute.fields.creator != null ? testToExecute.fields.creator.displayName : "ivcauto");
                                        create.Add("created", jiraStory);
                                        create.Add("runner", "Default");

                                        //create.Add("F2US", testToExecute.fields.labels[0]);
                                        create.Add("F2US", stashCommit + string.Format("({0})", jiraUser.Split('<')[0]));

                                        create.Add("counter", counter);
                                        create.Add("history", history);
                                        create.Add("avgduration", avgDuration);
                                        create.Add("IVUS", testToExecute.key);

                                        //create.Add("module", jiraStory);
                                        create.Add("module", testToExecute.fields.components[0].name);

                                        //create.Add("submodule", jiraUser.Split('<')[0]);
                                        create.Add("submodule", testToExecute.fields.labels[0]);


                                        create.Add("logs", new Jenkins().TestScheduled());
                                        create.Add("success", "ivctest");

                                        if (testToExecute.fields.SuiteID != null)
                                            create.Add("suitename", testToExecute.fields.SuiteID);
                                        else
                                            create.Add("suitename", testToExecute.fields.ScriptID);

                                        create.Add("testsetname", testSetName);
                                        create.Add("packname", pack);
                                        call = new RestCall()
                                        {
                                            Url = string.Format("{0}/results/insert/", SchedulerBase.mongoRest)
                                        };
                                        call.Post(create);
                                        create.Clear();
                                        iterator++;
                                    }
                                }
                            }

                            Console.WriteLine("{0} : {1} : {2} : {3}", jiraStory, jiraUser.Split('<')[0], stashCommit,
                                tests.Count);
                        }
                    }

                }
            }
            create.Clear();
            #endregion

            #region Get tests of previous dashboard and post them to newly created dashboard

            string previousTestSetName = connector.GetTestsetNameForPack(product, pack);
            List<ivc_test_result> oldTests = connector.GetValidTestsFromDashboard(product, pack, previousTestSetName);

            Dictionary<string, List<ivc_test_result>> testsBySubModule =
                oldTests.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in testsBySubModule.Keys)
            {
                SchedulerLogger.Log(testSetName, string.Format("Adding submodule :{0} tests to Dashboard : {1}", submodule, testsBySubModule[submodule].Count));
                foreach (var testToExecute in testsBySubModule[submodule])
                {
                    int history = 0;
                    int counter = 0;
                    int avgDuration = 900;

                    create.Clear();
                    create.Add("name", testToExecute.name);
                    create.Add("summary", testToExecute.summary);
                    create.Add("testid", testToExecute.testid);

                    create.Add("description", testToExecute.description);
                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");

                    create.Add("host", "Not Coded");

                    create.Add("author", testToExecute.author != null ? testToExecute.author : "ivcauto");
                    create.Add("created", testToExecute.created);
                    create.Add("runner", "Default");
                    create.Add("F2US", "9999");
                    create.Add("counter", counter);
                    create.Add("history", history);
                    create.Add("avgduration", avgDuration);
                    create.Add("IVUS", testToExecute.IVUS);
                    create.Add("module", testToExecute.module);
                    create.Add("submodule", testToExecute.submodule);
                    create.Add("logs", new Jenkins().TestScheduled());
                    create.Add("success", "ivctest");
                    if (testToExecute.name.ToLower().Contains("runlast"))
                    {
                        create.Add("suitename", "RunLast");
                    }
                    else
                    {
                        var suiteName = testToExecute.suitename ?? testToExecute.name;
                        create.Add("suitename", suiteName);
                    }
                    create.Add("testsetname", testSetName);
                    create.Add("packname", pack);
                    call = new RestCall() { Url = string.Format("{0}/results/insert/", mongoRest) };
                    call.Post(create);
                }
            }

            create.Clear();

            #endregion

            #region add basic sanity tests

            List<ScheduledTestInformation> sanitytests = connector.GetScheduledTestsforScrumAutomation(product, pack, "Sanity");
            SchedulerLogger.Log(testSetName,"Get the List of Sanity tests for " + opts.Pack);
            List<string> TestNameList = new List<string>();
            foreach (ScheduledTestInformation singleBlock in sanitytests)
            {
                foreach (var testSuite in singleBlock.tests)
                {
                    foreach (var singleTest in testSuite.qctests)
                    {
                        TestNameList.Add(singleTest.scriptid);
                    }
                }
            }
            SchedulerLogger.Log(testSetName, "Extract the list of Zephyr test names");            
            zephyrApi = new zapi(jiraUserName, jiraPassword, "https://projects.cdk.com");
            List<zTest> zephyrTests = zephyrApi.getTestsByScriptIds(TestNameList, "Automated");
            SchedulerLogger.Log(testSetName, "Pulling all automated test cases from Zephyr");
            call = new RestCall();            
            Dictionary<string, List<zTest>> testsBySubModuleforsanity = zephyrTests.GroupBy(t => t.fields.labels[0]).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in testsBySubModuleforsanity.Keys)
            {
                SchedulerLogger.Log(testSetName, string.Format("Adding submodule :{0} tests to Dasboard : {1}", submodule, testsBySubModuleforsanity[submodule].Count));
                foreach (var testToExecute in testsBySubModuleforsanity[submodule])
                {
                    int history = 0;
                    int counter = 0;
                    int avgDuration = 900;

                    create.Clear();
                    create.Add("name", testToExecute.fields.ScriptID);
                    create.Add("summary", testToExecute.fields.summary);
                    create.Add("testid", testToExecute.id);
                    create.Add("description", testToExecute.fields.description);
                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");
                    create.Add("host", "Not Coded");
                    create.Add("author", testToExecute.fields.creator != null ? testToExecute.fields.creator.displayName : "ivcauto");
                    create.Add("created", "Sanity");
                    create.Add("runner", "Default");
                    create.Add("F2US", testToExecute.fields.components[0].name);
                    create.Add("counter", counter);
                    create.Add("history", history);
                    create.Add("avgduration", avgDuration);
                    create.Add("IVUS", testToExecute.key);
                    create.Add("module", testToExecute.fields.components[0].name);
                    create.Add("submodule", testToExecute.fields.labels[0]);
                    create.Add("logs", new Jenkins().TestScheduled());
                    create.Add("success", "ivctest");
                    if (testToExecute.fields.ScriptID.ToLower().Contains("runlast"))
                    {
                        create.Add("suitename", "RunLast");
                    }
                    else
                    {
                        var suiteName = testToExecute.fields.SuiteID ?? testToExecute.fields.ScriptID;
                        create.Add("suitename", suiteName);
                    }
                    create.Add("testsetname", testSetName);
                    create.Add("packname", pack);
                    call = new RestCall() { Url = string.Format("{0}/results/insert/", mongoRest) };
                    call.Post(create);
                }
            }
            create.Clear();

            #endregion

            #region get the count of unique tests

            List<ivc_test_result> allProfilerTests =
                connector.GetAllTestsFromDashboard(product, pack, testSetName);

            List<ivc_test_result> allUniqueTests = new List<ivc_test_result>();

            foreach (var test in allProfilerTests)
            {
                List<ivc_test_result> tempTest = allUniqueTests.FindAll(t => t.name == test.name);
                if (tempTest.Count == 0)
                {
                    allUniqueTests.Add(test);
                }
            }

            Dictionary<string, List<ivc_test_result>> uniqueTestsBySubmodule =
                allUniqueTests.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in uniqueTestsBySubmodule.Keys)
            {
                if (uniqueTestsBySubmodule[submodule].Count > 0)
                {
                    SchedulerLogger.Log(testSetName,
                        string.Format("Added {0} unique test cases for {1} sumbodule",
                            uniqueTestsBySubmodule[submodule].Count, submodule));
                }

            }

            #endregion

            #region Update the releases table and recent_releases table                     
           
            ivc_recent_releases recentRelease = new ivc_recent_releases();
            recentRelease = connector.RecentReleases(opts.Product, opts.Pack, "Installed");
            if (recentRelease == null)
                recentRelease = connector.RecentReleases(opts.Product, opts.Pack, "Picked");
            string systemid = recentRelease.system;
            string relid = recentRelease.relid;
            string date = recentRelease.date;
            string updatedes = recentRelease.updatedesc;
            string updateid = recentRelease.updateid;
            create.Add("systemversion", systemid);
            create.Add("updateid", updateid);
            create.Add("updatedesc", updatedes);
            create.Add("updatedon", date);
            create.Add("systemversion", string.Format("{0} Unique Tests", allUniqueTests.Count));
            create.Add("ivccodeversion", opts.Pack.ToLower().Contains("mt") ? "MT" : codeVersion);
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "Drive-" + pack);
            create.Add("testsetid", "-1");
            create.Add("packname", pack);
            create.Add("product", product);
            create.Add("date", testSetName.Split('_')[0]);
            create.Add("file_created", DateTime.Now.ToString("s"));
            create.Add("last_modified", DateTime.Now.ToString("s"));
            create.Add("last_modified_notification", "");

            if ((!opts.Pack.ToLower().Contains("mt") && (!opts.Pack.ToLower().Contains("preprod"))))
            {
                connector.UpdateRecentReleaseStatus(opts.Product, opts.Pack);
                SchedulerLogger.Log(testSetName, "Updated the recent_releases table with 'Triggered' status");
            }

            call = new RestCall()
            {
                Url = string.Format("{0}/releases/insert/?data={1}", SchedulerBase.mongoRest,
                    JsonConvert.SerializeObject(create))
            };
            call.Post(create);
            SchedulerLogger.Log(testSetName, "Updated the release table with test set name");

            #endregion            

            return 0;
        }
        public static int MoveTestsToProcessed(MoveTestsettoProcessed opts)
        {
            AcceleratorMongo acm = new AcceleratorMongo(MongoUrl);
            acm.MoveToProcessed(opts.Testsetname, opts.UpdateID);
            Console.WriteLine("Moved tests of " + opts.Testsetname + " and Update ID "+ opts.UpdateID+" to processed");
            return 0;
        }
        public static int PreProdProfilerCreateDashboard()
        {
            string product = "Drive";
            string pack = "preprod";

            DashboardConnector connector = new DashboardConnector();
            MongoDriver driver = new MongoDriver();

            List<sConfig> items = JsonConvert
                .DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"))
                .FindAll(t => t.sequential.Equals(true));

            string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + pack + "-" +
                                 DateTime.Now.ToString("hhmmsstt");
            SchedulerLogger.Log(testSetName, "Started creating Dashboard with name" + testSetName);
            RestCall call;
            zapi zephyrApi = new zapi("userapi", "userapi123", "https://projects.cdk.com");
            string codeVersion = connector.CodeVersion(product, pack);
            Dictionary<string, object> create = new Dictionary<string, object>();

            AcceleratorMongo acm = new AcceleratorMongo(MongoUrl);
            List<ProfileData> allCommits = acm.getProfilerDataForPreprod();
            allCommits.RemoveAll(tt => tt.Tests.Count == 0);

            if (allCommits.Count == 0)
            {
                Console.WriteLine("There are no impacted test against the commits...");
                acm.MoveToProcessed(testSetName, "preprod");
                return -1;
            }

            List<string> impactedTestsList = new List<string>();
            foreach (var commit in allCommits)
            {
                foreach (var test in commit.Tests)
                {
                    impactedTestsList.Add(test.Split('.').Last());
                }
            }
            List<zTest> testsToExecute = zephyrApi.getTestsByScriptIds(impactedTestsList, "Automated");
            
            testsToExecute.AddRange(zephyrApi.getTestsByScriptIds(impactedTestsList, "Merged"));

            Dictionary<string, List<zTest>> testsBySubModule = testsToExecute.GroupBy(t => t.fields.labels[0]).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in testsBySubModule.Keys)
            {
                SchedulerLogger.Log(testSetName, string.Format("Adding submodule :{0} tests to Dasboard : {1}", submodule, testsBySubModule[submodule].Count));
                foreach (var testToExecute in testsBySubModule[submodule])
                {
                    int history = 0;
                    int counter = 0;
                    int avgDuration = 900;

                    create.Clear();
                    create.Add("name", testToExecute.fields.ScriptID);
                    create.Add("summary", testToExecute.fields.summary);
                    create.Add("testid", testToExecute.id);
                    string description = string.Empty;
                    if (!string.IsNullOrEmpty(testToExecute.fields.description))
                        description = Regex.Replace(testToExecute.fields.description, @"[^a-zA-Z0-9 ]", "",
                            RegexOptions.Compiled);
                    create.Add("description", description);
                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");
                    create.Add("host", "Not Coded");
                    create.Add("author", testToExecute.fields.creator != null ? testToExecute.fields.creator.displayName : "ivcauto");
                    create.Add("created", testToExecute.fields.versions[0].name);
                    create.Add("runner", "Default");
                    create.Add("F2US", "9999");
                    create.Add("counter", counter);
                    create.Add("history", history);
                    create.Add("avgduration", avgDuration);
                    create.Add("IVUS", testToExecute.key);
                    create.Add("module", testToExecute.fields.components[0].name);
                    create.Add("submodule", testToExecute.fields.labels[0]);
                    create.Add("logs", new Jenkins().TestScheduled());


                    if (items.Find(t => t.submodule.Equals(testToExecute.fields.labels[0], StringComparison.InvariantCultureIgnoreCase)) != null)
                    {
                        create.Add("success", testToExecute.fields.labels[0]);
                    }
                    else
                    {
                        create.Add("success", "ivctest");
                    }

                    if (testToExecute.fields.ScriptID.ToLower().Contains("runlast"))
                    {
                        create.Add("suitename", "RunLast");
                    }
                    else
                    {
                        var suiteName = testToExecute.fields.SuiteID ?? testToExecute.fields.ScriptID;
                        create.Add("suitename", suiteName);
                    }
                    create.Add("testsetname", testSetName);
                    create.Add("packname", pack);
                    call = new RestCall() { Url = string.Format("{0}/results/insert/", mongoRest) };
                    call.Post(create);
                }
            }

            create.Clear();

            #region get the count of unique tests

            List<ivc_test_result> allProfilerTests =
                connector.GetAllTestsFromDashboard(product, pack, testSetName);

            List<ivc_test_result> allUniqueTests = new List<ivc_test_result>();

            foreach (var test in allProfilerTests)
            {
                List<ivc_test_result> tempTest = allUniqueTests.FindAll(t => t.name == test.name);
                if (tempTest.Count == 0)
                {
                    allUniqueTests.Add(test);
                }
            }

            Dictionary<string, List<ivc_test_result>> uniqueTestsBySubmodule =
                allUniqueTests.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in uniqueTestsBySubmodule.Keys)
            {
                if (uniqueTestsBySubmodule[submodule].Count > 0)
                {
                    SchedulerLogger.Log(testSetName,
                        string.Format("Added {0} unique test cases for {1} sumbodule",
                            uniqueTestsBySubmodule[submodule].Count, submodule));
                }

            }

            #endregion

            #region Update the releases table and recent_releases table

            ////create.Add("updateid",
            //    GetInstalledRpmNumber(connector.GetAllAppServersForPackSetup("Drive", pack)
            //        .First().hostname));

            create.Add("systemversion", string.Format("{0} Unique Tests", allUniqueTests.Count));
            create.Add("ivccodeversion", "MT");
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "Drive-" + pack);
            create.Add("testsetid", "-1");
            create.Add("packname", pack);
            create.Add("product", product);
            create.Add("date", testSetName.Split('_')[0]);
            create.Add("file_created", DateTime.Now.ToString("s"));
            create.Add("last_modified", DateTime.Now.ToString("s"));
            create.Add("last_modified_notification", "");
            call = new RestCall()
            {
                Url = string.Format("{0}/releases/insert/?data={1}", SchedulerBase.mongoRest,
                    JsonConvert.SerializeObject(create))
            };
            call.Post(create);
            SchedulerLogger.Log(testSetName, "Updated the release table with test set name");

            #endregion

            #region now move the records to processed

            acm.MoveToProcessed(testSetName, "preprod");

            #endregion

            return 0;
        }

        public static int ProfilerTriggerTests()
        {
            string product = "Drive";
            string pack = "MT";

            Jenkins jenkins = new Jenkins();
            DashboardConnector connector = new DashboardConnector();

            releaseinformation releaseDetails = connector.GetReleaseInformationForProductAndPack(product, pack);
            int DelayExpected = 30;
            List<string> ivcmodules = new List<string>() { "Vehicles", "CRM", "Aftersales", "Accounts", "Environment" };
            foreach (string module in ivcmodules)
            {
                Dictionary<string, string> jobParams = new Dictionary<string, string>();
                jobParams.Add("product", product);
                jobParams.Add("pack", pack);
                jobParams.Add("module", module);
                SchedulerLogger.Log(releaseDetails.testsetname, string.Format("Triggering re-run for module : {0} by delay : {1}sec", module, DelayExpected));
                jenkins.TriggerJobWithDelay("REGRESSION_NIGHTLY_RUN_MODULE", Convert.ToString(DelayExpected), jobParams);
                DelayExpected = DelayExpected + 60;
            }


            return 0;
        }

        public static int ProfilerSingleTest(ProfilerSingleTestOptions opts)
        {
            DashboardConnector connector = new DashboardConnector();

            //get the release details
            ivc_pack_details targetPack = connector.GetPackDetails(opts.TestSetName);
            var test = connector.GetTestDetailsbyTestName(opts.Test, opts.TestSetName);
            SchedulerLogger.Log(opts.TestSetName,
                string.Format("Execution started for single test : {0} on appserver : {1}", opts.Test, opts.AppServer));
            if (test != null)
            {
                ivc_appserver serverDetails =
                    connector.GetServerKCMLService(targetPack.product, targetPack.packname, opts.AppServer);
                sConfig item = JsonConvert
                    .DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"))
                    .Find(yy => yy.submodule.Equals(test.submodule));

                //get and unzip the test libraries
                SchedulerBase.Regression_GetTestLibraries(targetPack.ivccodeversion);

                //update the configuration file
                SchedulerBase.ScrumAutomation_UpdateConfigurationFile(serverDetails.hostname, serverDetails.service,
                    opts.TestSetName, serverDetails.kpath, serverDetails.serverurl, serverDetails.portno, targetPack);

                Console.WriteLine("Output is : {0}", File.ReadAllText("TestLibraries\\Drive.config"));

                ////create Nunit Runlist file
                SchedulerBase.CreateNUnitRunList(opts.Test);

                int timeout = Convert.ToInt32(test.avgduration * 2);
                if (timeout < 180)
                    timeout = 240;

                //Trigger the execution
                SchedulerBase.TriggerExecution(targetPack.packname, serverDetails.hostname, opts.TestSetName, opts.Test,
                    item.submodule, item.subscribers, timeout * 1000);
            }

            SchedulerBase.KillKcmlProcess(opts.AppServer);
            return 0;
        }

        static void CreateVersionInLastUpdated(string testsetname, string sourceVersion, string targetVersion)
        {
            DashboardConnector dc = new DashboardConnector();
            List<ivc_test_result> allTests = dc.GetAllTestsFromDashboard("Drive", "Pilot", testsetname);

            AcceleratorMongo ac = new AcceleratorMongo(MongoUrl);
            List<LatestRun> allRuns = ac.getLastRunStatus(sourceVersion);

            List<LatestRun> insertOnes = new List<LatestRun>();

            foreach (ivc_test_result tmp in allTests)
            {
                var currentOne = allRuns.Find(tt => tt.WorkflowName.ToLower().Contains(tmp.name.ToLower()));

                if (currentOne != null)
                {
                    LatestRun tmpRun = new LatestRun();
                    tmpRun.WorkflowName = currentOne.WorkflowName;
                    tmpRun.Version = targetVersion;
                    tmpRun.LastExecuted = "Not updated";
                    insertOnes.Add(tmpRun);
                }
            }

            ac.AddLastRuns(insertOnes);

        }

        static void TransformWorkFlows()
        {

            //AcceleratorMongo ac = new AcceleratorMongo(MongoUrl);
            //List<Workflow> workFlows = ac.GetWorkflowsForVersion("MT");
            //Dictionary<string, List<Workflow>> workFlowsbyFunction = workFlows.GroupBy(t=>t.FunctionName).ToDictionary(tt => tt.Key, tt => tt.ToList());
            //int iterator = 1;
            //int totalCount = workFlowsbyFunction.Keys.Count;
            //foreach (string function in workFlowsbyFunction.Keys)
            //{
            //    Workflow nw = new Workflow();
            //    nw.FunctionName = function;
            //    nw.Version = "MT";
            //    nw.Workflows = new List<string>();
            //    var tests = workFlowsbyFunction[function].Select(tt => tt.WorkflowName).ToList();
            //    nw.Workflows.AddRange(tests);
            //    ac.InsertNewWorkFlow(nw);

            //    Console.WriteLine("{0} ({1}) : {2}", iterator++, totalCount, function);

            //}



        }

        static void CorrectLastUpdatedRecords(string version)
        {

            DashboardConnector dc = new DashboardConnector();
            string testsetname = dc.GetTestsetNameForPack("Drive", version);

            List<ivc_test_result> allTests = dc.GetAllTestsFromDashboard("Drive", version, testsetname);
            AcceleratorMongo ac = new AcceleratorMongo();
            List<LatestRun> allRuns = ac.getLastRunStatus(version);

            File.WriteAllText("LatestRun.json", JsonConvert.SerializeObject(allRuns));


            List<LatestRun> NewRuns = new List<LatestRun>();

            foreach (ivc_test_result tmp in allTests)
            {
                LatestRun tmpRun = allRuns.Find(t => t.WorkflowName.Contains(tmp.name));
                if (tmpRun != null)
                {
                    LatestRun newOne = new LatestRun();
                    newOne.WorkflowName = tmpRun.WorkflowName;
                    newOne.LastExecuted = tmpRun.LastExecuted;
                    newOne.Version = version.ToUpper();
                    NewRuns.Add(newOne);
                }
            }

            //Delete old records
            var recordsToRemove = Builders<LatestRun>.Filter.Where(t => t.Version == version.ToUpper());
            ac.DeleteManyLastRun(recordsToRemove);

            //Insert New valid records
            ac.InsertManyLastRun(NewRuns);





        }

        public static int ChangeWorkFlowVersionCodeCut()
        {
            AcceleratorMongo ac = new AcceleratorMongo();
            ac.WorkFlowsChangeVersionCodeCut();
            return 0;


        }

        private static void SendMail(string htmlBody, string sendTo, string version)
        {
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("EnhancedProfiler@cdk.com");
            string[] splitemail = sendTo.Split(';');
            foreach (string tmp in splitemail)
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    mail.To.Add(tmp);
                }
            }
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;

            //clause for differentiating hyd and gbh smtp servers
            if (Environment.MachineName.ToLower().Contains("gbh"))
                client.Host = "dsirelay.gbh.dsi.adp.com";
            else
                client.Host = "139.126.11.110";

            client.Host = "dsirelay.gbh.dsi.adp.com";
            mail.Subject = String.Format("Enhanced Profiler| Trace Status | {0}", version);
            mail.Body = htmlBody;
            mail.IsBodyHtml = true;
            client.Send(mail);
        }

        public static int TestsnotUpdatedwithProfiler(TestsnotUpdatedwithProfiler opts)
        {
            MongoDriver driver = new MongoDriver();
            string testSetName = driver.Releases.GetTestSetName("Drive", opts.Version);
            List<ivc_test_result> allTests = driver.Results.GetAllTests(testSetName);
            Dictionary<string, List<ivc_test_result>> allTestsbyModule = allTests.GroupBy(t => t.module).ToDictionary(tt => tt.Key, tt => tt.ToList());
            List<ivc_test_result> sendmail = new List<ivc_test_result>();

            AcceleratorMongo ac = new AcceleratorMongo(MongoUrl);
            List<LatestRun> allRuns = ac.getLastRunStatus(opts.Version.ToUpper());           
            List<string> NeverRun = allRuns.FindAll(t => t.LastExecuted.ToLower() == "not updated").Select(o=>o.WorkflowName).ToList();
            allRuns.RemoveAll(t=>t.LastExecuted.ToLower() == "not updated");
            List<string> updatedWorkflows = allRuns.Select(o => o.WorkflowName).ToList();

            List<ivc_test_result> notUpdated = allTests.FindAll(t => (updatedWorkflows.Find(x => x.Contains(t.name)) == null));

            Dictionary<string, List<ivc_test_result>> byModule = notUpdated.GroupBy(t => t.module).ToDictionary(tt => tt.Key, tt => tt.ToList());

            StringBuilder htmlBody = new StringBuilder();

            htmlBody.AppendLine("<html><body>");
            htmlBody.AppendLine("<table width=\"75%\" border = \"3\" style = \"float:left;\">");
            htmlBody.AppendLine("<col width=20%>");
            htmlBody.AppendLine("<col width=20%>");
            htmlBody.AppendLine("<col width=20%>");
            htmlBody.AppendLine("<col width=20%>");
            htmlBody.AppendLine("<col width=20%>");
            htmlBody.AppendLine("<tr>");

            foreach (string module in allTestsbyModule.Keys)
            {
                htmlBody.AppendFormat("<th style=\"background-color: darkkhaki\"><span style = \"font: size 25;font-family: 'Courier New', Courier, monospace, bold\"><b>{0}</b></span></th>", module);
            }

            htmlBody.AppendLine("<tr>");
            foreach (string module in allTestsbyModule.Keys)
            {
                htmlBody.AppendFormat("<td><font color=\"blue\">Total Tests :{0}</font></td>", allTestsbyModule[module].Count);
            }
            htmlBody.AppendLine("</tr>");

            htmlBody.AppendLine("<tr>");
            foreach (string module in allTestsbyModule.Keys)
            {
                int source = allTestsbyModule[module].Count;
                int destCount = byModule.Keys.Contains(module) ? byModule[module].Count : 0;
                
                htmlBody.AppendFormat("<td><font color=\"green\">Traced Tests :{0}</font></td>", source - destCount);
            }
            htmlBody.AppendLine("</tr>");

            htmlBody.AppendLine("<tr>");
            foreach (string module in allTestsbyModule.Keys)
            {
                int destCount = byModule.Keys.Contains(module) ? byModule[module].Count : 0;
                htmlBody.AppendFormat("<td><font color=\"red\">Missing Tests :{0}</font></td>", destCount);
            }
            htmlBody.AppendLine("</tr>");

            htmlBody.AppendLine("<tr>");
            foreach (string module in allTestsbyModule.Keys)
            {
                List<string> sb = new List<string>();
                if (byModule.Keys.Contains(module))
                {
                    foreach (ivc_test_result tmp in byModule[module])
                    {
                        sb.Add(tmp.name + "<br>");
                    }
                }
                htmlBody.AppendFormat("<td valign=\"top\" style=\"white-space:pre-wrap;word-wrap:break-word\">{0}</td>", String.Join("", sb));
            }
            htmlBody.AppendLine("</tr>");
            htmlBody.AppendLine("</table></body></html>");

            SendMail(htmlBody.ToString(), opts.Subscribers, opts.Version);

            return 0;
        }
        public static int GetIssuesList(GetIssuesList opts)
        {           
            MongoConnector mg = new MongoConnector(database: "accelerator");
            string ringmasterAPI = mg.getRelaseQaAPI(opts.Pack);            
            RestCall restCall = new RestCall() { Url = ringmasterAPI };
            string jsonoutput = restCall.Get();
            var releaseqa = JsonConvert.DeserializeObject<List<ReleaseQA>>(jsonoutput);
            List<ReleaseQA> _issuedetails = releaseqa.FindAll(t => (t.updateId == opts.updateId));
            List<string> issues = new List<string>();
            foreach (var issue in _issuedetails)
            {
                issues.Add(issue.issuedetails);
            }
            mg.updateIssueDetails(opts.updateId, issues);
            return 0;
        }

        #region Methods for DMS Lite

        public static int CreateDashboardDMSLite(CreateDashboardDMSLiteOptions opts)
        {
            string product = "Drive";
            string pack = opts.Pack;

            DashboardConnector connector = new DashboardConnector();
            MongoDriver driver = new MongoDriver();

            string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + pack + "-" +
                                 DateTime.Now.ToString("hhmmsstt");
            SchedulerLogger.Log(testSetName, "Started creating Dashboard with name" + testSetName);
            RestCall call;
            zapi zephyrApi = new zapi("userapi", "userapi123", "https://projects.cdk.com");
            string codeVersion = connector.CodeVersion(product, pack);
            Dictionary<string, object> create = new Dictionary<string, object>();

            Dictionary<string, List<string>> validTests = GetDashboardTestsForDMSLite();

            List<zTest> testsToExecute = driver.zephyrTests.GetRegressionTestsByModule(codeVersion, opts.Pack, "Aftersales,Vehicles,CRM,Accounts,Environment");
            //List<zTest> testsToExecute = zephyrApi.getRegressionTestsByModule(codeVersion, pack, "Aftersales,Vehicles,CRM,Accounts,Environment");

            if (validTests["Execute"] != null && validTests["Execute"].Count > 0)
                addSingleTest(testSetName, opts.Pack, validTests["Execute"], testsToExecute);

            if (validTests["Progress"] != null && validTests["Progress"].Count > 0)
                addSingleTest(testSetName, opts.Pack, validTests["Progress"], testsToExecute, false);


            //Add the testset name
            ivc_recent_releases recentRelease = new ivc_recent_releases();
            create.Add("ivccodeversion", opts.Pack.ToLower().Contains("mt") ? "MT" : codeVersion);
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "Drive-" + opts.Pack);
            create.Add("testsetid", "-1");
            create.Add("packname", opts.Pack);
            create.Add("product", product);
            create.Add("date", testSetName.Split('_')[0]);
            create.Add("file_created", DateTime.Now.ToString("s"));
            create.Add("last_modified", DateTime.Now.ToString("s"));
            create.Add("last_modified_notification", "");
            call = new RestCall()
            {
                Url = string.Format("{0}/releases/insert/?data={1}", mongoRest, JsonConvert.SerializeObject(create))
            };
            call.Post(create);
            SchedulerLogger.Log(testSetName, "Updated the release table with test set name");

            return 0;
        }
        public static void addSingleTest(string testSetName, string packname, List<string> Tests, List<zTest> testsToExecute, bool executeMode = true)
        {
            Dictionary<string, object> create = new Dictionary<string, object>();
            foreach (string currentTest in Tests)
            {
                //get the Zephyr representation of the test
                zTest testToExecute = testsToExecute.Find(t =>
                    t.fields.ScriptID.Equals(currentTest.Split('.').Last()));
                if (testToExecute != null)
                {

                    int iterator = 0;
                    //testToExecute.fields.SuiteID = stashCommit;

                    int history = 0;
                    int counter = 0;
                    int avgDuration = 900;

                    create.Clear();
                    create.Add("name", testToExecute.fields.ScriptID);
                    create.Add("summary", testToExecute.fields.summary);
                    create.Add("testid", testToExecute.id);
                    string description = string.Empty;
                    if (!string.IsNullOrEmpty(testToExecute.fields.description))
                        description = Regex.Replace(testToExecute.fields.description, @"[^a-zA-Z0-9 ]", "",
                            RegexOptions.Compiled);
                    create.Add("description", description);
                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");
                    create.Add("host", "Not Coded");
                    create.Add("author", testToExecute.fields.creator != null ? testToExecute.fields.creator.displayName : "ivcauto");
                    create.Add("created", testToExecute.fields.versions[0].name);
                    create.Add("runner", "Default");
                    create.Add("F2US", "9999");
                    create.Add("counter", counter);
                    create.Add("history", history);
                    create.Add("avgduration", avgDuration);
                    create.Add("IVUS", testToExecute.key);

                    if (executeMode)
                    {
                        create.Add("module", testToExecute.fields.components[0].name);
                        create.Add("submodule", testToExecute.fields.labels[0]);
                        var suiteName = testToExecute.fields.SuiteID ?? testToExecute.fields.ScriptID;
                        create.Add("suitename", suiteName);
                    }
                    else
                    {
                        create.Add("submodule", testToExecute.fields.components[0].name);
                        create.Add("module", "In Progress");
                        create.Add("suitename", testToExecute.fields.labels[0]);
                    }

                    create.Add("logs", new Jenkins().TestScheduled());
                    create.Add("success", "ivctest");
                    
                    
                    create.Add("testsetname", testSetName);
                    create.Add("packname", packname);
                    RestCall call = new RestCall() { Url = string.Format("{0}/results/insert/", mongoRest) };
                    call.Post(create);
                    iterator++;
                }
            }
        }
        public static Dictionary<string, List<string>> GetDashboardTestsForDMSLite()
        {
            Dictionary<string, List<string>> relevantTests = new Dictionary<string, List<string>>();

            relevantTests.Add("Execute", new List<string>());
            relevantTests.Add("Progress", new List<string>());

            string DMSLiteFolder = @"C:\webroot\Autoline_Drive\DMSLITE";
            //string DMSLiteFolder = Directory.GetCurrentDirectory();

            foreach (string testfile in Directory.GetFiles(DMSLiteFolder, "DMSLite.Tests.*"))
            {
                string fileContent = File.ReadAllText(testfile);

                if (!string.IsNullOrEmpty(fileContent))
                {
                    DMSLiteTests dt = JsonConvert.DeserializeObject<DMSLiteTests>(File.ReadAllText(testfile));

                    if (dt.Execute != null && dt.Execute.Count > 0)
                        relevantTests["Execute"].AddRange(dt.Execute);
                    if (dt.Progress != null && dt.Progress.Count > 0)
                        relevantTests["Progress"].AddRange(dt.Progress);
                }
            }

            return relevantTests;

        }

        #endregion


    }
}
    


