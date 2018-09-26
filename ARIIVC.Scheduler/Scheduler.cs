using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Net.Mail;
using zephyrapi;
using System.Web;
using ARIIVC.Logger;
using ARIIVC.Scheduler;
using ARIIVC.Scheduler.JsonReps;
using CommandLine;
using Microsoft.Win32;
using System.IO.Compression;
using System.Runtime.Remoting;
using CommandLine.Text;
using Newtonsoft.Json.Linq;
using ARIIVC.Utilities;
using ARIIVC.Utilities.JsonRepo;

namespace IVCScheduler
{

    public class Scheduler
    {
        static int Main(string[] args)
        {

            try
            {
                return Parser.Default.ParseArguments<CreateDashboardOptions,
                        InstallCheckOptions,
                        TriggerModuleOptions,
                        TriggerOrderedSubmodules,
                        TriggerSubModuleOptions,
                        TriggerNightlyRegression,
                        RunSingleTest,
                        RunSequentialTest,
                        RegressionSetLabels,
                        PackSetupOptions,
                        CheckforReleasesOptions,
                        SiteStatusOptions,
                        ReleaseTesting,
                        RunDeploymentTestsOptions, 
                        AddBCTSToReleaseDashBoardOptions, 
                        InfosysSmokeTrigger                   
                        >(args)
                    .MapResult(
                        (CreateDashboardOptions opts) => SchedulerBase.CreateDashboard(opts),
                        (InstallCheckOptions opts) => SchedulerBase.InstallCheck(opts),
                        (TriggerModuleOptions opts) => SchedulerBase.TriggerModule(opts),
                        (TriggerOrderedSubmodules opts) => SchedulerBase.IVCRegression_TriggeredOrderedSubmodules(opts),
                        (TriggerSubModuleOptions opts) => SchedulerBase.TriggerSubModule(opts),
                        (TriggerNightlyRegression opts) => SchedulerBase.IVCRegression_TriggerNightlyRegression(opts),
                        (RunSingleTest opts) => SchedulerBase.IVCRegression_Run(opts),
                        (RunSequentialTest opts) => SchedulerBase.IVCRegression_RunSequentialSubmodule(opts),
                        (RegressionSetLabels opts) => SchedulerBase.SetMachineLabels(),
                        (PackSetupOptions opts) => SchedulerBase.RegressionPackSetup(opts),
                        (CheckforReleasesOptions opts) => SchedulerBase.CheckforReleases(),
                        (SiteStatusOptions opts) => SchedulerBase.ReleaseTestingSiteStatusUpdate(opts),
                        (ReleaseTesting opts) => SchedulerBase.ReleaseTesting(opts),
                        (RunDeploymentTestsOptions opts) => SchedulerBase.RunDeploymentTests(opts),
                        (AddBCTSToReleaseDashBoardOptions opts) => SchedulerBase.AddBCTSToReleaseDashBoard(opts),
                        (InfosysSmokeTrigger opts) => SchedulerBase.InfosysTriggerSmokeTests(opts),               
                        errs => 1);
                //RunSingleBvtOptions ,(RunSingleBvtOptions opts) => SchedulerBase.RegressionRunSingleBvt(opts),
            }
            catch (Exception eeObj)
            {
                Console.WriteLine("Exception in Scheduler program : {0}", eeObj.Message);
                Console.WriteLine("Exception in Scheduler program : {0}", eeObj.StackTrace);
                return 1;
            }
        }
    }
}

namespace ARIIVC.Scheduler
{
    public static class ListExtensions
    {
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
    public static class ZipArchiveExtensions
    {
        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                string directory = Path.GetDirectoryName(completeFileName);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (file.Name != "")
                    file.ExtractToFile(completeFileName, true);
            }
        }
    }

    public class SchedulerBase
    {
        public static string dashboardserver = "gbhpdslivcweb01.dsi.ad.adp.com";
        public static string mongoRest = "http://" + dashboardserver + "/ivc/rest/";
        public static string jenkinsURL = "http://c05drddrv89.dslab.ad.adp.com:8080";
        public static string csdtRestURL = "http://gbhpcsdtrep01.gbh.dsi.adp.com/csdt/rest/";
        public static string almuser = "ivcauto2";
        public static string SscriptID, scriptversion, JiraRunStat;
        public static string almpassword = "testing";
        public static string jirauser = "svc_autoline_ivc";
        public static string jirapassword = "6xL@tCdw]/";
        public static string Drive = "Drive";
        public static string Drive_Sprint = "Drive%20Sprint%20Teams";
        public static string ReleaseEngineeringJenkinsUrl = "http://100.124.198.66:8080";
        public static string HyderabadJenkinsUrl = "http://139.126.80.68:8080";
        zapi zp = new zapi(jirauser, jirapassword);
        public static Dictionary<string, string> relMaps = new Dictionary<string, string>();
        public static SchedulerLogger schedulerLogger = new SchedulerLogger();

        public static int InstallCheck(InstallCheckOptions opts)
        {
            DashboardConnector connector = new DashboardConnector();
            // Verify if product / pack combination is installed
            if (!opts.Pack.ToLower().Contains("mt"))
            {
                if (connector.CheckInstalledRelease(opts.Product, opts.Pack, "Installed") || connector.CheckPickedRelease(opts.Product, opts.Pack))
                {
                    Console.WriteLine("Install check is completed.");
                    return 0;
                }
                else
                    return -1;
            }
            else
            {
                Console.WriteLine("No release install check for MT");
            }
            return 0;
        }
        public static int CheckforReleases()
        {
            DashboardConnector connector = new DashboardConnector();            
            List<RingReleaseInfo> packstocheck =
                JsonConvert.DeserializeObject<List<RingReleaseInfo>>(File.ReadAllText("CheckReleasePacks.json"));            
            foreach (var pack in packstocheck)
            {
                string Version = connector.GetPackSystemVersion(pack.product, pack.packname);
                string UpdateID = connector.CheckReleaseCreation(pack.product, Version, "Installed", pack.packname);
                if (UpdateID != "")
                {
                    Console.WriteLine("Created check is completed for " + pack.packname + " and update ID is " + UpdateID);                    
                    //update installed to picked
                    connector.UpdateRecentReleaseStatustoPicked(pack.product, pack.packname);

                    Dictionary<string, string> jenkinsJobParams = new Dictionary<string, string>();
                    jenkinsJobParams.Add("product", pack.product);
                    jenkinsJobParams.Add("pack", pack.packname);
                    jenkinsJobParams.Add("updateid", UpdateID);                  

                    Jenkins jenkins = new Jenkins(jenkinsURL);
                    jenkins.TriggerJob("RR_REGRESSION_Update_ImpactedTests", jenkinsJobParams);
                }
                else
                {
                    Console.WriteLine("No new release Installed for " + pack.packname, Version);                   
                }
            }  
            return 0;
        }        
        public static int TriggerTest(TriggerTest options)
        {
            int appserver = 0;
            DashboardConnector connector = new DashboardConnector();
            var testResults = connector.GetValidTestsForSubModuleFromDashboard(options.Product, options.Pack, options.SubModule);
            testResults.RemoveAll(t => t.name.ToLower().Contains("runlast") || t.suitename.ToLower().Contains("runlast"));
            testResults = testResults.OrderBy(t => t.history).ThenBy(t => t.avgduration).ToList();
            List<sConfig> subModuleConfigs = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"));
            sConfig subModuleConfig = subModuleConfigs.Find(t => t.submodule.Equals(options.SubModule));
            if (subModuleConfig.sequential)
            {
                List<string> testnames = new List<string>();
                testResults.ForEach(t => testnames.Add(t.suitename));
                var testsCommaSeparated = String.Join(",", testnames);
                RunSingleTestSuite(options.Product, options.Pack, options.SubModule, testsCommaSeparated, appserver);
            }
            else
            {


                foreach (var subModuleTest in testResults)
                {
                    RunSingleTestSuite(options.Product, options.Pack, options.SubModule, subModuleTest.suitename, appserver++);
                }
            }
            return 0;
        }
        public static void RunSingleTestSuite(string product, string ivcpack, string submodule, string testsuite, int iterator, string label = "ivctest")
        {
            //Variable declaration
            List<string> appservers = new List<string>();
            List<sConfig> items = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"));
            sConfig currentConfig = items.Find(t => t.submodule.Equals(submodule));
            RestCall rc = new RestCall();

            #region Identify the Appservers
            List<string> appserverlist = new List<string>();
            string appurl = mongoRest + "scheduler/appservers?query={\"active\":true," + "\"runconfig\":\"" + currentConfig.runconfig + "\"" + "," + "\"packname\":\"" + ivcpack + "\" }";
            rc.Url = appurl;
            dynamic app_th = Newtonsoft.Json.Linq.JValue.Parse(rc.Get());
            foreach (var appserver1 in app_th)
            {
                appserverlist.Add(Convert.ToString(appserver1["hostname"]));
            }
            #endregion

            DashboardConnector connector = new DashboardConnector();
            string version = connector.GetTestsetNameForPack(product, ivcpack);
            string testset = connector.GetPackVersion(product, ivcpack);
            string appserver = appserverlist[iterator % appserverlist.Count];

            Dictionary<string, string> jenkinsJobParams = new Dictionary<string, string>();
            jenkinsJobParams.Add("product", product);
            jenkinsJobParams.Add("packname", ivcpack);
            jenkinsJobParams.Add("appserver", appserver);
            jenkinsJobParams.Add("testname", testsuite);
            jenkinsJobParams.Add("version", version);
            jenkinsJobParams.Add("label", label);
            jenkinsJobParams.Add("targetusers", currentConfig.subscribers);
            jenkinsJobParams.Add("submodule", submodule);

            Jenkins jenkins = new Jenkins(jenkinsURL);
            jenkins.TriggerJob("REGRESSION_RUN_SUITES", jenkinsJobParams);
            System.Threading.Thread.Sleep(500);
            iterator++;
        }
        public static int ReserveSlave(ReserveSlaveOptions opts)
        {
            Jenkins jenkins = new Jenkins();
            DashboardConnector connector = new DashboardConnector();
            RestCall call = new RestCall();
            List<jenkinsNode> availableNodes = jenkins.getIdleNodes();
            List<machineallocation> machines = JsonConvert.DeserializeObject<List<machineallocation>>(File.ReadAllText("machineallocation.json"));
            int readyNodes = availableNodes.Count;
            foreach (machineallocation machine in machines.OrderBy(t => t.machines))
            {
                int forthis = Convert.ToInt16(readyNodes * machines.Find(t => t.module.Equals(machine.module)).machines / 100);
                List<jenkinsNode> currentNodes = availableNodes.Take(forthis).ToList();
                availableNodes.RemoveRange(0, currentNodes.Count);
                jenkins.changeNodeLabel(currentNodes, machine.module);
            }

            return 0;
        }
        public static int CreateDashboard(CreateDashboardOptions opts)
        {

            List<sConfig> items = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties")).FindAll(t => t.sequential.Equals(true));

            DashboardConnector connector = new DashboardConnector();
            MongoDriver driver = new MongoDriver();
            // Create test set in MongoDB through Dashboard REST API
            string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + opts.Pack + "-" +
                                 DateTime.Now.ToString("hhmmsstt");

            SchedulerLogger.Log(testSetName, "Started creating Dashboard with name" + testSetName);
            RestCall call = new RestCall();

            // Get tests from Zephyr
            zapi zephyrApi = new zapi(jirauser, jirapassword, "https://projects.cdk.com");
            string codeVersion = connector.CodeVersion(opts.Product, opts.Pack);

            jProject jp = zephyrApi.getProject("IDRIVE");

            jVersion currentVersion = jp.versions.Find(t => t.name.Contains(codeVersion));
            SchedulerLogger.Log(testSetName, "Getting the Current Version from Zephyr");
            string zephyrCodeVersion = currentVersion.name;

            List<zTest> testsToExecute;

            if (opts.CreateWithoutJiraApi.ToLower().Equals("true"))
            {
                testsToExecute = driver.zephyrTests.GetRegressionTestsByModule(codeVersion, opts.Pack, opts.Modules, opts.IncludeRrtScripts);
            }
            else
            {
                testsToExecute = opts.IncludeRrtScripts.Equals("true") ? zephyrApi.getRegressionTestsWithRrt(zephyrCodeVersion, opts.Pack, opts.Modules) : zephyrApi.getRegressionTestsByModule(zephyrCodeVersion, opts.Pack, opts.Modules);
            }
            
            if (opts.Pack != "Pilot")
            {
                testsToExecute.RemoveAll(t => t.fields.ScriptID.Contains("WSIA"));
            }

            //List<zTest> testsToExecute = zephyrApi.Get(zephyrCodeVersion, opts.Pack);
            SchedulerLogger.Log(testSetName, "Pulling test cases from Zephyr according to Version");

            //List<ExecutionBaseData> basedata = connector.GetBaseData(opts.Pack);
            Dictionary<string, object> create = new Dictionary<string, object>();

            int iterator = 0;

            #region Post each test to MongoDB

            #region Correct if SuiteID is not there
            foreach (zTest tmp in testsToExecute)
            {
                if (string.IsNullOrEmpty(tmp.fields.SuiteID))
                {
                    tmp.fields.SuiteID = tmp.fields.ScriptID;
                }
            }
            #endregion


            Dictionary<string, List<zTest>> testsBySubModule = testsToExecute.GroupBy(t => t.fields.labels[0]).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in testsBySubModule.Keys)
            {
                SchedulerLogger.Log(testSetName, string.Format("Adding submodule :{0} tests to Dasboard : {1}", submodule, testsBySubModule[submodule].Count));
                foreach (var testToExecute in testsBySubModule[submodule])
                {
                    int history = 0;
                    int counter = 0;
                    int avgDuration = 180;

                    avgDuration = driver.Results.FindLastSuccessDuration(testToExecute.fields.ScriptID,opts.Pack);

                    //ExecutionBaseData currentTestData = basedata.Find(t => t.testname.Equals(testToExecute));
                    //if (currentTestData != null)
                    //{
                    //    avgDuration = currentTestData.averageDuration;
                    //    history = currentTestData.history;
                    //}

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

                    //List<ivc_appserver> shortListed = appservers.FindAll(t => t.module.Contains(testToExecute.fields.components[0].name));
                    //ivc_appserver appserver = shortListed[iterator % shortListed.Count];
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
                    create.Add("packname", opts.Pack);
                    call = new RestCall() { Url = string.Format("{0}/results/insert/", mongoRest) };
                    call.Post(create);
                    iterator++;
                }
            }
            #endregion


            create.Clear();

            #region Update the releases table and recent_releases table
            ivc_recent_releases recentRelease = new ivc_recent_releases();

            if ((!opts.Pack.ToLower().Contains("mt") && (!opts.Pack.ToLower().Contains("preprod"))))
            {
                recentRelease = connector.RecentReleases(opts.Product, opts.Pack, "Installed");
                if(recentRelease == null)
                    recentRelease = connector.RecentReleases(opts.Product, opts.Pack, "Picked");
                string systemid = recentRelease.system;
                codeVersion = systemid.Replace("-01", string.Empty).Replace("N", string.Empty).Replace("-", ".");
                string relid = recentRelease.relid;
                string date = recentRelease.date;
                string updatedes = recentRelease.updatedesc;
                string updateid = recentRelease.updateid;
                create.Add("systemversion", systemid);
                //create.Add("alternateid", buildinfo.SelectSingleNode("Rev8Version").InnerText);
                create.Add("updateid", updateid);
                create.Add("updatedesc", updatedes);
                create.Add("updatedon", date);
                connector.UpdateRecentReleaseStatus(opts.Product, opts.Pack);
                SchedulerLogger.Log(testSetName, "Updated the recent_releases table with 'Triggered' status");

            }
            create.Add("ivccodeversion", opts.Pack.ToLower().Contains("mt") ? "MT" : codeVersion);
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "Drive-" + opts.Pack);
            create.Add("testsetid", "-1");
            create.Add("packname", opts.Pack);
            create.Add("product", opts.Product);
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
            #endregion

            return 0;
        }
        public static int CreateDashboardFromExisting(CreateDashboardOptions opts)
        {

            List<sConfig> items = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties")).FindAll(t => t.sequential.Equals(true));

            DashboardConnector connector = new DashboardConnector();
            // Create test set in MongoDB through Dashboard REST API
            string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + opts.Pack + "-" +
                                 DateTime.Now.ToString("hhmmsstt");
            StringBuilder testNameYml = new StringBuilder();
            testNameYml.AppendLine("# testnames.yml");
            testNameYml.AppendLine("TESTSETNAME:");
            testNameYml.AppendLine("- " + testSetName);
            testNameYml.AppendLine("TEST_NAMES:");
            StringBuilder runLastYml = new StringBuilder();
            runLastYml.AppendLine("exclude:");

            SchedulerLogger.Log(testSetName, "Started creating Dashboard");
            RestCall call = new RestCall();

            //get relevant appserver
            List<ivc_appserver> appservers = connector.GetAllAppServersForPackSetup(opts.Product, opts.Pack);

            // Get tests from Zephyr
            //zapi zephyrApi = new zapi(jirauser, jirapassword, "https://projects.cdk.com");
            string codeVersion = connector.CodeVersion(opts.Product, opts.Pack);

            //jProject jp = zephyrApi.getProject("IDRIVE");

            //jVersion currentVersion = jp.versions.Find(t => t.name.Contains(codeVersion));
            //SchedulerLogger.Log(testSetName, "Getting the Current Version from Zephyr");
            //string zephyrCodeVersion = "1.67";

            ////Uncomment this once done with testing 
            //List<zTest> testsToExecute = zephyrApi.getRegressionTestsByModule(zephyrCodeVersion, opts.Pack, opts.Modules);

            List<ivc_test_result> allTests1 = connector.GetAllTestsFromDashboard(opts.Product, opts.Pack, "18-09-2017_Pilot-111127PM");


            //List<zTest> testsToExecute = zephyrApi.Get(zephyrCodeVersion, opts.Pack);
            SchedulerLogger.Log(testSetName, "Pulling test cases from Zephyr according to Version");

            //List<ivc_test_result> basedata = connector.GetBaseData(opts.Pack);
            Dictionary<string, object> create = new Dictionary<string, object>();

            int iterator = 0;

            //#region Post each test to MongoDB

            #region Correct if SuiteID is not there
            //foreach (zTest tmp in testsToExecute)
            //{
            //    if (tmp.fields.SuiteID == null || tmp.fields.SuiteID == "")
            //    {
            //        tmp.fields.SuiteID = tmp.fields.ScriptID;
            //    }
            //}
            //#endregion


            Dictionary<string, List<ivc_test_result>> testsBySubModule = allTests1.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());

            foreach (string submodule in testsBySubModule.Keys)
            {
                SchedulerLogger.Log(testSetName, string.Format("Adding submodule :{0} tests to Dasboard : {1}", submodule, testsBySubModule[submodule].Count));
                foreach (var testToExecute in testsBySubModule[submodule])
                {
                    int history = 0;
                    int counter = 0;
                    int avgDuration = 60;
                    //if (basedata.Count != 0)
                    //{
                    //    List<ivc_test_result> allCurrentSuites =
                    //        basedata.FindAll(uu => uu.suitename.Equals(testToExecute.suitename));
                    //    foreach (ivc_test_result tmpresult in allCurrentSuites)
                    //    {
                    //        history = history + tmpresult.counter;
                    //        avgDuration = avgDuration + Convert.ToInt32(tmpresult.duration);
                    //    }
                    //    if (allCurrentSuites.Count > 0)
                    //    {
                    //        avgDuration = avgDuration / allCurrentSuites.Count;
                    //    }
                    //}
                    create.Clear();
                    create.Add("name", testToExecute.name);
                    create.Add("summary", testToExecute.summary);
                    create.Add("testid", testToExecute.testid);
                    string description = string.Empty;
                    if (!string.IsNullOrEmpty(testToExecute.description))
                        description = Regex.Replace(testToExecute.description, @"[^a-zA-Z0-9 ]", "",
                            RegexOptions.Compiled);
                    create.Add("description", description);
                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");

                    //List<ivc_appserver> shortListed = appservers.FindAll(t => t.module.Contains(testToExecute.fields.components[0].name));
                    //ivc_appserver appserver = shortListed[iterator % shortListed.Count];
                    create.Add("host", "Not Coded");

                    create.Add("author", testToExecute.author);
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


                    if (items.Find(t => t.submodule.Equals(testToExecute.submodule, StringComparison.InvariantCultureIgnoreCase)) != null)
                    {
                        create.Add("success", testToExecute.submodule);
                    }
                    else
                    {
                        create.Add("success", "ivctest");
                    }

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
                    create.Add("packname", opts.Pack);
                    call = new RestCall() { Url = string.Format("{0}/results/insert/", mongoRest) };
                    call.Post(create);
                    iterator++;
                }
            }
            #endregion


            create.Clear();

            #region Update the releases table and recent_releases table
            ivc_recent_releases recentRelease = new ivc_recent_releases();

            if (!opts.Pack.ToLower().Contains("mt"))
            {
                recentRelease = connector.RecentReleases(opts.Product, opts.Pack, "Installed");
                string systemid = recentRelease.system;
                codeVersion = systemid.Replace("-01", string.Empty).Replace("N", string.Empty).Replace("-", ".");
                string relid = recentRelease.relid;
                string date = recentRelease.date;
                string updatedes = recentRelease.updatedesc;
                string updateid = recentRelease.updateid;
                create.Add("systemversion", systemid);
                //create.Add("alternateid", buildinfo.SelectSingleNode("Rev8Version").InnerText);
                create.Add("updateid", updateid);
                create.Add("updatedesc", updatedes);
                create.Add("updatedon", date);
                connector.UpdateRecentReleaseStatus(opts.Product, opts.Pack);
                SchedulerLogger.Log(testSetName, "Updated the recent_releases table with 'Triggered' status");

            }
            create.Add("ivccodeversion", opts.Pack.ToLower().Contains("mt") ? "MT" : codeVersion);
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "Drive-" + opts.Pack);
            create.Add("testsetid", "-1");
            create.Add("packname", opts.Pack);
            create.Add("product", opts.Product);
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
            #endregion


            #region create YAML file
            List<string> validSuites = new List<string>();

            List<ivc_test_result> allTests = connector.GetValidTestsFromDashboard(opts.Product, opts.Pack, testSetName);

            allTests.RemoveAll(t => t.suitename.ToLower().Contains("runlast"));
            allTests.RemoveAll(t => t.name.ToLower().Contains("runlast"));

            Dictionary<string, List<ivc_test_result>> bysubModule = allTests.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());
            foreach (string submodule in bysubModule.Keys)
            {
                Dictionary<string, List<ivc_test_result>> bySuite = bysubModule[submodule].GroupBy(t => t.suitename).ToDictionary(tt => tt.Key, tt => tt.ToList());

                if (items.Find(t => t.submodule.Equals(submodule)) != null)
                {
                    validSuites.Add(string.Join(",", bySuite.Keys));
                }
                else
                {
                    foreach (string suiteid in bySuite.Keys)
                    {
                        validSuites.Add(suiteid);
                    }
                }
            }

            foreach (string suitename in validSuites)
            {
                testNameYml.AppendLine("- " + suitename);
            }

            //testNameYml.AppendLine(Convert.ToString(runLastYml));
            System.IO.File.WriteAllText("testnames.yml", testNameYml.ToString());


            #endregion

            return 0;
        }
        public static int ScrumAutomation_SingleBlock(ScrumAutomationRunOptions opts)
        {
            DashboardConnector connector = new DashboardConnector();

            //get the list of tests that are added in the scheduler
            List<ScheduledTestInformation> tests = connector.GetScheduledTestsforScrumAutomation(opts.Product, opts.Pack, opts.Group, opts.Name);

            //get and unzip the test libraries
            ScrumAutomation_GetTestLibraries();

            foreach (ScheduledTestInformation singleBlock in tests)
            {
                //get the service name based on appserver
                ivc_appserver serverDetails = connector.GetServerKCMLService(opts.Product, opts.Pack, singleBlock.appserver);

                //update the release information so that it is visible
                ScrumAutomation_updateReleaseInformation(opts.TestsetName, opts.Pack);

                //update the config file
                ScrumAutomation_UpdateConfigurationFile(singleBlock.appserver, serverDetails.service, opts.TestsetName, serverDetails.kpath, serverDetails.serverurl);

                //create Nunit Runlist file
                CreateNUnitRunList(singleBlock);

                //Trigger the execution
                //TriggerExecution(singleBlock);
            }

            return 0;
        }
        public static int IVCRegression_Run(RunSingleTest opts)
        {
            DashboardConnector connector = new DashboardConnector();

            //get the release details
            ivc_pack_details targetPack = connector.GetPackDetails(opts.TestSetName);
            var tests = connector.GetTestDetailsbySuiteName(opts.Test, opts.TestSetName);
            SchedulerLogger.Log(opts.TestSetName, string.Format("Execution started for single test : {0} on appserver : {1}", opts.Test, opts.AppServer));
            if (tests != null && tests.Count > 0)
            {
                Dictionary<string, List<ivc_test_result>> bySuite = tests.GroupBy(t => t.suitename).ToDictionary(tt => tt.Key, tt => tt.ToList());
                foreach (string suitename in bySuite.Keys)
                {
                    ivc_appserver serverDetails = connector.GetServerKCMLService(targetPack.product, targetPack.packname, opts.AppServer);
                    sConfig item = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties")).Find(yy => yy.submodule.Equals(bySuite[suitename].First().submodule));

                    //get and unzip the test libraries
                    Regression_GetTestLibraries(targetPack.ivccodeversion,targetPack.packname);

                    //update the configuration file
                    ScrumAutomation_UpdateConfigurationFile(serverDetails.hostname, serverDetails.service, opts.TestSetName, serverDetails.kpath, serverDetails.serverurl, serverDetails.portno, targetPack);

                    Console.WriteLine("Output is : {0}", File.ReadAllText("TestLibraries\\Drive.config"));

                    ////create Nunit Runlist file
                    CreateNUnitRunList(opts.Test);

                    //int timeout =  Convert.ToInt32(bySuite[suitename].Sum(t => t.avgduration));
                    MongoDriver driver = new MongoDriver();
                    int timeout = driver.Results.FindLastSuccessDuration(opts.Test, targetPack.packname);
                    Console.WriteLine("The Time out is calculated as : {0}", timeout);
                   
                    //Trigger the execution
                    TriggerExecution(targetPack.packname, serverDetails.hostname, opts.TestSetName, opts.Test, item.submodule, item.subscribers, (timeout*2));

                    KillKcmlProcess(opts.AppServer);
                }

            }
            
            return 0;
        }
        public static int IVCRegression_RunSequentialSubmodule(RunSequentialTest opts)
        {
            DashboardConnector connector = new DashboardConnector();
            sConfig item = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties")).Find(yy => yy.submodule.Equals(opts.Test));

            //get the release details
            ivc_pack_details targetPack = connector.GetPackDetails(opts.TestSetName);

            //get the appservers
            var tests = connector.GetValidTestsForSubModuleFromDashboard(opts.Test, opts.TestSetName);
            tests.RemoveAll(t => t.suitename.ToLower().Contains("runlast") || t.name.ToLower().Contains("runlast"));

            if (tests.Count > 0 && !string.IsNullOrEmpty(opts.AppServer))
            {
                ivc_appserver serverDetails = connector.GetServerKCMLService(targetPack.product, targetPack.packname, opts.AppServer);

                SchedulerLogger.Log(opts.TestSetName,
                    string.Format("Execution started for submodule : {0} on appserver : {1}", opts.Test, serverDetails.hostname));


                //get and unzip the test libraries
                Regression_GetTestLibraries(targetPack.ivccodeversion, targetPack.packname);

                //update the configuration file
                ScrumAutomation_UpdateConfigurationFile(serverDetails.hostname, serverDetails.service, opts.TestSetName, serverDetails.kpath, serverDetails.serverurl, serverDetails.portno, targetPack);

                //Now trigger the test one by one
                Dictionary<string, List<ivc_test_result>> bySuite = tests.GroupBy(t => t.suitename).ToDictionary(tt => tt.Key, tt => tt.ToList());
                foreach (string suitename in bySuite.Keys)
                {
                    ////create Nunit Runlist file
                    CreateNUnitRunList(suitename);
                    SchedulerLogger.Log(opts.TestSetName,  string.Format("Execution started for single test : {0} on appserver : {1}", suitename, serverDetails.hostname));

                    MongoDriver driver = new MongoDriver();
                    int timeout = driver.Results.FindLastSuccessDuration(suitename, targetPack.packname);
                    //Console.WriteLine("Time out for the test is : {0} : {1}", suitename, timeout);

                    //Trigger the execution
                    string currentDirectory = Directory.GetCurrentDirectory();
                    TriggerExecution(targetPack.packname, serverDetails.hostname, opts.TestSetName, suitename, item.submodule, item.subscribers, (timeout * 2));
                    killKclientProcess();
                    Directory.SetCurrentDirectory(currentDirectory);
                }
            }
            KillKcmlProcess(opts.AppServer);
            return 0;
        }
        public static int SetMachineLabels()
        {
            Jenkins JenkinsApi = new Jenkins();

            List<MachineDetails> machineDetails = JsonConvert.DeserializeObject<List<MachineDetails>>(File.ReadAllText("MachineDetails.json"));
            foreach (MachineDetails machineDetail in machineDetails)
            {
                foreach (string machineNumber in machineDetail.machines.Split(','))
                {
                    jenkinsNode currentNode = JenkinsApi.GetOneNode("gbh-ivc-pc" + machineNumber);
                    JenkinsApi.changeNodeLabel(currentNode, machineDetail.label);
                }
            }
            return 0;
        }
        public static int IVCRegression_TriggerNightlyRegression(TriggerNightlyRegression opts)
        {
            Jenkins jenkins = new Jenkins();
            DashboardConnector dc = new DashboardConnector();
            releaseinformation releaseDetails = dc.GetReleaseInformationForProductAndPack(opts.Product, opts.Pack);
            
            List<sConfig> items = JsonConvert
                .DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"))
                .FindAll(t => t.runconfig == "Core" || t.runconfig == "IA");
            Dictionary<int, List<sConfig>> sortedSubModules1 = items.GroupBy(t => t.order).ToDictionary(tt => tt.Key, tt => tt.ToList());
            SortedDictionary<int, List<sConfig>> sortedSubModules = new SortedDictionary<int, List<sConfig>>(sortedSubModules1);
            int DelayExpected = 600;
            foreach (int order in sortedSubModules.Keys)
            {
                Dictionary<string, string> jobParams = new Dictionary<string, string>();
                jobParams.Add("product", opts.Product);
                jobParams.Add("pack", opts.Pack);
                jobParams.Add("order", Convert.ToString(order));
                SchedulerLogger.Log(releaseDetails.testsetname, string.Format("Scheduling the submodules for order : {0} by delay : {1}sec", order, DelayExpected));
                jenkins.TriggerJobWithDelay("REGRESSION_TRIGGER_ORDERED_SUBMODULES", Convert.ToString(DelayExpected), jobParams);
                DelayExpected = DelayExpected + 1500;
            }

            List<string> ivcmodules = new List<string>() { "Vehicles", "CRM", "Aftersales", "Accounts" };
            foreach (string module in ivcmodules)
            {
                Dictionary<string, string> jobParams = new Dictionary<string, string>();
                jobParams.Add("product", opts.Product);
                jobParams.Add("pack", opts.Pack);
                jobParams.Add("module", module);
                SchedulerLogger.Log(releaseDetails.testsetname, string.Format("Triggering re-run for module : {0} by delay : {1}sec", module, DelayExpected));
                jenkins.TriggerJobWithDelay("REGRESSION_NIGHTLY_RUN_MODULE", Convert.ToString(DelayExpected), jobParams);
                DelayExpected = DelayExpected + 900;
            }

            List<Subscribers> subscribers = JsonConvert
                .DeserializeObject<List<Subscribers>>(File.ReadAllText("FrcSubscribers.json"));

            for (int i = 1; i <= 3; i++)
            {
                Dictionary<string, string> jobParams = new Dictionary<string, string>();
                jobParams.Add("product", opts.Product);
                jobParams.Add("pack", opts.Pack);
                jobParams.Add("subscribers", subscribers.Find(t => t.product.Equals(opts.Product)).emails);
                jobParams.Add("interval", Convert.ToString(i));
                SchedulerLogger.Log(releaseDetails.testsetname, string.Format("Triggering job to get Execution status by delay : {0}sec", DelayExpected));
                jenkins.TriggerJobWithDelay("REGRESSION_GET_FRC", Convert.ToString(DelayExpected), jobParams);
                DelayExpected = DelayExpected + 3600;
            }

            return 0;
        }
        public static int IVCRegression_TriggeredOrderedSubmodules(TriggerOrderedSubmodules opts)
        {
            DashboardConnector dc = new DashboardConnector();
            releaseinformation releaseDetails = dc.GetReleaseInformationForProductAndPack(opts.Product, opts.Pack);
            List<sConfig> items = JsonConvert
                .DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"))
                .FindAll(yy => yy.order.Equals(opts.Order));
            SchedulerLogger.Log(releaseDetails.testsetname, string.Format("Started execution for Order : {0}", opts.Order));
            Jenkins jenkins = new Jenkins();
            int iterator = 0;
            foreach (sConfig currentSubModule in items)
            {

                if (currentSubModule.sequential)
                {
                    List<ivc_appserver> allServers =
                        dc.GetAllAppServersForPackSetup(opts.Product, opts.Pack, currentSubModule.runconfig)
                            .FindAll(tt => tt.module.Contains(currentSubModule.module));

                    Dictionary<string, string> jobParams = new Dictionary<string, string>();
                    jobParams.Add("submodule", currentSubModule.submodule);
                    jobParams.Add("testset", releaseDetails.testsetname);
                    jobParams.Add("appserver", allServers[iterator % allServers.Count].hostname);
                    jenkins.TriggerJob("REGRESSION_NIGHTLY_RUN_SEQUENTIAL_SUBMODULE", jobParams);
                    iterator++;
                    SchedulerLogger.Log(releaseDetails.testsetname,
                        string.Format("Scheduled sequential submodule : {0}", currentSubModule.submodule));
                }
                else
                {
                    //get all the tests for the submodule
                    List<ivc_test_result> submoduletests =
                        dc.GetValidTestsForSubModuleFromDashboard(currentSubModule.submodule,
                            releaseDetails.testsetname);

                    submoduletests.RemoveAll(t =>
                        t.name.ToLower().Contains("runlast") || t.suitename.ToLower().Contains("runlast"));

                    Dictionary<string, List<ivc_test_result>> bySuite = submoduletests.GroupBy(t => t.suitename)
                        .ToDictionary(tt => tt.Key, tt => tt.ToList());
                    List<ivc_appserver> allServers =
                        dc.GetAllAppServersForPackSetup(opts.Product, opts.Pack, currentSubModule.runconfig)
                            .FindAll(tt => tt.module.Contains(currentSubModule.module));

                    iterator = 0;
                    foreach (string suitename in bySuite.Keys)
                    {
                        Dictionary<string, string> jobParams = new Dictionary<string, string>();
                        jobParams.Add("test", suitename);
                        jobParams.Add("testset", releaseDetails.testsetname);
                        jobParams.Add("appserver", allServers[iterator % allServers.Count].hostname);
                        jobParams.Add("label", currentSubModule.module.ToLower());
                        jenkins.TriggerJob("REGRESSION_NIGHTLY_RUN_SINGLE", jobParams);
                        iterator++;
                        SchedulerLogger.Log(releaseDetails.testsetname,
                            string.Format(
                                "Scheduled single test : {0} for submodule : {1} on appserver : {2} with label : {3}",
                                suitename, currentSubModule.submodule, allServers[iterator % allServers.Count].hostname,
                                currentSubModule.module.ToLower()));
                    }

                }


            }

            return 0;
        }
        public static int TriggerSubModule(TriggerSubModuleOptions options)
        {
            DashboardConnector connector = new DashboardConnector();
            Jenkins jenkins = new Jenkins(Environment.GetEnvironmentVariable("JENKINS_URL"));
            List<sConfig> subModuleConfigs = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"));
            sConfig currentSubModule = subModuleConfigs.Find(t => t.submodule.Equals(options.SubModule));

            if (!currentSubModule.sequential)
            {
                //get all the tests for the submodule
                releaseinformation releaseDetails =
                    connector.GetReleaseInformationForProductAndPack(options.Product, options.Pack);
                List<ivc_test_result> submoduletests =
                    connector.GetValidTestsForSubModuleFromDashboard(options.SubModule, releaseDetails.testsetname);
                submoduletests.RemoveAll(t =>
                    t.name.ToLower().Contains("runlast") || t.suitename.ToLower().Contains("runlast"));

                Dictionary<string, List<ivc_test_result>> bySuite = submoduletests.GroupBy(t => t.suitename)
                    .ToDictionary(tt => tt.Key, tt => tt.ToList());
                List<ivc_appserver> allServers =
                    connector.GetAllAppServersForPackSetup(options.Product, options.Pack, currentSubModule.runconfig).FindAll(tt => tt.module.Contains(currentSubModule.module));

                SchedulerLogger.Log(releaseDetails.testsetname,
                    string.Format("Triggered Run Submodule Job for {0}", options.SubModule));

                int iterator = 0;
                foreach (string suitename in bySuite.Keys)
                {
                    Dictionary<string, string> jobParams = new Dictionary<string, string>();
                    jobParams.Add("test", suitename);
                    jobParams.Add("testset", releaseDetails.testsetname);
                    jobParams.Add("appserver", allServers[iterator % allServers.Count].hostname);
                    if (options.Pack.ToLower() == "mt")
                        jobParams.Add("label", "MT");
                    else
                        jobParams.Add("label", currentSubModule.module.ToLower());
                    
                    jenkins.TriggerJob("REGRESSION_NIGHTLY_RUN_SINGLE", jobParams);
                    iterator++;
                    Thread.Sleep(500);
                }
            }
            else
            {
                Console.WriteLine(
                    "The Submodule {0} is a sequential submodule...\nPlease use REGRESSION_NIGHTLY_RUN_SEQUENTIAL_SUBMODULE job to trigger tests for this submodule", options.SubModule);
                return -1;
            }
            return 0;
        }
        public static int TriggerModule(TriggerModuleOptions options)
        {
            DashboardConnector connector = new DashboardConnector();
            Jenkins jenkins = new Jenkins();
            releaseinformation releaseDetails = connector.GetReleaseInformationForProductAndPack(options.Product, options.Pack);

            List<sConfig> subModuleConfigs = JsonConvert
                .DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties"))
                .FindAll(t => t.runconfig == "Core" || t.runconfig == "IA");
            List<ivc_test_result> moduletests = connector.GetValidTestsForModuleFromDashboard(options.Product, options.Pack, options.Module);

            moduletests.RemoveAll(t => t.submodule == "POS_Benelux" || t.submodule == "RRT.OEM.Ford" || t.submodule == "VM_Benelux");
            moduletests.RemoveAll(t => t.name.ToLower().Contains("runlast") || t.suitename.ToLower().Contains("runlast"));
            Dictionary<string, List<ivc_test_result>> bySubmodule = moduletests.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());
            
            foreach (string submodule in bySubmodule.Keys)
            {

                sConfig currentSubModule = subModuleConfigs.Find(t => t.submodule.Equals(submodule));
                List<ivc_appserver> allServers = connector.GetAllAppServersForPackSetup(options.Product, options.Pack, currentSubModule.runconfig).FindAll(tt => tt.module.Contains(currentSubModule.module));
                int iterator = 0;

                if (!currentSubModule.sequential)
                {
                    //get all the tests for the submodule
                    List<ivc_test_result> submoduletests = bySubmodule[submodule];
                    Dictionary<string, List<ivc_test_result>> bySuite = submoduletests.GroupBy(t => t.suitename).ToDictionary(tt => tt.Key, tt => tt.ToList());
                    foreach (string suitename in bySuite.Keys)
                    {
                        Dictionary<string, string> jobParams = new Dictionary<string, string>();
                        jobParams.Add("test", suitename);
                        jobParams.Add("testset", releaseDetails.testsetname);
                        jobParams.Add("appserver", allServers[iterator % allServers.Count].hostname);
                        if (options.Pack.ToLower() == "mt")
                            jobParams.Add("label", "MT");
                        else
                            jobParams.Add("label", currentSubModule.module.ToLower());
                        jenkins.TriggerJob("REGRESSION_NIGHTLY_RUN_SINGLE", jobParams);
                        iterator++;
                    }
                }
                else
                {
                    Dictionary<string, string> jobParams = new Dictionary<string, string>();
                    jobParams.Add("submodule", currentSubModule.submodule);
                    jobParams.Add("testset", releaseDetails.testsetname);
                    jobParams.Add("appserver", allServers[iterator % allServers.Count].hostname);
                    jenkins.TriggerJob("REGRESSION_NIGHTLY_RUN_SEQUENTIAL_SUBMODULE", jobParams);
                    iterator++;

                }
            }

            return 0;
        }
        public static int RegressionRunSingleBvt(RunSingleBvtOptions opts)
        {

            DashboardConnector connector = new DashboardConnector();

            //get the release details
            ivc_pack_details targetPack = connector.GetPackDetails(opts.TestSetName);
            SchedulerLogger.Log(opts.TestSetName, string.Format("Execution started for BVT : {0} on appserver : {1}", opts.Script, opts.AppServer));

            ivc_appserver serverDetails = connector.GetServerKCMLService(targetPack.product, targetPack.packname, opts.AppServer);

            //get and unzip the test libraries
            Regression_GetTestLibraries(targetPack.ivccodeversion, targetPack.packname);

            //update the configuration file
            ScrumAutomation_UpdateConfigurationFile(serverDetails.hostname, serverDetails.service, opts.TestSetName, serverDetails.kpath, serverDetails.serverurl, serverDetails.portno, targetPack);

            Console.WriteLine("Output is : {0}", File.ReadAllText("TestLibraries\\Drive.config"));

            if (opts.Script.Equals("SetProductFeatureAsInMaster"))
            {
                SetProductFeatureInfo(targetPack.packname);
            }

            ////create Nunit Runlist file
            CreateNUnitRunList(opts.Script);

            int timeout = 300;
            string subscribers = "anusha.engu@cdk.com;Nisar.Ahmed@cdk.com;Narsingsingh.Rajput@cdk.com;simanta.muduli@cdk.com;Sastry.Poranki@cdk.com";
            //Trigger the execution
            TriggerExecution(targetPack.packname, serverDetails.hostname, opts.TestSetName, opts.Script, "Pack_Setup", subscribers, (timeout * 1000));

            return 0;
        }
        public static int RegressionPackSetup(PackSetupOptions opts)
        {
            DashboardConnector connector = new DashboardConnector();
            releaseinformation targetPack = connector.GetReleaseInformationForProductAndPack(opts.Product, opts.Pack);

            string testSetName = connector.GetTestsetNameForPack(opts.Product, opts.Pack);
            Regression_GetTestLibraries(targetPack.ivccodeversion, targetPack.packname);

            ivc_appserver targetServer = connector.GetServerByHostName(opts.Server);
            ivc_pack_details targetTest = connector.GetPackDetails(targetPack.testsetname);

            //update the configuration file
            ScrumAutomation_UpdateConfigurationFile(targetServer.hostname, targetServer.service, targetPack.testsetname, targetServer.kpath, targetServer.serverurl, targetServer.portno, targetTest);
            Console.WriteLine("Output is : {0}", File.ReadAllText("TestLibraries\\Drive.config"));

            int timeout = 1200;
            string subscribers = "AkhileshVarma.Gadiraju@cdk.com;Nisar.Ahmed@cdk.com;Narsingsingh.Rajput@cdk.com;Prashanthi.Puchala@cdk.com;Sai.Atmakuru@cdk.com;Pavan.Nagireddy@cdk.com;SharathBabu.Pinupolu@cdk.com";
            
             KillKcmlProcess(targetServer.hostname);
             SchedulerLogger.Log(testSetName, string.Format("Scheduled BVT : {0} on Drive 1 appserver : BVTSA Packsetup Scripts", targetServer.hostname));                   
             TriggerExecutionBVT(opts.Pack, targetServer.hostname, testSetName, opts.Category,"Pack_Setup", subscribers, (timeout * 1000));     
                       
            return 0;
        }
        public static void TriggerExecutionBVT(string packname, string appserver, string testsetname, string category, string group, string subscribers, int timeout = 120)
        {
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries"));
            Console.WriteLine("Working Directory is : {0}", Directory.GetCurrentDirectory());
            //where is the nunit
            string nunitexe = "C:\\Program Files (x86)\\NUnit 2.6.3\\bin\\nunit-console-x86.exe";
            RemoveOldLogs();
            ProcessStartInfo psInfo = new ProcessStartInfo(nunitexe);
            psInfo.Arguments = String.Format(" /xml:bvttests_{0}.xml Drive.nunit /include:{0} /timeout:{1}", category, timeout);
            Console.WriteLine(" /xml:bvttests_{0}.xml Drive.nunit /include:{0} /timeout:{1}", category, timeout);
            Process pNunit = Process.Start(psInfo);

            //if the nunit crashes please update the status of the test to failed       
            if (!pNunit.WaitForExit(timeout))
            {
                Console.WriteLine("Killing Nunit as the specified timeout has expired...");
                pNunit.Kill();
            }
            if (File.Exists(string.Format("bvttests_{0}.xml", category)))
                sendmailJenkins(string.Format("bvttests_{0}.xml", category), packname, appserver, testsetname, subscribers, category, group);
        }

        /// <summary>
        /// This can be further optimized to looked at only relevant ZIP files and DLLs - LATER
        /// </summary>
        /// <param name="sti"></param>
        public static void CreateNUnitRunList(ScheduledTestInformation sti)
        {
            FileStream fs = new FileStream("TestLibraries\\runlist.txt", FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            XmlTextReader reader = new XmlTextReader("TestLibraries\\Drive.nunit");
            List<string> dllList = new List<string>();

            while (reader.Read())
            {
                if (reader.Name == "assembly")
                {
                    dllList.Add(reader.GetAttribute("path"));
                }
            }
            reader.Close();

            foreach (testsuite test in sti.tests)
            {
                foreach (string kvp in dllList)
                {
                    string tmptest = test.test;
                    Assembly assembly1 = System.Reflection.Assembly.LoadFrom("TestLibraries\\" + kvp);
                    Console.WriteLine(tmptest + ";" + Path.Combine(kvp));
                    var gh = assembly1.GetTypes().ToList().Find(t => (t.GetMembers().ToList().Find(tt => tt.Name.Equals(string.Format("{0}", tmptest))) != null));
                    if (gh != null)
                    {
                        Console.WriteLine("{0}.{1}", gh.FullName, tmptest);
                        string testFullName = string.Format("{0}.{1}", gh.FullName, tmptest);
                        sw.WriteLine(testFullName);
                        break;
                    }
                }
            }
            sw.Close();
            fs.Close();

        }
        public static void CreateNUnitRunList(string testname)
        {
            FileStream fs = new FileStream("TestLibraries\\runlist.txt", FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            XmlTextReader reader = new XmlTextReader("TestLibraries\\Drive.nunit");
            List<string> dllList = new List<string>();

            while (reader.Read())
            {
                if (reader.Name == "assembly")
                {
                    dllList.Add(reader.GetAttribute("path"));
                }
            }
            reader.Close();

            foreach (string kvp in dllList)
            {
                Console.WriteLine("Library name : {0}", kvp);
                Assembly assembly1 = System.Reflection.Assembly.LoadFrom("TestLibraries\\" + kvp);
                Console.WriteLine(testname + ";" + Path.Combine(kvp));
                var gh = assembly1.GetTypes().ToList().Find(t => (t.GetMembers().ToList().Find(tt => tt.Name.Equals(string.Format("{0}", testname))) != null));
                if (gh != null)
                {
                    Console.WriteLine("{0}.{1}", gh.FullName, testname);
                    string testFullName = string.Format("{0}.{1}", gh.FullName, testname);
                    sw.WriteLine(testFullName);
                    break;
                }
            }

            sw.Close();
            fs.Close();

        }
        public static void TriggerExecution(string packname, string appserver, string testsetname, string testname, string group, string subscribers, int timeout = 120)
        {
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries"));
            Console.WriteLine("Working Directory is : {0}", Directory.GetCurrentDirectory());
            //where is the nunit
            string nunitexe = "C:\\Program Files (x86)\\NUnit 2.6.3\\bin\\nunit-console-x86.exe";
            RemoveOldLogs();
            ProcessStartInfo psInfo = new ProcessStartInfo(nunitexe);
            //psInfo.WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries");
            psInfo.Arguments = String.Format(" /xml:nunit-test-{0}.xml Drive.nunit /runlist:{1} /timeout:{2}", testname, "runlist.txt", timeout*1000);
            Console.WriteLine(" /xml:nunit-test-{0}.xml Drive.nunit /runlist:{1} /timeout:{2}", testname, "runlist.txt", timeout*1000);

            Process pNunit = Process.Start(psInfo);

            //if the nunit crashes please update the status of the test to failed
            if (!pNunit.WaitForExit(timeout*1000))
            {
                Console.WriteLine("Killing Nunit as the specified timeout has expired...");
                pNunit.Kill();

                //foreach (string ivctest in File.ReadAllLines("runlist.txt").ToList())
                //{
                //    string[] testnamesplit = ivctest.Split('.');
                //    Dashboard dashboard = new Dashboard()
                //    {
                //        XmlPath = testsetname + ".xml",
                //        Testname = testsetname,
                //        Att = "status",
                //        AttVal = "Failed",
                //        TestFixtureSetupDuration = "0",
                //        Duration = Convert.ToString(timeout),
                //        Hostname = Environment.MachineName,
                //        Author = appserver,
                //        Runner = Process.GetCurrentProcess().ProcessName,
                //        Logxml = "",
                //        Mongorestapi = "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc/rest"
                //    };
                //    dashboard.Update();
                //}
            }
            
            if (File.Exists(string.Format("nunit-test-{0}.xml", testname)))
                sendmailJenkins(string.Format("nunit-test-{0}.xml", testname), packname, appserver, testsetname, subscribers, testname, group);


        }


        public static void TriggerExecutionForSmokeTests(string RpmVersion)
        {
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries"));
            Console.WriteLine("Working Directory is : {0}", Directory.GetCurrentDirectory());

            string nunitexe = "C:\\Program Files (x86)\\NUnit 2.6.3\\bin\\nunit-console-x86.exe";
            ProcessStartInfo psInfo = new ProcessStartInfo(nunitexe);
            psInfo.Arguments = String.Format(" /xml:SmokeTests_{0}.xml ARIIVC.RpmDeployment.dll", RpmVersion);
            Process pNunit = Process.Start(psInfo);
            pNunit.WaitForExit();
        }
        public static void ScrumAutomation_UpdateConfigurationFile(string appserver, string service, string testsetname, string kpath, string serverurl, string portno = null, ivc_pack_details targetPack = null)
        {
            appserver = portno != null ? appserver + ":" + portno : appserver;
            XmlDocument configxml = new XmlDocument();
            configxml.Load("TestLibraries\\Drive.config");
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='server']").Attributes["value"].Value = appserver;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='service']").Attributes["value"].Value = service;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='updatedashboard']").Attributes["value"].Value = "true";       
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='accelerator']").Attributes["value"].Value = "true";            
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='TakeSnapshots']").Attributes["value"].Value = "false";
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='ReportsLocation']").Attributes["value"].Value = "\\\\gbhpdslivcweb01.dsi.ad.adp.com\\Autoline_Drive\\Reports\\" + targetPack.packname;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='dashxml']").Attributes["value"].Value = testsetname;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='kpath']").Attributes["value"].Value = kpath;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='serverurl']").Attributes["value"].Value = serverurl;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='targetversion']").Attributes["value"].Value = targetPack.ivccodeversion;
            configxml.Save("TestLibraries\\Drive.config");



        }

        public static void SanityCheckUpdateConfigurationFile(string appserver, string service, string kpath,
            string serverurl, string systemVersion, string UpdateId, string portno = null)
        {
            appserver = portno != null ? appserver + ":" + portno : appserver;
            XmlDocument configxml = new XmlDocument();
            configxml.Load("TestLibraries\\Drive.config");
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='server']").Attributes["value"].Value =
                appserver;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='service']").Attributes["value"].Value =
                service;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='kpath']").Attributes["value"].Value =
                kpath;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='serverurl']").Attributes["value"]
                .Value = serverurl;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='systemversion']").Attributes["value"]
                .Value = systemVersion;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='updateid']").Attributes["value"].Value =
                UpdateId;
            configxml.Save("TestLibraries\\Drive.config");

        }

        public static void SmokeTriggerUpdateConfigFile(string server, string service, string kPath, string RpmVersion,
            string environmentCode, string portNumber, string userName, string password)
        {
            server = portNumber != null ? server + ":" + portNumber : server;
            XmlDocument configxml = new XmlDocument();
            configxml.Load("TestLibraries\\ARIIVC.RpmDeployment.dll.config");
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='server']").Attributes["value"].Value =
                server;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='service']").Attributes["value"].Value =
                service;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='kpath']").Attributes["value"].Value =
                kPath;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='envcode']").Attributes["value"].Value =
                environmentCode;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='rpmnumber']").Attributes["value"]
                .Value = RpmVersion;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='username']").Attributes["value"]
               .Value = userName;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='password']").Attributes["value"]
              .Value = password;
            configxml.Save("TestLibraries\\ARIIVC.RpmDeployment.dll.config");
        }

        public static void ScrumAutomation_ExecuteSingleBlock(ScheduledTestInformation singleTestBlock)
        {

        }
        public static void ExtractFiles(string zipfile, string targetFolder)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipfile))
            {
                archive.ExtractToDirectory(targetFolder, true);
            }
        }
        public static void ScrumAutomation_GetTestLibraries()
        {

            #region List of Test Libraries Required

            List<string> testLibraries = new List<string>
            {
                "ariivc.framework.zip",
                "ariivc.web.zip",
                "ariivc.autolinedrive.tests.zip"
            };

            #endregion

            using (var client = new WebClient())
            {
                foreach (string zipfile in testLibraries)
                {
                    Console.WriteLine("http://139.126.80.202/Autoline_Drive/Regression-MT/" + zipfile);
                    client.DownloadFile("http://139.126.80.202/Autoline_Drive/Regression-MT/" + zipfile, zipfile);
                    ExtractFiles(zipfile, Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries"));
                }
            }
        }
        public static void Regression_GetTestLibraries(string codeversion,string packname = null)
        {

            #region List of Test Libraries Required
            List<string> testLibraries = new List<string>();
            if (packname.ToLower() == "dmslite")
            {
                testLibraries.Add("ariivc.dmslite.tests.zip");
            }
            else
            {
                testLibraries.Add("ariivc.framework.zip");
                testLibraries.Add("ariivc.autolinedrive.tests.zip");
            }
            #endregion

            string serverToDownload = "";
            try
            {
                if (!Environment.GetEnvironmentVariable("JENKINS_URL").Contains("139.126.80.68"))
                {
                    serverToDownload = "http://gbhpdslivcweb01.dsi.ad.adp.com";
                }
                else
                {
                    serverToDownload = "http://139.126.80.202";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Env variable is null : " + e);
            }

            using (var client = new WebClient())
            {
                foreach (string zipfile in testLibraries)
                {
                    try
                    {
                        if (packname.ToLower() == "dmslite")
                        {
                            Console.WriteLine(string.Format("{2}/Autoline_Drive/DMSLITE/{1}", codeversion, zipfile, serverToDownload));
                            client.DownloadFile(string.Format("{1}/Autoline_Drive/DMSLITE/", codeversion, serverToDownload) + zipfile, zipfile);
                            ExtractFiles(zipfile, Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries"));
                        }
                        else
                        {
                            Console.WriteLine(string.Format("{2}/Autoline_Drive/Regression-{0}/{1}", codeversion, zipfile, serverToDownload));
                            client.DownloadFile(string.Format("{1}/Autoline_Drive/Regression-{0}/", codeversion, serverToDownload) + zipfile, zipfile);
                            ExtractFiles(zipfile, Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries"));
                        }
                    }
                    catch (Exception downloadException)
                    {
                        Console.WriteLine("{0} : {1}", downloadException.GetType(), downloadException.StackTrace);
                        Jenkins jenkins = new Jenkins(jenkinsURL);
                        var jNode = jenkins.GetOneNode(Environment.GetEnvironmentVariable("COMPUTERNAME"));
                        jenkins.changeNodeLabel(jNode, "HTTP-ERROR");
                    }
                    
                }
            }
        }
        public static void ScrumAutomation_updateReleaseInformation(string testsetname, string packname)
        {
            Dictionary<string, object> create = new Dictionary<string, object>();
            create.Clear();
            create.Add("ivccodeversion", "MT");
            create.Add("testsetname", testsetname);
            create.Add("pack_version", "MT");
            create.Add("testsetid", "-1");
            create.Add("packname", packname);
            create.Add("product", "Drive Sprint Teams");
            create.Add("date", DateTime.Now.ToString("dd-MM-yyyy"));
            create.Add("file_created", DateTime.Now.ToString("s"));
            create.Add("last_modified", DateTime.Now.ToString("s"));
            create.Add("last_modified_notification", "");
            postMongo(string.Format("{0}/releases/insert/?data={1}", mongoRest, JsonConvert.SerializeObject(create)));

        }
        public static string postMongo(string url, string data)
        {
            RestCall call = new RestCall() { Url = url };
            return call.Post(data);
        }
        public static void postMongo(string url)
        {
            RestCall call = new RestCall() { Url = url };
            call.Post();
        }
        private static string getRest(string url)
        {
            RestCall call = new RestCall() { Url = url };
            return call.Get();
        }
        private static void putRest(string url)
        {
            RestCall call = new RestCall() { Url = url };
            call.Put();
        }
        public static void ScrumAutomation_CreateDashboardEntry(List<testsuite> tests, string testsetname, string packname)
        {
            foreach (testsuite ts in tests)
            {
                string module = ts.group;
                string submodule = ts.name;
                foreach (singletest sts in ts.qctests)
                {
                    Dictionary<string, object> create = new Dictionary<string, object>();
                    create.Add("name", sts.scriptid);
                    create.Add("summary", sts.name);
                    create.Add("testid", sts.jira);
                    if (!string.IsNullOrEmpty(sts.name))
                        create.Add("description", Regex.Replace(sts.name, @"[^a-zA-Z0-9 ]", "", RegexOptions.Compiled));
                    else
                        create.Add("description", "");
                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");
                    create.Add("host", "Not Applicable");
                    create.Add("success", "100");
                    create.Add("author", sts.owner);
                    create.Add("created", DateTime.Parse(sts.creationtime).ToShortDateString());
                    create.Add("runner", "Default");
                    create.Add("F2US", "9999");
                    create.Add("IVUS", "");
                    create.Add("module", module);
                    create.Add("avgduration", 300);
                    create.Add("counter", 0);
                    create.Add("submodule", submodule);
                    create.Add("suitename", sts.suitename);
                    create.Add("executionid", "-1");
                    create.Add("testsetname", testsetname);
                    create.Add("packname", packname);
                    string mongoposttext = JsonConvert.SerializeObject(create);
                    Console.WriteLine("Posted on MongoDB result: " + mongoposttext);
                    postMongo(string.Format("{0}/results/insert/", mongoRest), mongoposttext);

                }
            }
        }

        //Can this be removed??
        public static void run_Scrum_Scheduled_Regression(string product, string packname, string group, string label = "ivctest")
        {
            string url = mongoRest + "/scheduler/get?query={\"product\":\"" + product + "\",\"packname\":\"" + packname + "\",\"group\":\"" + group + "\"}";
            string json_output = getRest(url);
            List<ScheduledTestInformation> stis = JsonConvert.DeserializeObject<List<ScheduledTestInformation>>(json_output);
            Dictionary<string, object> create = new Dictionary<string, object>();
            string appserver = "";
            string testsetname = DateTime.Now.ToString("dd-MM-yyyy") + "_" + packname + "-" + DateTime.Now.ToString("hhmmsstt");

            #region Create the tests in IVC Dashboard

            foreach (ScheduledTestInformation sti in stis)
            {
                appserver = sti.appserver;
                foreach (testsuite ts in sti.tests)
                {
                    string module = ts.group;
                    string submodule = ts.name;
                    foreach (singletest sts in ts.qctests)
                    {
                        create.Clear();
                        create.Add("name", sts.scriptid);
                        create.Add("summary", sts.name);
                        create.Add("testid", sts.jira);
                        if (!string.IsNullOrEmpty(sts.name))
                            create.Add("description", Regex.Replace(sts.name, @"[^a-zA-Z0-9 ]", "", RegexOptions.Compiled));
                        else
                            create.Add("description", "");
                        create.Add("status", "No Run");
                        create.Add("testsetid", "-1");
                        create.Add("duration", "0");
                        create.Add("host", "Not Applicable");
                        create.Add("success", "100");
                        create.Add("author", sts.owner);
                        create.Add("created", DateTime.Parse(sts.creationtime).ToShortDateString());
                        create.Add("runner", "Default");
                        create.Add("F2US", "9999");
                        create.Add("IVUS", "");
                        create.Add("module", module);
                        create.Add("avgduration", 300);
                        create.Add("counter", 0);
                        create.Add("submodule", submodule);
                        create.Add("suitename", sts.suitename);
                        create.Add("executionid", "-1");
                        create.Add("testsetname", testsetname);
                        create.Add("packname", packname);
                        string mongoposttext = JsonConvert.SerializeObject(create);
                        Console.WriteLine("Posted on MongoDB result: " + mongoposttext);
                        postMongo(string.Format("{0}/results/insert/", mongoRest), mongoposttext);

                    }
                }

            }

            #endregion

            #region Get Pack Details from appservers to display on Dashboard
            string appurl = mongoRest + "scheduler/appservers?query={" + "\"hostname\":\"" + appserver + "\"}";
            Console.WriteLine(appurl);
            HttpWebRequest app_request = WebRequest.Create(appurl) as HttpWebRequest;
            app_request.Method = "GET";
            HttpWebResponse app_resp = app_request.GetResponse() as HttpWebResponse;
            StreamReader app_sr = new StreamReader(app_resp.GetResponseStream());
            string app_output = app_sr.ReadToEnd();
            Console.WriteLine(app_output);
            dynamic app_th = Newtonsoft.Json.Linq.JValue.Parse(app_output);
            string service = app_th[0]["service"];
            string pack_version = appserver + ":" + app_th[0]["portno"] + "/" + app_th[0]["service"];

            #endregion

            #region update the release information

            create.Clear();
            create.Add("ivccodeversion", "MT");
            create.Add("testsetname", testsetname);
            create.Add("pack_version", pack_version);
            create.Add("testsetid", "-1");
            create.Add("packname", packname);
            create.Add("product", "Drive Sprint Teams");
            create.Add("date", DateTime.Now.ToString("dd-MM-yyyy"));
            create.Add("file_created", DateTime.Now.ToString("s"));
            create.Add("last_modified", DateTime.Now.ToString("s"));
            create.Add("last_modified_notification", "");
            postMongo(string.Format("{0}/releases/insert/?data={1}", mongoRest, JsonConvert.SerializeObject(create)));


            #endregion

            //execute_Sprint_Scheduled(product, packname, stis, label);

        }
        public static void PopulateReleaseMaps()
        {
            relMaps.Add("cngenrel", "China - General Release");
            relMaps.Add("cnint", "China - Internal");
            relMaps.Add("ea1", "Early Adopters 1");
            relMaps.Add("ea2", "Early Adopters 2");
            relMaps.Add("frgr", "France - General Release");
            relMaps.Add("gr1", "General Release 1");
            relMaps.Add("gr2", "General Release 2");
            relMaps.Add("megr", "General Release - Middle East");
            relMaps.Add("pilotlive", "Pilot - Live");
            relMaps.Add("pilotuat", "Pilot - UAT");
            relMaps.Add("null", "Not Defined");
        }
        public static void killKclientProcess()
        {
            try
            {
                foreach (Process tmp in Process.GetProcessesByName("kclient"))
                {
                    Console.WriteLine("Killing kclient process : {0}", tmp.Id);
                    tmp.Kill();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Occured : " + e);
            }


        }
        public static void KillKcmlProcess(string appServer)
        {
            //string ipAddress = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
            string ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0].ToString();
            Jenkins jenkins = new Jenkins(ReleaseEngineeringJenkinsUrl);
            Dictionary<string, string> jobParams = new Dictionary<string, string>
            {
                {"MachineIP", ipAddress},
                {"Server", appServer.Replace(".gbh.dsi.adp.com", "")}
            };

            if (appServer.Contains("d-ivc-") || appServer.Contains("gbhpivcmt0"))
            {
                jenkins.TriggerJobByToken("IVC-Kill-KCML-Processes", jobParams, "IVCREMOTE");
                Thread.Sleep(5000);
            }
        }
        public static void AgentControllerTrigger(string environment)
        {            
            Jenkins jenkins = new Jenkins(ReleaseEngineeringJenkinsUrl);
            Dictionary<string, string> jobParams = new Dictionary<string, string>
            {
                {"ENVIRONMENT_NAME", environment.ToLower()},
                {"MODE", "STARTAGENT"}
            };           
            jenkins.TriggerJobByToken("private-ivc-agent-controller-trigger", jobParams, "IVCREMOTE");
            Thread.Sleep(5000);            
        }
        
        public static void RemoveOldLogs()
        {
            try
            {
                if (Directory.Exists("logs"))
                {
                    Directory.Delete("logs", true);
                    Console.WriteLine("Removed old logs...");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception Occured : " + e);
            }
        }
        public static void CreateYmlFile(string product, string pack, string testSetName)
        {
            List<sConfig> items = JsonConvert.DeserializeObject<List<sConfig>>(File.ReadAllText("runconfiguration.properties")).FindAll(t => t.sequential.Equals(true));
            DashboardConnector connector = new DashboardConnector();

            List<string> validSuites = new List<string>();

            List<ivc_test_result> allTests = connector.GetValidTestsFromDashboard(product, pack, testSetName);

            allTests.RemoveAll(t => t.suitename.ToLower().Contains("runlast"));
            allTests.RemoveAll(t => t.name.ToLower().Contains("runlast"));

            Dictionary<string, List<ivc_test_result>> bysubModule = allTests.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());
            StringBuilder testNameYml = new StringBuilder();
            testNameYml.AppendLine("# testnames.yml");
            testNameYml.AppendLine("TESTSETNAME:");
            testNameYml.AppendLine("- " + testSetName);
            testNameYml.AppendLine("TEST_NAMES:");
            StringBuilder runLastYml = new StringBuilder();
            runLastYml.AppendLine("exclude:");

            foreach (string submodule in bysubModule.Keys)
            {
                Dictionary<string, List<ivc_test_result>> bySuite = bysubModule[submodule].GroupBy(t => t.suitename).ToDictionary(tt => tt.Key, tt => tt.ToList());

                if (items.Find(t => t.submodule.Equals(submodule)) != null)
                {
                    validSuites.Add(string.Join(",", bySuite.Keys));
                }
                else
                {
                    foreach (string suiteid in bySuite.Keys)
                    {
                        validSuites.Add(suiteid);
                    }
                }
            }

            foreach (string suitename in validSuites)
            {
                testNameYml.AppendLine("- " + suitename);
            }

            //testNameYml.AppendLine(Convert.ToString(runLastYml));
            System.IO.File.WriteAllText("testnames.yml", testNameYml.ToString());
        }
        public static string getPackVersion(string product, string packname)
        {
            DashboardConnector connector = new DashboardConnector();
            return connector.GetPackVersion(product, packname);
        }
        protected static string putMongoDB(string url, string data)
        {
            RestCall call = new RestCall() { Url = url };
            return call.Put(data);
        }
        public static void sendmailJenkins(string xmlFile, string ivcpack, string ivcserver, string testlabpath, string sendTo, string testsuite, string submodule)
        {
            MailTestResultNotification notification = new MailTestResultNotification()
            {
                DashBoardServerUrl = "http://" + dashboardserver,
                Drive = Drive,
                IvcPack = ivcpack,
                NotificationTargets = sendTo,
                DriveSprint = Drive_Sprint,
                IvcServer = ivcserver,
                SubModule = submodule,
                TestResultXmlFile = xmlFile,
                TestSuite = testsuite,
                TestSetName = testlabpath

            };
            notification.Notify();
        }
        public static List<List<T>> SplitIntoChunks<T>(List<T> list, int chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentException("chunkSize must be greater than 0.");
            }
            List<List<T>> retVal = new List<List<T>>();
            int index = 0;
            while (index < list.Count)
            {
                int count = list.Count - index > chunkSize ? chunkSize : list.Count - index;
                retVal.Add(list.GetRange(index, count));
                index += chunkSize;
            }
            return retVal;
        }

        public static int InfosysTriggerSmokeTests(InfosysSmokeTrigger opts)
        {
            Regression_GetTestLibraries("MT","MT");
            SmokeTriggerUpdateConfigFile(opts.Server, opts.Service, "C:\\CDK\\KCMLMT", opts.RpmVersion,
                opts.EnvironmentCode, opts.PortNumber, opts.UserName, opts.Password);
            TriggerExecutionForSmokeTests(opts.RpmVersion);

            return 0;
        }

        public static int SetProductFeatureInfo(string pack)
        {
            ProductFeatureData getProductFeatureData = new ProductFeatureData();
            Dictionary<string, List<FeatureInfo>> productConfig = getProductFeatureData.GetFeatureInfo(pack);
            string productFeaturesJson = JsonConvert.SerializeObject(productConfig);
            File.WriteAllText(Directory.GetCurrentDirectory() + @"\TestLibraries\testdata\productFeature.json", productFeaturesJson);

            return 0;
        }

        public static int MongoDbCleanUp(MongoDbCleanUpOptions opts)
        {
            DateTime tillDate = new DateTime(Convert.ToInt32(opts.Year), Convert.ToInt16(opts.Month), Convert.ToInt16(opts.Day));

            List<string> packsList = new List<string>
            {
                "Live2",
                "Pilot2",
                "MT2",
                "JobMaps",
                "IVCSprint",
                "jobmaps",
                "Sprint",
                "CRMORANGE",
                "Accelerate",
                "HummingBird",
                "Aftersales",
                "Vehicles",
                "CRM",
                "Accounts",
                "AccelerateMulti",
                "JobMaps-Pilot",
                "JobMaps-Live",
                "JobMaps-MT",
                "CRMOrange",
                "AccMultipleCases",
                "Live",
                "Pilot",
                "MT",				
                "ReleaseTesting",
                "preprod"
            };
            MongoDriver mongoDriver = new MongoDriver();
            foreach (var pack in packsList)
            {
                HashSet<string> testSetNames = mongoDriver.AssociatedRuns.GetTestSetNamesFilteredByDate(pack, tillDate);

                foreach (var testSetName in testSetNames)
                {
                    mongoDriver.Results.RemoveTestsByTestSetName(testSetName);
                }

                mongoDriver.AssociatedRuns.DeleteAssociatedRuns(pack, testSetNames);
            }
            return 0;
        }

        #region Methods for Release Testing
        public static int ReleaseTesting(ReleaseTesting opts)
        {
            //Dash board creation required parameter
            if (opts.DefaultVersion != null)
            {
                releasetestingdashboard releaseTestingOpts = new releasetestingdashboard();
                releaseTestingOpts.defaultversion = opts.DefaultVersion;
                ReleaseTestingDashboardCreation(releaseTestingOpts);
            }

            //sitestaus creation required parameter
            if (opts.VersionId != null)
            {
                SiteStatusOptions releaseTestingOpts = new SiteStatusOptions();
                releaseTestingOpts.versionId = opts.VersionId;
                ReleaseTestingSiteStatusUpdate(releaseTestingOpts);
            }

            //nightly run required parameters
            if (opts.SiteName != null && opts.TestsetName != null && opts.Module != null)
            {
                NightlyRunOptions releaseTestingOpts = new NightlyRunOptions();
                releaseTestingOpts.sitename = opts.SiteName;
                releaseTestingOpts.TestSetName = opts.TestsetName;
                releaseTestingOpts.module = opts.Module;
                ReleaseSitesNightlyRun(releaseTestingOpts.sitename, releaseTestingOpts.TestSetName, releaseTestingOpts.module,releaseTestingOpts.region);
            }

            return 0;
        }
        public static int ReleaseTestingDashboardCreation(releasetestingdashboard opts)
        {

            #region Dashboard creation with the scheduled date and add all related site test flows 
            string testSetName, releaseversion, updateid;
            string SchduledDate = DateTime.Now.AddDays(-1).ToShortDateString().Replace("/", "");

            #region List of Test Libraries Required
            Regression_GetTestLibraries("MT", "MT");
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries\\ScheduledList\\" + opts.defaultversion));
            #endregion

            string str_directory = Environment.CurrentDirectory.ToString();
            string filepath = str_directory + "\\" + SchduledDate + ".json";

            if (File.Exists(filepath))
            {
                Console.WriteLine("File exists i.e customer sites exists/scheduled to create a dashboard" + filepath);

                List<customersitescheduleList> siteDetails = JsonConvert.DeserializeObject<List<customersitescheduleList>>(File.ReadAllText(SchduledDate + ".json"));
                foreach (customersitescheduleList siteDetail in siteDetails)
                {
                    if (siteDetail.date == SchduledDate)
                    {
                        releaseversion = siteDetail.releaseversion;
                        updateid = siteDetail.updateid;
                        if (updateid.Length.Equals(1))
                            updateid = "0" + siteDetail.updateid;

                        //get the testsetname for the current release
                        testSetName = GetTestSetNameForRelease(releaseversion);

                        #region Get the Deployment Tests
                        Dictionary<string, string> create = new Dictionary<string, string>();

                        Jira jira = new Jira("svc_autoline_ivc", "6xL@tCdw]/", "https://projects.cdk.com");
                        jProject jp = jira.Project("IDRIVE");
                        List<zTest> ztl = jira.DeploymentTestsInCodeVersion("");

                        #endregion

                        #region Add the Tests to Mongo DB
                        SchedulerLogger.Log(testSetName, DateTime.Now.ToShortDateString() + "test set name created with the current date BST i.e : " + testSetName);

                        foreach (string tmpRelease in siteDetail.scheduledsites.Split(','))
                        {
                            SchedulerLogger.Log(testSetName, "Adding Tests to the customer site :  " + tmpRelease);

                            CustomerSiteConfig.Update(tmpRelease, "false", "scheduled", SchduledDate);

                            foreach (zTest tmp in ztl)
                            {


                                create.Clear();
                                create.Add("name", tmp.fields.ScriptID);
                                create.Add("summary", tmp.fields.summary);
                                create.Add("testid", tmp.id);

                                if (!string.IsNullOrEmpty(tmp.fields.description))
                                    create.Add("description", Regex.Replace(tmp.fields.description, @"[^a-zA-Z0-9 ]", "", RegexOptions.Compiled));
                                else
                                    create.Add("description", "");

                                create.Add("status", "No Run");
                                create.Add("testsetid", "-1");
                                create.Add("duration", "0");
                                create.Add("host", "Not Applicable");
                                create.Add("success", "100");
                                create.Add("author", tmp.fields.creator != null ? tmp.fields.creator.displayName : "ivcauto");
                                create.Add("created", tmp.fields.components[0].name);
                                create.Add("runner", "Default");
                                create.Add("F2US", "9999");
                                create.Add("IVUS", tmp.key);
                                create.Add("module", DateTime.Now.AddDays(-1).ToShortDateString() + "(R" + siteDetail.region + "1" + siteDetail.releaseversion.Split('-')[1] + "R00000" + updateid + ")");
                                create.Add("submodule", tmpRelease);
                                create.Add("suitename", tmp.fields.labels[0]);
                                create.Add("executionid", "-1");
                                create.Add("testsetname", testSetName);
                                create.Add("packname", "ReleaseTesting");
                                create.Add("scheduleddate", SchduledDate);
                                create.Add("sitestatus", "scheduled");
                                //create result set
                                string mongoposttext = JsonConvert.SerializeObject(create);
                                Console.WriteLine("Posted on MongoDB result: " + mongoposttext);
                                postMongo(string.Format("{0}/results/insert/", mongoRest), mongoposttext);

                            }


                            string modulename = create["module"];
                            if (tmpRelease == "Busseys UAT")
                            {
                                SchedulerLogger.Log(testSetName, DateTime.Now.ToShortDateString() + "Adding Customer Specific tests to the customer site is : Busseys UAT  ");
                                AddBCTSToReleaseDashBoard("ReleaseTesting", modulename, tmpRelease, "ReleaseTesting.Busseys", testSetName);
                                SchedulerLogger.Log(testSetName, string.Format("Adding Customer Specific tests to the customer site is : {0} is done  ", tmpRelease));
                            }
                            if (tmpRelease == "Hendy's UAT")
                            {
                                SchedulerLogger.Log(testSetName, DateTime.Now.ToShortDateString() + "Adding Customer Specific tests to the customer site is : " + tmpRelease);
                                AddBCTSToReleaseDashBoard("ReleaseTesting", modulename, tmpRelease, "ReleaseTesting.EAUAT", testSetName);
                                SchedulerLogger.Log(testSetName, string.Format("Adding Customer Specific tests to the customer site is : {0}  done  ", tmpRelease));
                            }
                            if (tmpRelease == "Renault Retail Group EVAL UAT")
                            {
                                SchedulerLogger.Log(testSetName, DateTime.Now.ToShortDateString() + "Adding Customer Specific tests to the customer site is : Renault Retail Group EVAL UAT");
                                AddBCTSToReleaseDashBoard("ReleaseTesting", modulename, tmpRelease, "ReleaseTesting.RRG", testSetName);
                                SchedulerLogger.Log(testSetName, "Adding Customer Specific tests to the customer site is : Renault Retail Group EVAL UAT is done");
                            }
                            if (tmpRelease == "Busseys Live" || tmpRelease == "Hendy's Group Live" || tmpRelease == "Snows Motor Group LIVE" || tmpRelease == "Renault Retail Group Live" || tmpRelease == "Gates of Epping Live" || tmpRelease == "BMW Specialist Cars LIVE")
                            {
                                SchedulerLogger.Log(testSetName, "Adding Early Adopter live Specific tests to the customer site is :  " + tmpRelease);
                                AddBCTSToReleaseDashBoard("ReleaseTesting", modulename, tmpRelease, "ReleaseTesting.EALive", testSetName);
                                SchedulerLogger.Log(testSetName, DateTime.Now.ToShortDateString() + string.Format("Adding Early Adopter live Specific tests to the customer site is : {0}  done", tmpRelease));
                            }
                            SchedulerLogger.Log(testSetName, DateTime.Now.ToShortDateString() + string.Format("Adding Tests to the customer site {0} is done ", tmpRelease));



                        }



                        #endregion

                        #region update the releases table with the testset name
                        DashboardConnector connector = new DashboardConnector();
                        create.Clear();
                        string codeVersion = "MT";
                        string date1 = DateTime.Now.ToShortDateString();
                        string updateid1 = siteDetail.updateid;
                        create.Add("systemversion", siteDetail.releaseversion);
                        create.Add("updateid", updateid1);
                        create.Add("updatedon", DateTime.Now.ToShortDateString());
                        create.Add("ivccodeversion", codeVersion);
                        create.Add("testsetname", testSetName);
                        //create.Add("submodule", siteName);
                        create.Add("pack_version", "Drive-" + "ReleaseTesting");
                        create.Add("testsetid", "-1");
                        create.Add("packname", "ReleaseTesting");
                        create.Add("product", "Drive");
                        create.Add("date", testSetName.Split('_')[0]);
                        create.Add("file_created", DateTime.Now.ToString("s"));
                        create.Add("last_modified", DateTime.Now.ToString("s"));
                        create.Add("last_modified_notification", "");
                        RestCall call = new RestCall()
                        {
                            Url = string.Format("{0}/releases/insert/?data={1}", mongoRest, JsonConvert.SerializeObject(create))
                        };
                        call.Post(create.ToString());
                        SchedulerLogger.Log(testSetName, DateTime.Now.ToShortDateString() + "Updated the release table in database with test set name" + testSetName);
                        #endregion

                    }

                }
            }
            else
                Console.WriteLine("File deos not exists i.e customer sites not exists/scheduled to create a dashboard" + filepath);

            #endregion
            return 0;

        }
        public static int ReleaseTestingSiteStatusUpdate(SiteStatusOptions opts)
        {


            #region Check the sitelist in the and file and get the scheduled sites installation status from RM and update in mongodb
            string SchduledDate = DateTime.Now.AddDays(-1).ToShortDateString().Replace("/", "");
            string teststatus, installationstatus, siteName, updateid;
            #region List of Test Libraries Required
            Regression_GetTestLibraries("MT","MT");
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries\\ScheduledList\\" + opts.versionId));
            #endregion

            #region Get the Dashboard XML details

            string url1 = mongoRest + "releases/get?query={" + "\"product\":\"" + "Drive" + "\",\"packname\":\"" + "ReleaseTesting" + "\"}";
            Console.WriteLine(url1);
            HttpWebRequest request = WebRequest.Create(url1) as HttpWebRequest;
            request.Method = "GET";
            HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string output = sr.ReadToEnd();
            Console.WriteLine(output);
            dynamic th = Newtonsoft.Json.Linq.JValue.Parse(output);

            #endregion

            #region installation status update ,Add the specific tests and run if status is true
            Dictionary<string, string> create = new Dictionary<string, string>();

            string str_directory = Environment.CurrentDirectory.ToString();
            string filepath = str_directory + "\\" + SchduledDate + ".json";

            if (File.Exists(filepath))
            {
                Console.WriteLine("File exists i.e customer sites exists/scheduled" + filepath);

                List<customersitescheduleList> siteDetails = JsonConvert.DeserializeObject<List<customersitescheduleList>>(File.ReadAllText(SchduledDate + ".json"));

                str_directory = Environment.CurrentDirectory.ToString();
                string parent1 = System.IO.Directory.GetParent(str_directory).FullName;
                string parent2 = System.IO.Directory.GetParent(parent1).FullName;
                string parent3 = System.IO.Directory.GetParent(parent2).FullName;
                Console.WriteLine("Directory path :-{0}", parent3);
                Directory.SetCurrentDirectory(Path.Combine(parent3, "TestLibraries"));

                foreach (customersitescheduleList siteDetail in siteDetails)
                {
                    if (siteDetail.region != "FR")
                    {
                        if (siteDetail.date == SchduledDate)
                        {
                            foreach (string site in siteDetail.scheduledsites.Split(','))
                            {
                              
                                siteName = site;
                                List<SiteConfig> val = CustomerSiteConfig.FindFieldValues(siteName);


                                if (val == null || val.Count == 0)
                                {
                                    SchedulerLogger.Log(Convert.ToString(th["testsetname"]), DateTime.Now.ToShortDateString() + "Skipping further check as Site name not found in MONGODB : " + siteName);
                                }
                                else
                                {
                                    teststatus = val[0].SiteStatus;
                                    installationstatus = val[0].installed;
                                    Console.WriteLine("Site name is : {0} and site status is : {1}", siteName, teststatus);

                                    if (installationstatus == null || installationstatus == "false")
                                    {
                                        string URL = csdtRestURL + "release-testing/deploy_status?";

                                        Dictionary<string, string> query = new Dictionary<string, string>()
                                            {
                                                {"name",siteName.Replace("&","%26").Replace(" ","%20").Replace("'","%27")},
                                                {"updateid",siteDetail.updateid},
                                                {"version",siteDetail.releaseversion},
                                            };
                                        Console.WriteLine("Input data for Ring Master Qurey is siteName ={0},update id={1}, releaseversion={2}", siteName, siteDetail.updateid, siteDetail.releaseversion);

                                        try
                                        {
                                            string geturl = string.Format("{0}query={1}", URL, JsonConvert.SerializeObject(query));
                                            string jsonoutput = getRest(geturl);
                                            var test = JValue.Parse(jsonoutput);
                                            SiteConfig ReleaseTestResults = JsonConvert.DeserializeObject<SiteConfig>(test.ToString());
                                            installationstatus = ReleaseTestResults.installed;
                                            var customername = query["name"];
                                            Console.WriteLine("Getting the installation status for the site '{1}' is : '{0}'", installationstatus, site);
                                            CustomerSiteConfig.Update(siteName, installationstatus, teststatus, SchduledDate);
                                        }
                                        catch (Exception ee)
                                        {
                                            SchedulerLogger.Log(Convert.ToString(th["testsetname"]), DateTime.Now.ToShortDateString() + "Rest query returned an exception for site : " + siteName + " Hence skipping further " + ee.StackTrace);
                                        }

                                    }

                                    updateid = siteDetail.updateid;
                                    if (updateid.Length.Equals(1))
                                        updateid = "0" + siteDetail.updateid;

                                    #region Once installation status true it has to trigger the site
                                    if (installationstatus == "true" && siteDetail.date == SchduledDate && teststatus != "triggered")
                                    {
                                        Console.WriteLine("Installation is true and smoke tests is triggering for the site - {0}", siteName);
                                        SchedulerLogger.Log(Convert.ToString(th["testsetname"]), DateTime.Now.ToShortDateString() + " :- Installation status is :-  " + installationstatus + ": for the site : " + siteName + " and triggered status is :" + "triggred");

                                        Jenkins jenkins = new Jenkins();
                                        Dictionary<string, string> jobParams = new Dictionary<string, string>();
                                        jobParams.Add("sitename", siteName.Replace(" ", "%20").Replace("&", "%26"));
                                        jobParams.Add("testsetname", Convert.ToString(th["testsetname"]));
                                        jobParams.Add("module", "R" + siteDetail.region + "1" + siteDetail.releaseversion.Split('-')[1] + "R00000" + updateid);
                                        if (siteDetail.region == "CN")
                                        {
                                            jobParams.Add("locale", "zh-CN");
                                        }
                                        else
                                        {
                                            jobParams.Add("locale", "");
                                        }

                                        jenkins.TriggerJob("RELEASETESTING_NIGHTLYRUN", jobParams);

                                        CustomerSiteConfig.Update(siteName, installationstatus, "triggered", SchduledDate);

                                    }
                                    if (installationstatus != "true" && siteDetail.date == SchduledDate)
                                    {
                                        Console.WriteLine("Installation is false and smoke tests is not yet trigger for the site - {0}", siteName);
                                        SchedulerLogger.Log(Convert.ToString(th["testsetname"]), DateTime.Now.ToShortDateString() + ":-Installation status is :- " + installationstatus + "  for the site : " + siteName + "and triggered status is :" + "Not triggred");
                                        CustomerSiteConfig.Update(siteName, installationstatus, "scheduledbutnottriggered", SchduledDate);
                                    }
                                    #endregion

                                }
                            }
                        }
                    }
                }

            }
            else
                Console.WriteLine("File does not  exists i.e customer sites are not exists or scheduled");
            #endregion

            #endregion

            return 0;

        }
        public static void ReleaseSitesNightlyRun(string siteName, string groupName,string Module, string region)
        {
            #region Nightly run Customer sites after successful installation

            #region Variable Declartions
            string host, service, envcode, releaseversion, updateid, runuser, mrepo_server_url;
            string SchduledDate = DateTime.Now.AddDays(-1).ToShortDateString().Replace("/", "");
            #endregion

            #region get Release Update Details
            Dictionary<string, string> rDetails = new Dictionary<string, string>();
            rDetails.Add("name", siteName.Replace("&", "%26").Replace(" ", "%20").Replace("'","%27"));
            string release_url = mongoRest + "customer-checks/get?query=" + JsonConvert.SerializeObject(rDetails);
            List<prod_hosts> prodhosts = JsonConvert.DeserializeObject<List<prod_hosts>>(getRest(release_url));//.FindAll(t => !t.status.ToLower().Equals("triggered"));
            host = prodhosts[0].ip_addr;
            service = prodhosts[0].service;
            envcode = prodhosts[0].envcode;
            releaseversion = prodhosts[0].version;
            updateid = Module;//prodhosts[0].relid;
            runuser = prodhosts[0].loginuser;
            mrepo_server_url = "http://gbhpcsdtrep01.gbh.dsi.adp.com";
            #endregion

            #region Get the Dashboard XML details

            string url1 = mongoRest + "releases/get?query={" + "\"product\":\"" + "Drive" + "\",\"packname\":\"" + "ReleaseTesting" + "\"}";
            Console.WriteLine(url1);
            HttpWebRequest request = WebRequest.Create(url1) as HttpWebRequest;
            request.Method = "GET";
            HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string output = sr.ReadToEnd();
            Console.WriteLine(output);
            dynamic th = Newtonsoft.Json.Linq.JValue.Parse(output);
            string systemversion = th["systemversion"];
            string[] VersionID = systemversion.Split('-');
            #endregion

            #region Prodhosts update with the version
            Dictionary<string, string> query = new Dictionary<string, string>();
            query.Add("name", siteName);
            Dictionary<string, string> putMongo = new Dictionary<string, string>();
            putMongo.Add("version", systemversion);
            string url = string.Format("{0}hosts/update/?query={1}&data={2}", mongoRest, JsonConvert.SerializeObject(query), JsonConvert.SerializeObject(putMongo));
            putRest(url);
            #endregion

            #region List of Test Libraries Required
            Regression_GetTestLibraries("MT", "MT");
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries\\ScheduledList\\" + VersionID[0] + VersionID[1]));
            string str_directory = Environment.CurrentDirectory.ToString();
            string parent1 = System.IO.Directory.GetParent(str_directory).FullName;
            string parent2 = System.IO.Directory.GetParent(parent1).FullName;
            string parent3 = System.IO.Directory.GetParent(parent2).FullName;
            Console.WriteLine("Directory path :-{0}", parent3);
            Directory.SetCurrentDirectory(Path.Combine(parent3, "TestLibraries"));

            #endregion

            #region configuration file creation

            XmlDocument configxml = new XmlDocument();
            configxml.Load("ARIIVC.DriveDeployment.dll.config");
            SchedulerLogger.Log(Convert.ToString(th["testsetname"]), DateTime.Now.ToShortDateString() + ":- Configuration file creation started with customer information");
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='server']").Attributes["value"].Value = host;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='service']").Attributes["value"].Value = service;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='envcode']").Attributes["value"].Value = envcode;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='releaseid']").Attributes["value"].Value = systemversion;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='updateid']").Attributes["value"].Value = updateid;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='groupname']").Attributes["value"].Value = groupName;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='clientname']").Attributes["value"].Value = siteName;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='updatedashboard']").Attributes["value"].Value = "true";
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='dashxml']").Attributes["value"].Value = th["testsetname"];
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='loginuser']").Attributes["value"].Value = runuser;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='boxswap']").Attributes["value"].Value = "false";
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='locale']").Attributes["value"].Value = region;
           
            configxml.Save("ARIIVC.DriveDeployment.dll.config");
            SchedulerLogger.Log(Convert.ToString(th["testsetname"]), DateTime.Now.ToShortDateString() + " :- Configuration file created with customer information successfully");
            string nunitexe = "C:\\Program Files (x86)\\NUnit 2.6.3\\bin\\nunit-console-x86.exe";
            ProcessStartInfo psInfo = new ProcessStartInfo(nunitexe);
            psInfo.Arguments = String.Format(" /xml:deploymenttests_{0}.xml ARIIVC.DriveDeployment.dll /timeout:240000", envcode);
            Process pNunit = Process.Start(psInfo);
            pNunit.WaitForExit();
            string triggerURL = mrepo_server_url;
            if (File.Exists(string.Format("deploymenttests_{0}.xml", envcode)))
            {
                SchedulerLogger.Log(Convert.ToString(th["testsetname"]), DateTime.Now.ToShortDateString() + ":- Customer sites triggered to test smoke testcases");
                triggerURL += "/csdt/rest/customer-checks/update_testxml?query={\"ip_addr\":\"" + host + "\", \"service\":\"" + service + "\"}";
                PostMultipleFiles(triggerURL, string.Format("deploymenttests_{0}.xml", envcode));
            }
            else
            {
                triggerURL += "/csdt/rest/customer-checks/update?query={\"ip_addr\":\"" + host + "\", \"service\":\"" + service + "\"}";
            }

            // triggerURL = "http://gbhsremrepo01.gbh.dsi.adp.com/csdt/rest/customer-checks/update_testxml?query={\"ip_addr\":\"" + "asdasd" + "\", \"service\":\"" + "ewew" + "\"}";


            #endregion

            #endregion

        }

        public static int RunDeploymentTests(RunDeploymentTestsOptions opts)
        {
            #region Get the Release Details
            Dictionary<string, string> release = new Dictionary<string, string>();
            release.Add("ip_addr", opts.Host);
            release.Add("service", opts.Service);

            string req_url = "http://gbhpcsdtrep01.gbh.dsi.adp.com/csdt/rest/customer-checks/hosts?query=" + JsonConvert.SerializeObject(release);
            List<CustomerDetails> cDetails = JsonConvert.DeserializeObject<List<CustomerDetails>>(getRest(req_url));
            string groupname = "";
            string clientname = "";
            if (cDetails.Count > 0)
            {
                groupname = cDetails[0].relpri.ToLower();
                if (relMaps.Keys.Contains(cDetails[0].relpri.ToLower()))
                {
                    groupname = relMaps[cDetails[0].relpri.ToLower()];
                }
                clientname = cDetails[0].name;
            }

            #endregion

            #region Get the Dashboard XML details

            string url1 = "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc/rest/releases/get?query={" + "\"product\":\"" + "Drive" + "\",\"packname\":\"" + "ReleaseTesting" + "\"}";
            Console.WriteLine(url1);
            HttpWebRequest request = WebRequest.Create(url1) as HttpWebRequest;
            request.Method = "GET";
            HttpWebResponse resp = request.GetResponse() as HttpWebResponse;
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string output = sr.ReadToEnd();
            Console.WriteLine(output);
            dynamic th = Newtonsoft.Json.Linq.JValue.Parse(output);

            #endregion

            Regression_GetTestLibraries("MT", "MT");
            Directory.SetCurrentDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TestLibraries"));
            XmlDocument configxml = new XmlDocument();
            configxml.Load("ARIIVC.DriveDeployment.dll.config");
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='server']").Attributes["value"].Value = opts.Host;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='service']").Attributes["value"].Value = opts.Service;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='envcode']").Attributes["value"].Value = opts.EnvCode;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='releaseid']").Attributes["value"].Value = opts.ReleaseVersion;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='updateid']").Attributes["value"].Value = opts.UpdateId;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='groupname']").Attributes["value"].Value = groupname;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='clientname']").Attributes["value"].Value = clientname;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='clientname']").Attributes["value"].Value = clientname;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='updatedashboard']").Attributes["value"].Value = "true";
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='dashxml']").Attributes["value"].Value = th["testsetname"];
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='loginuser']").Attributes["value"].Value = opts.RunUser;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='boxswap']").Attributes["value"].Value = "false";

            configxml.Save("ARIIVC.DriveDeployment.dll.config");

            string nunitexe = "C:\\Program Files (x86)\\NUnit 2.6.3\\bin\\nunit-console-x86.exe";
            ProcessStartInfo psInfo = new ProcessStartInfo(nunitexe);
            psInfo.Arguments = String.Format(" /xml:deploymenttests_{0}.xml ARIIVC.DriveDeployment.dll", opts.EnvCode);
            Process pNunit = Process.Start(psInfo);
            pNunit.WaitForExit();
            string triggerURL = opts.MrepoServerUrl;
            if (File.Exists(string.Format("deploymenttests_{0}.xml", opts.EnvCode)))
            {
                triggerURL += "/csdt/rest/customer-checks/update_testxml?query={\"ip_addr\":\"" + opts.Host + "\", \"service\":\"" + opts.Service + "\"}";
            }
            else
            {
                triggerURL += "/csdt/rest/customer-checks/update?query={\"ip_addr\":\"" + opts.Host + "\", \"service\":\"" + opts.Service + "\"}";
            }

            //triggerURL = "http://gbhsremrepo01.gbh.dsi.adp.com/csdt/rest/customer-checks/update_testxml?query={\"ip_addr\":\"" + "asdasd" + "\", \"service\":\"" + "ewew" + "\"}";
            PostMultipleFiles(triggerURL, string.Format("deploymenttests_{0}.xml", opts.EnvCode));

            return 0;
        }
        #endregion

        #region Methods Related to Release Testing
        public static string GetTestSetNameForRelease(string releaseName, string packname = "ReleaseTesting")
        {
            string testsetname = "";
            Dictionary<string, string> queryparams = new Dictionary<string, string>();
            queryparams.Add("packname", packname);
            string req_url = mongoRest + "releases/get?query=" + JsonConvert.SerializeObject(queryparams);
            string jsonOutput = getRest(req_url);
            ivc_pack_details targetPack = JsonConvert.DeserializeObject<ivc_pack_details>(jsonOutput);

            if (targetPack != null)
            {
                //ivc_pack_details targetPack = cDetails[0];
                if (targetPack.systemversion == releaseName)
                {
                    testsetname = targetPack.testsetname;
                }
                else
                {
                    string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + "ReleaseTesting" + "-" + DateTime.Now.ToString("hhmmsstt");
                    Dictionary<string, string> updateData = new Dictionary<string, string>();
                    updateData.Add("systemversion", releaseName);
                    updateData.Add("testsetname", testSetName);
                    updateData.Add("pack_version", "ReleaseTesting");
                    updateData.Add("testsetid", "99999");
                    updateData.Add("packname", "ReleaseTesting");
                    updateData.Add("product", "Drive");
                    updateData.Add("date", DateTime.Now.ToString("dd-MM-yyyy"));
                    updateData.Add("file_created", DateTime.Now.ToString("s"));
                    updateData.Add("last_modified", DateTime.Now.ToString("s"));
                    updateData.Add("last_modified_notification", "");
                    postMongo(string.Format("{0}/releases/insert/?data={1}", mongoRest, JsonConvert.SerializeObject(updateData)));
                    testsetname = testSetName;

                }
            }

            return testsetname;
        }
        public static void CreateScheduledTests(string releaseName, string date, string ringrelease = null)
        {
            //Hard-coded the pack for release testing
            string repackname = "ReleaseTesting";

            //get the testsetname for the current release
            string testsetname = GetTestSetNameForRelease(releaseName);

            //get the code version - defaulted to MT
            string codeversion = getPackVersion("Drive", "ReleaseTesting");

            #region Get the Scheduled Sites Information

            Dictionary<string, string> release = new Dictionary<string, string>();
            release.Add("version", releaseName);
            release.Add("date", date);
            string query = JsonConvert.SerializeObject(release);
            string req_url = csdtRestURL + "schedule/events?query=" + query;
            string jsonoutput = getRest(req_url).Replace("\"envdetails\":[]", "\"envdetails\": null");
            List<csdtschedule> siteDetails = JsonConvert.DeserializeObject<List<csdtschedule>>(jsonoutput).FindAll(t => !t.environment.ToLower().Equals("internal"));

            #endregion

            #region Get the Deployment Tests
            Dictionary<string, string> create = new Dictionary<string, string>();
            Jira jira = new Jira("svc_autoline_ivc", "6xL@tCdw]/", "https://projects.cdk.com");
            jProject jp = jira.Project("IDRIVE");
            List<zTest> ztl = jira.DeploymentTestsInCodeVersion(codeversion);
            #endregion

            #region Add the Tests to Mongo DB
            jVersion currentVersion;
            currentVersion = jp.versions.Find(t => t.name.Contains(codeversion));
            foreach (csdtschedule tmpRelease in siteDetails)
            {
                foreach (zTest tmp in ztl)
                {
                    create.Clear();
                    create.Add("name", tmp.fields.ScriptID);
                    create.Add("summary", tmp.fields.summary);
                    create.Add("testid", tmp.id);

                    if (!string.IsNullOrEmpty(tmp.fields.description))
                        create.Add("description", Regex.Replace(tmp.fields.description, @"[^a-zA-Z0-9 ]", "", RegexOptions.Compiled));
                    else
                        create.Add("description", "");

                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");
                    create.Add("host", "Not Applicable");
                    create.Add("success", "100");
                    create.Add("author", tmp.fields.creator != null ? tmp.fields.creator.displayName : "ivcauto");
                    create.Add("created", tmpRelease.host_name);
                    create.Add("runner", "Default");
                    create.Add("F2US", ringrelease);
                    create.Add("IVUS", tmp.key);

                    if (!string.IsNullOrEmpty(tmpRelease.startdate))
                    {
                        create.Add("module", tmpRelease.startdate + "(" + ringrelease + ")");
                    }

                    create.Add("submodule", tmpRelease.host_name);
                    create.Add("suitename", tmp.fields.labels[0]);
                    create.Add("executionid", "-1");
                    create.Add("testsetname", testsetname);
                    create.Add("packname", repackname);
                    //create result set
                    string mongoposttext = JsonConvert.SerializeObject(create);
                    Console.WriteLine("Posted on MongoDB result: " + mongoposttext);
                    postMongo(string.Format("{0}/results/insert/", mongoRest), mongoposttext);

                }

            }

            #endregion

            #region Update the Release Collection - Not Required
            //create.Clear();
            //create.Add("ivccodeversion", "MT");
            //create.Add("testsetname", testsetname);
            //create.Add("pack_version", "ReleaseTesting");
            //create.Add("testsetid", "99999");
            //create.Add("packname", "ReleaseTesting");
            //create.Add("product", "Drive");
            //create.Add("date", DateTime.Now.ToString("dd-MM-yyyy"));
            //create.Add("file_created", DateTime.Now.ToString("s"));
            //create.Add("last_modified", DateTime.Now.ToString("s"));
            //create.Add("last_modified_notification", "");
            //postMongo(string.Format("{0}/releases/insert/?data={1}", mongoRest, JsonConvert.SerializeObject(create)));
            #endregion

        }
        public static void updateResults(string source, string target)
        {
            Dictionary<string, string> destparams = new Dictionary<string, string>();
            destparams.Add("testsetname", target);
            string desturl =  mongoRest + "results/get?query=" + JsonConvert.SerializeObject(destparams);

            List<ivc_test_result> results = JsonConvert.DeserializeObject<List<ivc_test_result>>(getRest(desturl));

            foreach (ivc_test_result currentOne in results)
            {
                Dictionary<string, string> sourceparams = new Dictionary<string, string>();
                sourceparams.Add("testsetname", source);
                sourceparams.Add("submodule", currentOne.submodule);
                sourceparams.Add("name", currentOne.name);
                string sourceurl = mongoRest + "results/get?query=" + JsonConvert.SerializeObject(sourceparams);
                List<ivc_test_result> oldresults = JsonConvert.DeserializeObject<List<ivc_test_result>>(getRest(sourceurl));

                if (oldresults.Count > 0)
                {
                    Console.WriteLine("{0} : {1}", oldresults[0].name, oldresults[0].status);
                    updateResult(target, currentOne.submodule, currentOne.name, oldresults[0].status);
                }

            }
        }
        public static void updateResult(string testsetname, string customer, string testname, string result)
        {

            Dictionary<string, string> query = new Dictionary<string, string>()
                {
                    {"testsetname",testsetname},
                    {"name",testname},
                    {"submodule", customer }
                };

            Dictionary<string, string> putMongo = new Dictionary<string, string>() { { "status", result } };

            Dictionary<string, string> jsonData = new Dictionary<string, string>();
            jsonData.Add("query", JsonConvert.SerializeObject(query));
            jsonData.Add("data", JsonConvert.SerializeObject(putMongo));

            string url = string.Format("{0}results/update", mongoRest);
            putMongoDB(url, JsonConvert.SerializeObject(jsonData));
        }
        public static void CreateReleaseExecutionCycle_Old(string repackname, string driverelease)
        {
            #region Release Scheduler Mapper


            #endregion
            #region Get the Scheduled Sites Information
            string codeversion = getPackVersion("Drive", repackname);
            Dictionary<string, string> release = new Dictionary<string, string>();
            release.Add("version", driverelease);
            string query = JsonConvert.SerializeObject(release);
            string req_url = csdtRestURL + "schedule/events?query=" + query;
            List<ReleaseScheduler> siteDetails = JsonConvert.DeserializeObject<List<ReleaseScheduler>>(getRest(req_url)).FindAll(t => !t.environment.ToLower().Equals("internal"));
            #endregion
            #region Get the Deployment Tests
            Dictionary<string, string> create = new Dictionary<string, string>();

            Jira jira = new Jira("svc_autoline_ivc", "6xL@tCdw]/", "https://projects.cdk.com");
            jProject jp = jira.Project("IDRIVE");
            List<zTest> ztl = jira.DeploymentTestsInCodeVersion(codeversion);
            #endregion
            #region Add the Tests to Mongo DB
            jVersion currentVersion;
            currentVersion = jp.versions.Find(t => t.name.Contains(codeversion));
            string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + repackname + "-" + DateTime.Now.ToString("hhmmsstt");
            foreach (ReleaseScheduler tmpRelease in siteDetails)
            {
                foreach (zTest tmp in ztl)
                {

                    create.Clear();
                    create.Add("name", tmp.fields.ScriptID);
                    create.Add("summary", tmp.fields.summary);
                    create.Add("testid", tmp.id);

                    if (!string.IsNullOrEmpty(tmp.fields.description))
                        create.Add("description", Regex.Replace(tmp.fields.description, @"[^a-zA-Z0-9 ]", "", RegexOptions.Compiled));
                    else
                        create.Add("description", "");

                    create.Add("status", "No Run");
                    create.Add("testsetid", "-1");
                    create.Add("duration", "0");
                    create.Add("host", "Not Applicable");
                    create.Add("success", "100");
                    create.Add("author", tmp.fields.creator != null ? tmp.fields.creator.displayName : "ivcauto");
                    create.Add("created", tmp.fields.components[0].name);
                    create.Add("runner", "Default");
                    create.Add("F2US", "9999");
                    create.Add("IVUS", tmp.key);

                    if (!string.IsNullOrEmpty(tmpRelease.host_relpri))
                    {
                        if (relMaps.Keys.Contains(tmpRelease.host_relpri.ToLower()))
                        {
                            create.Add("module", relMaps[tmpRelease.host_relpri.ToLower()]);
                        }
                        else
                        {
                            create.Add("module", tmpRelease.host_relpri.ToLower());
                        }
                    }
                    else
                    {
                        create.Add("module", "No Group");
                    }


                    create.Add("submodule", tmpRelease.host_name);
                    create.Add("suitename", tmp.fields.labels[0]);
                    create.Add("executionid", "-1");
                    create.Add("testsetname", testSetName);
                    create.Add("packname", repackname);
                    //create result set
                    string mongoposttext = JsonConvert.SerializeObject(create);
                    Console.WriteLine("Posted on MongoDB result: " + mongoposttext);
                    postMongo(string.Format("{0}/results/insert/", mongoRest), mongoposttext);

                }
            }




            #endregion
            #region Update the Release Collection
            create.Clear();
            create.Add("ivccodeversion", "MT");
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "ReleaseTesting");
            create.Add("testsetid", "99999");
            create.Add("packname", "ReleaseTesting");
            create.Add("product", "Drive");
            create.Add("date", DateTime.Now.ToString("dd-MM-yyyy"));
            create.Add("file_created", DateTime.Now.ToString("s"));
            create.Add("last_modified", DateTime.Now.ToString("s"));
            create.Add("last_modified_notification", "");
            postMongo(string.Format("{0}/releases/insert/?data={1}", mongoRest, JsonConvert.SerializeObject(create)));
            #endregion

        }
        public static void AddBCTSToReleaseDashBoard(string repackname, string groupname, string customername, string label, string testSetName)
        {
            #region Get the Deployment Tests
            Dictionary<string, string> create = new Dictionary<string, string>();
            Jira jira = new Jira("svc_autoline_ivc", "6xL@tCdw]/", "https://projects.cdk.com");
            jProject jp = jira.Project("IDRIVE");
            if (customername != null)
            {
                customername = customername.Replace("%", " ");
            }
            List<zTest> ztl = jira.TestsByLabel(label);

            #endregion
            foreach (zTest tmp in ztl)
            {

                create.Clear();
                create.Add("name", tmp.fields.ScriptID);
                create.Add("summary", tmp.fields.summary);
                create.Add("testid", tmp.id);

                if (!string.IsNullOrEmpty(tmp.fields.description))
                    create.Add("description", Regex.Replace(tmp.fields.description, @"[^a-zA-Z0-9 ]", "", RegexOptions.Compiled));
                else
                    create.Add("description", "");

                create.Add("status", "No Run");
                create.Add("testsetid", "-1");
                create.Add("duration", "0");
                create.Add("host", "Not Applicable");
                create.Add("success", "100");
                create.Add("author", tmp.fields.creator != null ? tmp.fields.creator.displayName : "ivcauto");
                create.Add("created", tmp.fields.components[0].name);
                create.Add("runner", "Default");
                create.Add("F2US", "9999");
                create.Add("IVUS", tmp.key);

                if (!string.IsNullOrEmpty(groupname))
                {
                    if (relMaps.Keys.Contains(groupname.ToLower()))
                    {
                        create.Add("module", relMaps[groupname.ToLower()]);
                    }
                    else
                    {
                        create.Add("module", groupname);
                    }
                }
                else
                {
                    create.Add("module", "No Group");
                }


                create.Add("submodule", customername);
                create.Add("suitename", tmp.fields.labels[0]);
                create.Add("executionid", "-1");
                create.Add("testsetname", testSetName);
                create.Add("packname", repackname);
                //create result set
                string mongoposttext = JsonConvert.SerializeObject(create);
                Console.WriteLine("Posted on MongoDB result: " + mongoposttext);
                postMongo(string.Format("{0}/results/insert/", mongoRest), mongoposttext);

            }
        }
        public static int AddBCTSToReleaseDashBoard(AddBCTSToReleaseDashBoardOptions opts)
        {
            #region Get the Deployment Tests
            Dictionary<string, string> create = new Dictionary<string, string>();
            Jira jira = new Jira("svc_autoline_ivc", "6xL@tCdw]/", "https://projects.cdk.com");
            jProject jp = jira.Project("IDRIVE");
            if (opts.CustomerName != null)
            {
                opts.CustomerName = opts.CustomerName.Replace("%", " ");
            }
            List<zTest> ztl = jira.TestsByLabel(opts.Label);

            #endregion
            foreach (zTest tmp in ztl)
            {

                create.Clear();
                create.Add("name", tmp.fields.ScriptID);
                create.Add("summary", tmp.fields.summary);
                create.Add("testid", tmp.id);

                if (!string.IsNullOrEmpty(tmp.fields.description))
                    create.Add("description", Regex.Replace(tmp.fields.description, @"[^a-zA-Z0-9 ]", "", RegexOptions.Compiled));
                else
                    create.Add("description", "");

                create.Add("status", "No Run");
                create.Add("testsetid", "-1");
                create.Add("duration", "0");
                create.Add("host", "Not Applicable");
                create.Add("success", "100");
                create.Add("author", tmp.fields.creator != null ? tmp.fields.creator.displayName : "ivcauto");
                create.Add("created", tmp.fields.components[0].name);
                create.Add("runner", "Default");
                create.Add("F2US", "9999");
                create.Add("IVUS", tmp.key);

                if (!string.IsNullOrEmpty(opts.GroupName))
                {
                    if (relMaps.Keys.Contains(opts.GroupName.ToLower()))
                    {
                        create.Add("module", relMaps[opts.GroupName.ToLower()]);
                    }
                    else
                    {
                        create.Add("module", opts.GroupName);
                    }
                }
                else
                {
                    create.Add("module", "No Group");
                }


                create.Add("submodule", opts.CustomerName);
                create.Add("suitename", tmp.fields.labels[0]);
                create.Add("executionid", "-1");
                create.Add("testsetname", opts.TestSetName);
                create.Add("packname", opts.PackName);
                //create result set
                string mongoposttext = JsonConvert.SerializeObject(create);
                Console.WriteLine("Posted on MongoDB result: " + mongoposttext);
                postMongo(string.Format("{0}/results/insert/", mongoRest), mongoposttext);

            }

            return 0;
        }
        public static void PostMultipleFiles(string url, string file)
        {
            string boundary = DateTime.Now.Ticks.ToString("x");
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            httpWebRequest.Method = "POST";
            httpWebRequest.KeepAlive = true;
            httpWebRequest.Accept = "application/json, application/xml, text/json, text/x-json, text/javascript, text/xml";
            httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip;
            //httpWebRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;
            Stream memStream = new System.IO.MemoryStream();
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            string formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition:  form-data; file=\"{0}\";\r\n\r\n{1}";
            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: text/xml\r\n\r\n";
            memStream.Write(boundarybytes, 0, boundarybytes.Length);
            string header = string.Format(headerTemplate, "file", Path.GetFileName(file));
            //string header = string.Format(headerTemplate, "uplTheFile", files[i]);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            memStream.Write(headerbytes, 0, headerbytes.Length);
            FileStream fileStream = new FileStream(file, FileMode.Open,
            FileAccess.Read);
            byte[] buffer = new byte[1024];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                memStream.Write(buffer, 0, bytesRead);
            }
            boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            memStream.Write(boundarybytes, 0, boundarybytes.Length);
            fileStream.Close();
            httpWebRequest.ContentLength = memStream.Length;
            Stream requestStream = httpWebRequest.GetRequestStream();
            memStream.Position = 0;
            byte[] tempBuffer = new byte[memStream.Length];
            memStream.Read(tempBuffer, 0, tempBuffer.Length);
            memStream.Close();
            requestStream.Write(tempBuffer, 0, tempBuffer.Length);
            requestStream.Close();
            try
            {
                HttpWebResponse webResponse = httpWebRequest.GetResponse() as HttpWebResponse;
                Stream stream = webResponse.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string var = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            httpWebRequest = null;
            memStream.Flush();
        }
        public static void RunDeploymentTestsBoxswap(string server, string service, string boxswap)
        {
            XmlDocument configxml = new XmlDocument();
            configxml.Load("ARIIVC.DriveDeployment.dll.config");
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='server']").Attributes["value"].Value = server;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='service']").Attributes["value"].Value = service;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='updatedashboard']").Attributes["value"].Value = "true";
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='boxswap']").Attributes["value"].Value = boxswap;
            configxml.SelectSingleNode("//configuration//appSettings//add[@key='kpath']").Attributes["value"].Value = "C:\\ADP\\KCMLMT";

            configxml.Save("ARIIVC.DriveDeployment.dll.config");

            string nunitexe = "C:\\Program Files (x86)\\NUnit 2.6.3\\bin\\nunit-console-x86.exe";
            ProcessStartInfo psInfo = new ProcessStartInfo(nunitexe);
            psInfo.Arguments = String.Format(" /xml:deploymenttests_boxswap.xml ARIIVC.DriveDeployment.dll");
            Process pNunit = Process.Start(psInfo);
            pNunit.WaitForExit();
        }
        public static void RunCompletedDeploymentSites(string driverelease, string date)
        {
            Dictionary<string, string> release = new Dictionary<string, string>();
            release.Add("date", date);
            release.Add("version", driverelease);

            string req_url = csdtRestURL + "schedule/events?query=" + JsonConvert.SerializeObject(release);
            List<csdtschedule> siteDetails = JsonConvert.DeserializeObject<List<csdtschedule>>(getRest(req_url)).FindAll(t => !t.environment.ToLower().Equals("internal"));
            int i = 0;
            int iterator = 0;
            foreach (csdtschedule top in siteDetails)
            {
                if (top.status.ToLower().Equals("completed"))
                {
                    #region get Release Update Details
                    Dictionary<string, string> rDetails = new Dictionary<string, string>();
                    rDetails.Add("ip_addr", top.ip_addr);
                    rDetails.Add("service", top.service);
                    string release_url = mongoRest + "customer-checks/get?query=" + JsonConvert.SerializeObject(rDetails);
                    List<prod_hosts> prodhosts = JsonConvert.DeserializeObject<List<prod_hosts>>(getRest(release_url));//.FindAll(t => !t.status.ToLower().Equals("triggered"));
                    #endregion

                    if (prodhosts.Count > 0)
                    {
                        if (prodhosts[0].runstatus == null || prodhosts[0].runstatus.ToLower().Equals("not-run") || prodhosts[0].runstatus.ToLower().Equals("in-progress"))
                        {

                            Jenkins jenkins = new Jenkins(jenkinsURL);
                            Dictionary<string, string> jobParams = new Dictionary<string, string>();
                            jobParams.Add("ip_addr", top.ip_addr);
                            jobParams.Add("service", top.service);
                            jobParams.Add("envcode", prodhosts[0].envcode);
                            jobParams.Add("version", prodhosts[0].version);
                            jobParams.Add("relid", prodhosts[0].relid);
                            jobParams.Add("mrepo_server_url", "http://gbhpdslivcweb01.dsi.ad.adp.com/ivc");
                            jobParams.Add("ip_addr", top.ip_addr);
                            jobParams.Add("runuser", "kcc");
                            jenkins.TriggerJob("DEPLOYMENT_SMOKE_TESTS", jobParams);

                            //Need to change this 
                            Dictionary<string, string> query = new Dictionary<string, string>();
                            query.Add("ip_addr", top.ip_addr);
                            query.Add("service", top.service);
                            Dictionary<string, string> putMongo = new Dictionary<string, string>();
                            putMongo.Add("status", "triggered");
                            string url = string.Format("{0}customer-checks/update_hosts/?query={1}&data={2}", mongoRest, JsonConvert.SerializeObject(query), JsonConvert.SerializeObject(putMongo));
                            putRest(url);
                            i++;
                            
                            iterator++;
                        }

                    }
                }

                if (iterator > 10)
                    break;
            }

            if (siteDetails.Count > 0)
                SendDeploymentTestsMail(siteDetails, "Santhosh.Koyyana@cdk.com;Pradeep.Vegesna@cdk.com;Kiran.Karre@cdk.com", driverelease);

        }
        public static void GetDeploymentSummary(string driverelease, string date)
        {

            #region Refresh Prod Hosts
            string refresh_pro_hosts = "http://" + dashboardserver + "/ivc/dynamic/refresh_prodhosts.php";
            getRest(refresh_pro_hosts);
            #endregion

            Dictionary<string, string> release = new Dictionary<string, string>();
            release.Add("date", date);
            release.Add("version", driverelease);

            string req_url = csdtRestURL + "schedule/events?query=" + JsonConvert.SerializeObject(release);
            List<csdtschedule> siteDetails = JsonConvert.DeserializeObject<List<csdtschedule>>(getRest(req_url)).FindAll(t => !t.environment.ToLower().Equals("internal"));
            if (siteDetails.Count > 0)
                SendDeploymentTestsMail(siteDetails, "Santhosh.Koyyana@cdk.com;Pradeep.Vegesna@cdk.com;Kiran.Karre@cdk.com", driverelease);

        }
        public static void TestRingAdmin()
        {
            ReleaseServicesService sas = new ReleaseServicesService();
            string response = "";
            sas.GetRingMasters(ref response);
        }
        public static void SendDeploymentTestsMail(List<csdtschedule> scheduledsites, string sendTo, string version)
        {
            #region Build the HTML file
            StringBuilder stBuilder = new StringBuilder();
            stBuilder.AppendLine("<html><body><b>IVC Deployment Site Checks</b>");
            stBuilder.AppendLine("<p style=\"font-family:verdana; color:Black; font-size:12px\">");
            stBuilder.AppendLine(String.Format("<p style=\"font-family:verdana; color:Black; font-size:12px\">Please find the results summary<br/></p>"));
            stBuilder.AppendLine("<table border=\"1\" width=\"80%\" cellpadding=\"1\" style=\"font-family:Verdana; font-size:13px\"");
            stBuilder.AppendLine("<tr><td><b>Version</b></td>");
            stBuilder.AppendLine("<td><b>Host Name</b></td>");
            stBuilder.AppendLine("<td><b>Install Status</b></td>");
            stBuilder.AppendLine("<td><b>Run Status</b></td>");
            stBuilder.AppendLine("<td><b>Environment Code</b></td>");
            stBuilder.AppendLine("<td><b>IP Address</b></td>");
            stBuilder.AppendLine("<td><b>Service</b></td>");
            stBuilder.AppendLine("<td><b>Schedule Group</b></td></tr>");

            foreach (csdtschedule top in scheduledsites)
            {
                if (true)
                {
                    #region get Release Update Details
                    Dictionary<string, string> rDetails = new Dictionary<string, string>();
                    rDetails.Add("name", HttpUtility.UrlEncode(top.host_name));
                    //rDetails.Add("service", top.service);
                    string release_url = "http://" + dashboardserver + "/ivc/rest/customer-checks/get?query=" + JsonConvert.SerializeObject(rDetails);
                    List<prod_hosts> prodhosts = JsonConvert.DeserializeObject<List<prod_hosts>>(getRest(release_url));
                    #endregion
                    //Console.WriteLine("URL : {0}", System.Uri.EscapeDataString(release_url));
                    string currentstatus = "NOT KNOWN";
                    string envcode = "Not Registered";
                    string server = string.Empty;
                    string service = string.Empty;
                    if (prodhosts.Count > 0)
                    {
                        Console.WriteLine("The host is : {0}", prodhosts[0].name);
                        if (!string.IsNullOrEmpty(prodhosts[0].runstatus))
                            currentstatus = prodhosts[0].runstatus;

                        envcode = prodhosts[0].envcode;
                        server = prodhosts[0].ip_addr;
                        service = prodhosts[0].service;
                    }

                    if (currentstatus.ToLower().Equals("passed"))
                    {
                        stBuilder.AppendLine(string.Format("<tr bgcolor=\"#33cc33\"><td>{0}</td>", top.version));
                    }
                    else if (currentstatus.ToLower().Equals("failed"))
                    {
                        stBuilder.AppendLine(string.Format("<tr bgcolor=\"red\"><td>{0}</td>", top.version));
                    }
                    else if (currentstatus.ToLower().Equals("in-progress"))
                    {
                        stBuilder.AppendLine(string.Format("<tr bgcolor=\"#99ffff\"><td>{0}</td>", top.version));
                    }
                    else
                    {
                        stBuilder.AppendLine(string.Format("<tr><td>{0}</td>", top.version));
                    }

                    stBuilder.AppendLine(string.Format("<td>{0}</td>", top.host_name));
                    stBuilder.AppendLine(string.Format("<td>{0}</td>", top.status));
                    stBuilder.AppendLine(string.Format("<td><b>{0}</b></td>", currentstatus));
                    stBuilder.AppendLine(string.Format("<td>{0}</td>", envcode));
                    stBuilder.AppendLine(string.Format("<td>{0}</td>", server));
                    stBuilder.AppendLine(string.Format("<td>{0}</td>", service));
                    stBuilder.AppendLine(string.Format("<td>{0}</td></tr>", top.host_relpri));
                }
            }


            stBuilder.AppendLine("</table>");
            stBuilder.AppendLine("<p style=\"font-family:verdana; color:Black; font-size:13px\"><br/><br/><b>Thanks,</b><br/>IVC Team<br/></p></body></html>");

            #endregion
            #region send mail

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("ReleaseTesting@cdk.com");
            string[] splitemail = sendTo.Split(';');
            foreach (string tmp in splitemail)
            {
                mail.To.Add(tmp);
            }

            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = "dsirelay.gbh.dsi.adp.com";
            mail.Subject = String.Format("IVC Release Smoke Tests | {0} | {1}", version, DateTime.Now.ToShortDateString());
            mail.Body = stBuilder.ToString();
            mail.IsBodyHtml = true;
            client.Send(mail);


            #endregion
        }
        #endregion

        #region Method to create dashboard for RRT Browser tests
        public static int CreateDashboardWithBrowserTests(CreateBrowserDashboardOptions opts)
        {
            DashboardConnector connector = new DashboardConnector();
            string testSetName = DateTime.Now.ToString("dd-MM-yyyy") + "_" + opts.Pack + "-" +
                                 DateTime.Now.ToString("hhmmsstt");
            string codeVersion = "BT";
            SchedulerLogger.Log(testSetName, "Started creating Dashboard with name" + testSetName);
            RestCall call = new RestCall();

            // Get tests from Zephyr
            zapi zephyrApi = new zapi(jirauser, jirapassword, "https://projects.cdk.com");

            List<zTest> testsToExecute = zephyrApi.GetBrowserTests("RRT.ReleaseTesting.Vehicles", "In Progress");

            SchedulerLogger.Log(testSetName, "Pulling test cases from Zephyr according to Label");

            Dictionary<string, object> create = new Dictionary<string, object>();

            #region Correct if SuiteID is not there

            foreach (zTest tmp in testsToExecute)
            {
                if (string.IsNullOrEmpty(tmp.fields.SuiteID))
                {
                    tmp.fields.SuiteID = tmp.fields.ScriptID;
                }
            }

            #endregion

            #region Post each test to MongoDB

            SchedulerLogger.Log(testSetName, "Adding browser tests to MongoDB");
            foreach (var testToExecute in testsToExecute)
            {
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
                create.Add("author", testToExecute.fields.creator.displayName);
                create.Add("created", testToExecute.fields.versions[0].name);
                create.Add("runner", "Default");
                create.Add("F2US", "9999");
                create.Add("IVUS", testToExecute.key);
                create.Add("module", testToExecute.fields.components[0].name);
                create.Add("submodule", testToExecute.fields.labels[0]);
                create.Add("logs", new Jenkins().TestScheduled());
                create.Add("success", "ivctest");
                var suiteName = testToExecute.fields.SuiteID ?? testToExecute.fields.ScriptID;
                create.Add("suitename", suiteName);
                create.Add("testsetname", testSetName);
                create.Add("packname", opts.Pack);
                call = new RestCall() { Url = string.Format("{0}/results/insert/", mongoRest) };
                call.Post(create);

            }

            #endregion

            create.Clear();

            #region Update the releases table and recent_releases tab
            create.Add("ivccodeversion", opts.Pack.ToLower().Contains("mt") ? "MT" : codeVersion);
            create.Add("testsetname", testSetName);
            create.Add("pack_version", "Drive-" + opts.Pack);
            create.Add("testsetid", "-1");
            create.Add("packname", opts.Pack);
            create.Add("product", opts.Product);
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
            #endregion

            return 0;
        }
        #endregion
    }
}