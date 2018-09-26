using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Scheduler;
using ARIIVC.Scheduler.JsonReps;
using ARIIVC.Utilities.JsonRepo;
using CommandLine;
using Newtonsoft.Json;
using zephyrapi;

namespace ARIIVC.SchedulerDerived
{
    class SchedulerDerived : SchedulerBase
    {
        public static string ivcMongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
        public static string iqaMongoUrl = "mongodb://gbhpiqaweb01.dsi.ad.adp.com:27017";
        public static string jiraUrl = "https://projects.cdk.com";

        static int Main(string[] args)
        {
            return Parser.Default
                .ParseArguments<ExecutionSnapshotOptions, GetProductFeatureOptions, GetFirstRunCountOptions,
                    PerformSanityChecksOptions, ZephyrTestsPullerOptions,CreateBrowserDashboardOptions>(args)
                .MapResult((ExecutionSnapshotOptions opts) => TakeExecutionSnapshot(opts),
                    (GetProductFeatureOptions opts) => GetProductFeatureInfo(opts),
                    (GetFirstRunCountOptions opts) => GetFirstRunCount(opts),
                    (PerformSanityChecksOptions opts) => RegressionSanityChecks(opts),
                    (ZephyrTestsPullerOptions opts) => ZephyrTestsPuller(),
                    (CreateBrowserDashboardOptions opts) => SchedulerBase.CreateDashboardWithBrowserTests(opts),
                    errs => 1);
        }

        public static int TakeExecutionSnapshot(ExecutionSnapshotOptions opts)
        {

            string[] splitArray = opts.Pack.Split(',');
            foreach (string pack in splitArray)
            {
                DashboardConnector connector = new DashboardConnector();
                string snapShotData = connector.GetSnapShot(pack);
                connector.PostSnapShot(snapShotData);
            }
            return 0;
        }

        public static int GetProductFeatureInfo(GetProductFeatureOptions opts)
        {
            DashboardConnector connector = new DashboardConnector();
            string testSetName = connector.GetTestsetNameForPack(opts.Product, opts.Pack);
            List<ivc_appserver> masterAppServer = connector.GetAllAppServersForPackSetup(opts.Product, opts.Pack)
                .FindAll(t => t.runconfig.ToLower().Equals("master"));

            SchedulerLogger.Log(testSetName,
                string.Format("Scheduled script to get product feature info : {0} on appserver : {1}", opts.Script,
                    masterAppServer.First().hostname));

            //get the release details
            ivc_pack_details targetPack = connector.GetPackDetails(testSetName);
            SchedulerLogger.Log(testSetName,
                string.Format("Execution started for script : {0} on appserver : {1} to get product feature info",
                    opts.Script, masterAppServer.First().hostname));

            ivc_appserver serverDetails =
                connector.GetServerKCMLService(opts.Product, opts.Pack, masterAppServer.First().hostname);

            //get and unzip the test libraries            
            Regression_GetTestLibraries(targetPack.ivccodeversion, targetPack.packname);

            //update the configuration file
            ScrumAutomation_UpdateConfigurationFile(serverDetails.hostname, serverDetails.service, testSetName,
                serverDetails.kpath, serverDetails.serverurl, serverDetails.portno, targetPack);

            Console.WriteLine("Output is : {0}", File.ReadAllText("TestLibraries\\Drive.config"));

            //create Nunit Runlist file
            CreateNUnitRunList(opts.Script);

            int timeout = 300;
            string subscribers = "anusha.engu@cdk.com;Narsingsingh.Rajput@cdk.com";
            //Trigger the execution
            TriggerExecution(targetPack.packname, serverDetails.hostname, testSetName, opts.Script, "Get_Product_Feature_Data",
                subscribers, timeout * 1000);

            return ReadJsonAndPostFeatureData(opts.Pack);
        }

        public static int ReadJsonAndPostFeatureData(string pack)
        {
            try
            {
                ProductFeatureTab productFeatureTabs =
                    JsonConvert.DeserializeObject<ProductFeatureTab>(
                        File.ReadAllText(Directory.GetCurrentDirectory() + @"\testdata\productFeature.json"));
                ProductFeatureData postProductFeatureData = new ProductFeatureData();
                postProductFeatureData.InsertFeatureInfo(productFeatureTabs, pack);
            }
            catch (Exception e)
            {
                Console.WriteLine("Json file not found in the testdata folder | Exception occured :" +  e);
                return -1;
            }
            return 0;
        }

        public static int GetFirstRunCount(GetFirstRunCountOptions opts)
        {
            string mongoUrl = opts.Product.ToLower().Equals("drive") ? ivcMongoUrl : iqaMongoUrl;

            MongoDriver driver = new MongoDriver(mongoUrl);
            
            string testSetName = driver.Releases.GetTestSetName(opts.Product, opts.Pack);

            List<ivc_test_result> testList = driver.Results.GetAllTests(testSetName);

            MailTestResultNotification notification = new MailTestResultNotification
            {
                IvcPack = opts.Pack,
                TestSetName = testSetName,
                NotificationTargets = opts.Subscribers
            };
            notification.SendFrcMail(testList);
            //string csvFileName =
            //    string.Format("{0:dd-MM-yyyy}_{1:HH-mm-ss-tt}_{2}_{3}.csv", DateTime.Now, DateTime.Now, opts.Pack,
            //        opts.Product);

            //CreateCsvFile(csvFileName, testList);
          
            return 0;
        }

        public static void CreateCsvFile(string fileName, List<ivc_test_result> testList)
        {

            StringBuilder sb = new StringBuilder();
            List<string> header = new List<string>
            {
                "Test Name",
                "Suite Name",
                "Test Status",
                "Counter",
                "Description",
                "Module",
                "Submodule",
                "Runnner"
            };

            sb.AppendLine(string.Join(",", header));

            foreach (var test in testList)
            {
                List<string> singleLine = new List<string>
                {
                    test.name,
                    test.suitename,
                    test.status,
                    Convert.ToString(test.counter),
                    test.description,
                    test.module,
                    test.submodule,
                    test.runner
                };

                sb.AppendLine(string.Join(",", singleLine));
            }
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), fileName), sb.ToString());
        }

        public static int RegressionSanityChecks(PerformSanityChecksOptions opts)
        {
            DashboardConnector connector = new DashboardConnector();
            ivc_appserver serverDetails = connector.GetAllAppServersForPackSetup(opts.Product, opts.Pack, "Core").Find(t => t.hostname.Contains(opts.Slave));

            //get and unzip the test libraries
            Regression_GetTestLibraries("MT", "MT");

            //update the configuration file
            SanityCheckUpdateConfigurationFile(serverDetails.hostname, serverDetails.service, serverDetails.kpath, serverDetails.serverurl, opts.SystemVersion, opts.UpdateId, serverDetails.portno);

            Console.WriteLine("Output is : {0}", File.ReadAllText("TestLibraries\\Drive.config"));

            ////create Nunit Runlist file
            CreateNUnitRunList("SanityChecksForIvcPacks");

            int timeout = 300;
            string subscribers = "Akash.Sahu@cdk.com;Sastry.Poranki@cdk.com;NagaVaishnavi.Gullapally@cdk.com";
            //Trigger the execution
            TriggerExecution(opts.Pack, serverDetails.hostname, "SanityCheckTestSet", "SanityChecksForIvcPacks", "Sanity_Checks", subscribers, (timeout * 1000));
            MailTestResultNotification notification = new MailTestResultNotification
            {
                TestResultXmlFile = "nunit-test-SanityChecksForIvcPacks.xml"
            };
            string testResult = notification.GetResultStatusString();
            

            if (testResult.ToLower().Equals("passed"))
                return 0;
            else
            {
                return -1;
            }
        }

        public static int ZephyrTestsPuller()
        {
            zapi zephyrApi = new zapi(jirauser, jirapassword, jiraUrl);

            List<zTest> allZephyrTests = zephyrApi.getAllZephyrTests();
            MongoDriver mongoDriver = new MongoDriver();
            mongoDriver.zephyrTests.InsertLatestTests(allZephyrTests);
            return 0;
        }
    }
}
