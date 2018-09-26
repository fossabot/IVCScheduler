using System;
using System.Collections.Generic;
using MongoDB.Driver;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json;
using System.IO;
using ARIIVC.Scheduler.JsonReps;
using ARIIVC.Scheduler;
using ARIIVC.Accelerator;
using ARIIVC.Utilities.JsonRepo;
using ARIIVC.Utilities;

namespace ARIIVC.Accelerator.Tests
{
    [TestFixture]
    public class AcceleraorUnitTest
    {
        #region Variables
        private string version = "MT";
        private string _workflowname = "ARIIVC.Marketing.Tests.CMTestsDrive2.CMCRM16105001_D2";
        public string mongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
        List<string> filelist = new List<string>();
        public static int total_func_count_xml = 0;        
        public string Version { get { return version; } set { version = value; } }
        public string WorkflowName { get { return _workflowname; } set { _workflowname = value; } }
        AcceleratorXmlParser parser = new AcceleratorXmlParser();
   
        #endregion
        #region Reusables
        public AcceleratorXmlParser UT_ProcessXML(string remotePath = "")
        {
            total_func_count_xml = 0;
            List<ProfileParameters> profileparmeters = JsonConvert.DeserializeObject<List<ProfileParameters>>(File.ReadAllText("ProfileParametersTests.json"));
            AcceleratorMongo acm = new AcceleratorMongo(workflows: "workflows_test");
            ProfileParameters _profileparmeters = profileparmeters.Find(t => t.version.Equals(Version.ToUpper()));
            filelist = Ftp.FileList(_profileparmeters.mountPoint, remotePath, _profileparmeters.user, _profileparmeters.password);
            List<string> newFiles = filelist.FindAll(t => (t.Contains(WorkflowName)));
            Ftp.DownloadFiles(_profileparmeters.mountPoint, remotePath, newFiles, _profileparmeters.user, _profileparmeters.password, Path.Combine(Directory.GetCurrentDirectory()));
            AcceleratorXmlParser parser = new AcceleratorXmlParser(_workflowname);
            total_func_count_xml=parser.ProcessXmlFiles(newFiles);
            return parser;
        }
        public void UT_UpdateWorkflow(AcceleratorXmlParser _parser)
        {
            
            if (string.IsNullOrEmpty(_parser.Error))
            {
                AcceleratorMongo workflowMongo = new AcceleratorMongo(mongoUrl,workflows: "workflows_test")
                {
                    Version = Version,
                    WorkflowName = WorkflowName,
                    Functions = _parser.Functions
                };
                workflowMongo.UpdateWorkflows();
            }
        }
        public int UT_GetFunctionsCountWorkFlow()
        {
            AcceleratorMongo acm = new AcceleratorMongo(workflows: "workflows_test");
            List<Workflow> allFunctionsForVersion = acm._workflowstest.Find(t => t.Version == Version).ToList();
            List<Workflow> FunctionsWithWorkflow = allFunctionsForVersion.FindAll(t => t.Workflows.Contains(WorkflowName));
            return FunctionsWithWorkflow.Count;
        }
        #endregion
        #region Tests
        #region DB
        [Test,Description("Verify tracebility data is correctly inserted for a workflow to MongoDB")]
        public void DB_Insert()
        {
            int total_func_count_DB = 0;
            string remotePath = "insert";
            parser = UT_ProcessXML(remotePath);
            UT_UpdateWorkflow(parser);
            total_func_count_DB=UT_GetFunctionsCountWorkFlow();
            Assert.AreEqual(total_func_count_DB, 6, "Validate Inserted methods");
        }
        [Test, Description("Verify tracebility data is correctly updated for a workflow to MongoDB")]
        public void DB_Update()
        {
            int total_func_count_DB = 0;
            string remotePath = "update";
            parser = UT_ProcessXML(remotePath);
            UT_UpdateWorkflow(parser);           
            total_func_count_DB = UT_GetFunctionsCountWorkFlow();
            Assert.AreEqual(total_func_count_DB,8, "Validate Inserted methods");
        }
        [Test,Description("Verify tracebility data is correctly removed for a workflow to MongoDB")]
        public void DB_Remove()
        {
            int total_func_count_DB = 0;
            string remotePath = "remove";
            parser = UT_ProcessXML(remotePath);
            UT_UpdateWorkflow(parser);
            UT_UpdateWorkflow(parser);
            total_func_count_DB = UT_GetFunctionsCountWorkFlow();
            Assert.AreEqual(total_func_count_DB, 7, "Validate Inserted methods");
        }
        [Test, Description("Validate timestamp in lastupdated table")]
        public void DB_LastUpdated()
        {
            #region get the relevant tests from the test set
            MongoDriver driver = new MongoDriver();
            string testSetName = driver.Releases.GetTestSetName("Drive", Version);
            Console.WriteLine("Considering test set {0} for Result Validation", testSetName);
            List<ivc_test_result> allTests = driver.Results.GetAllTests(testSetName);            
            var testcasename = WorkflowName.Split('.').Last();       
            var currentTest = allTests.Find(t => t.name.ToLower().Equals(testcasename.ToLower()));
            #endregion
            #region Verify Last update as per test Status
         
            if (currentTest.status.ToLower() == "passed")
            {
                var time = DateTime.Now.ToString("dd-MMM-yy h:mm:ss tt");
                Accelerator.UpdateLatestRun(WorkflowName, version, lastupdated: "lastupdated_test");
                AcceleratorMongo acceleratorMongo = new AcceleratorMongo(lastrun: "lastupdated_test");
                var lastupdateddata = acceleratorMongo._lastrun.Find(t => t.WorkflowName == WorkflowName).ToList();
                Assert.AreEqual(lastupdateddata[0].LastExecuted, time, "Time Stamp Updated Correctly");
                Console.WriteLine("Last status update for :{0} was on : {1}", testcasename, lastupdateddata[0].LastExecuted);
            }
            else
            {
                Console.WriteLine("Test status is failed for : {0} in : {1}", testcasename, testSetName);
            }
            #endregion
        }
		[Test,Description("Validate there are no duplicate functions in workflowtable")]
        public void DB_Workflow_DuplicateFunction()
        {
            List<string> unique_func = new List<string>();
            AcceleratorMongo acm = new AcceleratorMongo(workflows: "workflows_test");           
            List<Workflow> allFunctionsForVersion = acm._workflows.Find(t => t.Version == Version).ToList();
            for(int i=0;i<allFunctionsForVersion.Count;i++)
            {
                unique_func.Add(allFunctionsForVersion[i].FunctionName);
            }
            int unique_func_count = unique_func.Distinct().Count();
            Assert.AreEqual(allFunctionsForVersion.Count, unique_func_count, "Collection has unique functions");
        }
        [Test, Description("Validate there are no duplicate workflows in functions")]
        public void DB_Workflow_DuplicateWorkFlows()
        {
            string _dupfunc = "Main program @ Unknown";
            List<string> unique_workflow = new List<string>();
            AcceleratorMongo acm = new AcceleratorMongo(workflows: "workflows_test");
            List<Workflow> FuncName = acm._workflows.Find(t => t.FunctionName == _dupfunc).ToList();
            foreach(string wf in FuncName[0].Workflows)
            {
                unique_workflow.Add(wf);
            }         
            int unique_workflow_count = unique_workflow.Distinct().Count();
            Assert.AreEqual(unique_workflow_count, FuncName[0].Workflows.Count, "Collection has unique workflows");
        }
        [Test, Description("Validate only valid entries are posted to lastupdate table")]
        public void DB_Lastupated_ValidItems()
        {
            #region Get count of tests in Results collection
            MongoDriver driver = new MongoDriver();
            string testSetName = driver.Releases.GetTestSetName("Drive", Version);
            Console.WriteLine("Considering test set {0} for Result Validation", testSetName);
            List<ivc_test_result> allTests = driver.Results.GetAllTests(testSetName);
            #endregion
            #region Get count of tests in Lastupdated colleaction
            AcceleratorMongo acceleratorMongo = new AcceleratorMongo();
            var lastupdateddata = acceleratorMongo._lastrun.Find(t => t.Version == Version).ToList();
            #endregion
            Assert.AreEqual(allTests.Count(), lastupdateddata.Count(), "Verify the count in lastupdated vs results");
        }
        #endregion
        #region XML
        [Test, Description("Validate number of functions in xml")]
        public void XML_FunctionsCount()
        {
            string remotePath = "update";
            UT_ProcessXML(remotePath);
            Assert.AreEqual(total_func_count_xml, 8, "Validate number of functions in xml");
        }
	    [Test, Description("Delete XML files with zero methods")]
	    public void DeleteXMLwithZeroMethods()
	    {
		    string remotePath = "zero_methods";
		    string remotePath2 = "zero_methods_dest";
		    parser = UT_ProcessXML(remotePath);
		    if (parser.Functions.Count == 0)
		    {
			    List<ProfileParameters> profileparmeters = JsonConvert.DeserializeObject<List<ProfileParameters>>(File.ReadAllText("ProfileParametersTests.json"));
			    ProfileParameters _profileparmeters = profileparmeters.Find(t => t.version.Equals(Version.ToUpper()));
			    filelist = Ftp.FileList(_profileparmeters.mountPoint, remotePath, _profileparmeters.user, _profileparmeters.password);
			    List<string> newFiles = filelist.FindAll(t => (t.Contains(WorkflowName)));
			    Ftp.DownloadFiles(_profileparmeters.mountPoint, remotePath, newFiles, _profileparmeters.user, _profileparmeters.password, Path.Combine(Directory.GetCurrentDirectory()));
			    Ftp.UploadFile(_profileparmeters.mountPoint, remotePath2, Path.Combine(Directory.GetCurrentDirectory(), newFiles[0]), _profileparmeters.user, _profileparmeters.password);
		    }
	    }

	    [Test, Description("Delete XML files with zero size")]
	    public void DelteFilesWithZeroSize()
	    {
		    string remotePath = "zero_size";
		    string remotePath2 = "zero_size_dest";

			List<ProfileParameters> profileparmeters = JsonConvert.DeserializeObject<List<ProfileParameters>>(File.ReadAllText("ProfileParametersTests.json"));
			ProfileParameters _profileparmeters = profileparmeters.Find(t => t.version.Equals(Version.ToUpper()));
		    filelist = Ftp.FileList(_profileparmeters.mountPoint, remotePath, _profileparmeters.user, _profileparmeters.password);
		    List<string> newFiles = filelist.FindAll(t => (t.Contains(WorkflowName)));
		    if (newFiles[0].Length / 1024 == 0)
		    {
			    Ftp.DownloadFiles(_profileparmeters.mountPoint, remotePath, newFiles, _profileparmeters.user, _profileparmeters.password, Path.Combine(Directory.GetCurrentDirectory()));
			    Ftp.UploadFile(_profileparmeters.mountPoint, remotePath2, Path.Combine(Directory.GetCurrentDirectory(), newFiles[0]), _profileparmeters.user, _profileparmeters.password);
			}
		}
        [Test]
        public void Update()
        {
            string pack = "MT";
            CSDTConnector.CSDTConnector cSDTConnector = new CSDTConnector.CSDTConnector();
            string repmVersion = cSDTConnector.GetLatestAvailablePackage("dev", "bin");
            DashboardConnector connector = new DashboardConnector();    
            string lastsucessfulbuild = connector.GetLastInstalledRpmNumber(pack);
            AcceleratorMongo ac = new AcceleratorMongo();
            ac.UpdateBuildVersion(Convert.ToInt32(lastsucessfulbuild.Split('-')[1]), Convert.ToInt32(repmVersion.Split('-')[1]));
        }
	    #endregion
		#endregion
	}
}
