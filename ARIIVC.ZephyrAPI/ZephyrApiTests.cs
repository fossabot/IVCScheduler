using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using NUnit.Framework;

namespace zephyrapi.tests
{
    [TestFixture]
    public class zapitests
    {
        zapi zp;
        almapi alm;

        [TestFixtureSetUp]
        public void class_setup()
        {
            zp = new zapi("svc_autoline_ivc", "6xL@tCdw]/");
            alm = new almapi();
        }

        [Test]
        public void createZephyrTest()
        {

            jProject project = zp.getProject("IDRIVE");
            jVersion currentVersion = project.versions.Find(t => t.name.Equals("N1.55 Oct-2016"));
            zTest jiraticket = new zTest();
            jiraticket.fields.description = "Test ZAPI ";
            jiraticket.fields.summary = "Create a Bulk Purchase Order";
            jiraticket.fields.issuetype.name = "Test";
            jiraticket.fields.project = project;
            jiraticket.fields.components.Add(new jComponent("Vehicles"));
            jiraticket.fields.labels.Add("Bulk_Purchase_Order");
            jiraticket.fields.labels.Add("AVMBPO01");
            TestStatus ss = new TestStatus();
            ss.value = "Automated";
            jiraticket.fields.teststatus = ss;
            jiraticket.fields.SuiteID = "AVMBPO01";
            jiraticket.fields.ScriptID = "AVMBO01";
            zp.createTest(jiraticket);
        }
        [Test]
        public void MoveTestALM2Zephyr()
        {
            List<string> submodules = new List<string>();
            submodules.Add("Service POS");
            foreach (string submodule in submodules)
            {

                List<string> designsteps = new List<string>();
                //String submodule = System.Uri.EscapeUriString("Parts POS");
                string url = "http://dsalm.ds.ad.adp.com:8080/qcbin/rest/domains/DSI/projects/DSIAUTOLINE/tests?page-size=max&fields=id&query={status[automated];user-07[DriveOEM];user-04[\"" + submodule + "\"]}";

                jProject project = zp.getProject("IDRIVE");
                //jVersion currentVersion = project.versions.Find(t => t.name.Equals("N1.50 May-2016"));


                //1438
                Stream alltests = alm.get(url);
                XmlDocument alltestXML = new XmlDocument();
                alltestXML.Load(alltests);

                //zTest jr = alm.Alm2Zephyr("4877", ref designsteps, project);
                //jr.fields.versions.Add(currentVersion);
                //zTest test = zp.createTest(jr);
                //zp.AddTestSteps(test.id, designsteps);

                foreach (XmlNode tmp in alltestXML.SelectNodes("//Entities/Entity"))
                {
                    //zp = new zapi("siddarer", "SI45$%dI");//update this with username and password
                    //zp.reAuthenticate();
                    designsteps = new List<string>();
                    string testid = tmp.SelectSingleNode("Fields/Field[@Name='id']").SelectSingleNode("Value").InnerText;
                    Console.WriteLine(testid);
                    zTest jr = alm.Alm2Zephyr(testid, ref designsteps, project);
                    //jr.fields.versions.Add(currentVersion);
                    zTest test = zp.createTest(jr);
                    zp.AddTestSteps(test.id, designsteps);
                    designsteps.Clear();
                }
            }
        }
        [Test]
        public void getProjectDetails()
        {
            jProject jp = zp.getProject("IDRIVE");

            Console.WriteLine("Project for IDRIVE are : Key {0}, ID {1}, No of Components {2}, No of Issue Types {3} and No of Versions {4}", jp.key, jp.id, jp.components.Count, jp.issueTypes.Count, jp.versions.Count);
        }
        [Test]
        public void CreateZephyrTestCycle()
        {
            jProject jp = zp.getProject("IDRIVE");
            jVersion currentVersion = jp.versions.Find(t => t.name.Equals("N1.55 Oct-2016"));

            string startDay = String.Format("{0}/{1}/{2}", DateTime.Now.Day, DateTime.Now.ToString("MMM"), DateTime.Now.ToString("yy"));
            string endDay = String.Format("{0}/{1}/{2}", DateTime.Now.AddDays(1).Day, DateTime.Now.AddDays(1).ToString("MMM"), DateTime.Now.AddDays(1).ToString("yy"));

            Console.WriteLine(startDay + " : " + endDay);
            zTestCycle ztc = new zTestCycle(Convert.ToInt16(jp.id), "MT Regression Cycle", "Created from API", startDay, endDay, Convert.ToInt16(currentVersion.id));
            string cycleID = zp.createTestCycle(ztc);
            Console.WriteLine("Cycle ID : {0}", cycleID);

        }
        [Test]
        public void AddTests2TestCycle()
        {
            jProject jp = zp.getProject("IDRIVE");
            jVersion currentVersion = jp.versions.Find(t => t.name.Equals("N1.55 Oct-2016"));

            string startDay = String.Format("{0}/{1}/{2}", DateTime.Now.Day, DateTime.Now.ToString("MMM"), DateTime.Now.ToString("yy"));
            string endDay = String.Format("{0}/{1}/{2}", DateTime.Now.AddDays(1).Day, DateTime.Now.AddDays(1).ToString("MMM"), DateTime.Now.AddDays(1).ToString("yy"));

            Console.WriteLine(startDay + " : " + endDay);
            zTestCycle ztc = new zTestCycle(Convert.ToInt16(jp.id), DateTime.Now.ToShortDateString() + "MT Regression Cycle12", "Created from API by sharath", startDay, endDay, Convert.ToInt16(currentVersion.id));
            string cycleID = zp.createTestCycle(ztc);
            Console.WriteLine("Cycle ID : {0}", cycleID);

            //string output = zp.aAddTests(jp.id, cycleID, "10656");
            List<zTest> issues = zp.getRegressionTests(currentVersion.name);
            string output = zp.AddTestsbyList(jp.id, cycleID, issues);
            //Console.WriteLine(output);

        }
        [Test]
        public void createExecutions()
        {

            //get project and version details
            jProject jp = zp.getProject("IDRIVE");
            jVersion currentVersion = jp.versions.Find(t => t.name.Equals("N1.55 Oct-2016"));

            //Sort out the start and end dates
            string startDay = String.Format("{0}/{1}/{2}", DateTime.Now.Day, DateTime.Now.ToString("MMM"), DateTime.Now.ToString("yy"));
            string endDay = String.Format("{0}/{1}/{2}", DateTime.Now.AddDays(1).Day, DateTime.Now.AddDays(1).ToString("MMM"), DateTime.Now.AddDays(1).ToString("yy"));
            Console.WriteLine(startDay + " : " + endDay);

            //Create a Test Cycle
            zTestCycle ztc = new zTestCycle(Convert.ToInt16(jp.id), DateTime.Now.ToShortDateString() + "MT Regression Cycle", "Created from API", startDay, endDay, Convert.ToInt16(currentVersion.id));
            string cycleID = zp.createTestCycle(ztc);
            Console.WriteLine("Cycle ID : {0}", cycleID);

            //Add the tests from an existing filter
            string output = zp.AddTests(jp.id, cycleID, "10656");

            //Get the issues from the filter
            zTestList tests = zp.getIssuesFromFilter("10656");
            foreach (zTest tmp in tests.issues)
            {
                Dictionary<string, zExecute> ze = zp.createExecution(new zCreateExecution(cycleID, tmp.id, jp.id, currentVersion.id));
                foreach (string tmpE in ze.Keys)
                {
                    Console.WriteLine("Execution ID is : {0}", ze[tmpE].id);
                }
            }


        }
        [Test]
        public void getFilterDetails()
        {
            jFilter jF = zp.getJiraFilterDetails("10766");
            Console.WriteLine("{0} : {1} : {2}", jF.jql, jF.searchUrl, jF.owner.displayName);
        }
        [Test]
        public void UpdateTeststatus()
        {

            jFilter filterDetails = zp.getJiraFilterDetails("10656");
            zTestList ztl = zp.getIssuesfromURL(filterDetails.searchUrl);
            zExecutions zexList = zp.getExecutionId("55");

            //foreach (zTest tmp in ztl.issues)
            //{
            //    string testid = tmp.id;
            //    string testname = tmp.fields.summary;


            //}



            //foreach (zExecute tmp1 in zexList.executions)
            //{
            //    Console.WriteLine(tmp1.id + "sss");

            //}
            //string kk= zp.getExecutionId(testid).executions[0].id;

            //zp.UpdateStatus(kk);

            //Console.WriteLine("{0} : {1} : {2} : {3} : {4}", tmp.key, tmp.id, tmp.fields.customfield_10119.value, tmp.fields.customfield_10120,"");

            //}


            Dictionary<string, string> create = new Dictionary<string, string>();

            //string output = zp.AddTests(ProjectId, testsetID, "10656");
            //Console.WriteLine(output);

            //Post details to mango

            foreach (zTest tmp in ztl.issues)
            {

                //create.Clear();
                create.Add("name", tmp.fields.ScriptID);
                create.Add("summary", tmp.fields.summary);
                create.Add("testid", tmp.id);
                create.Add("description", "sss");
                create.Add("status", tmp.fields.teststatus.value);
                create.Add("testsetid", "55");
                create.Add("duration", "0");
                create.Add("host", "Not Applicable");
                create.Add("success", "No Run");
                create.Add("author", tmp.fields.creator != null ? tmp.fields.creator.displayName : "ivcauto");
                create.Add("created", tmp.fields.versions[0].name);
                create.Add("runner", "Default");
                create.Add("F2US", tmp.fields.issuelinks[0].outwardIssue.key);
                create.Add("IVUS", tmp.key);
                create.Add("module", tmp.fields.components[0].name);
                create.Add("submodule", tmp.fields.labels[0]);
                create.Add("suitename", tmp.fields.SuiteID);
                create.Add("executionid", zexList.getExecutionId(Convert.ToInt16(tmp.id)));

                //create result set
                string mongoposttext = JsonConvert.SerializeObject(create);
                Console.WriteLine("Posted on MongoDB result: " + mongoposttext);
                //Console.WriteLine("Description: {0} : Id: {1} : Summary: {2}, Status: {3}, Modules: {4}, submod: {5}, scriptid: {6}, displayname: {7}", tmp.fields.description, tmp.id, tmp.fields.summary, tmp.fields.customfield_10119.value, tmp.fields.components[0].name, tmp.fields.labels[0], tmp.fields.customfield_10120);
            }


            //zExecutions zexList = zp.getExecutionId("55");

            //foreach (zExecute tmp1 in zexList.executions)
            //{
            //    Console.WriteLine(tmp1.id + "sss");
            //    create.Add("execution id", tmp1.id.ToString());
            //}

            //int kk = 111;
        }
        [Test]
        public void getIssues()
        {
            jFilter filterDetails = zp.getJiraFilterDetails("10656");
            string MaxResults = filterDetails.searchUrl.Replace("jql=project", "&maxResults=2000&jql=project");
            zTestList ztl = zp.getIssuesfromURL(MaxResults);

            //sharath look at this
            //List<zTest> hello =ztl.issues.FindAll(t => t.hasComponent("Accounts")).ToList();


            //ztl.issues.FindAll(t => t.hasComponent("Accounts")).ToList();

            foreach (zTest tmp in ztl.issues)
            {
                Console.WriteLine("{0} : {1} : {2} : {3}", tmp.key, tmp.id, tmp.fields.teststatus.value, tmp.fields.ScriptID);
            }
        }
        [Test]
        public void getTestSteps()
        {

            zp.deleteTestSteps("11098");

        }
        [Test]
        public void createDashboard()
        {

            #region Format Needed


            //create.Clear();
            //create.Add("name", testname);
            //create.Add("testid", testid);
            //create.Add("description", Regex.Replace(testdescription, @"[^a-zA-Z0-9 ]", "", RegexOptions.Compiled));
            //create.Add("status", teststatus);
            //create.Add("testsetid", testsetID);
            //create.Add("duration", duration);
            //create.Add("host", host);
            //create.Add("success", success);
            //create.Add("author", author);
            //create.Add("created", relevant);
            //create.Add("runner", "Default");
            //create.Add("F2US", F2US);
            //create.Add("IVUS", IVUS);
            //create.Add("module", Module);
            //create.Add("submodule", subMod);
            //create.Add("suitename", testsuitename);
            //create.Add("packname", packNode.Attributes["value"].Value);
            //create.Add("testsetname", targetXMLname.Split('.')[0]);



            #endregion


        }
        [Test]
        public void getSingleIssue()
        {
            zTest jt = zp.getSingleIssue("IDRIVE-532");
        }
        [Test]
        public void getQCSubModules()
        {
            string url = "http://dsalm.ds.ad.adp.com:8080/qcbin/rest/domains/DSI/projects/DSIAUTOLINE/tests?page-size=max&fields=user-03&query={status[automated]}";
            XmlDocument xd = new XmlDocument();
            xd.Load(alm.get(url));
            List<string> sub = new List<string>();

            foreach (XmlNode tmp in xd.SelectNodes("//Entities/Entity"))
            {
                string subMod = tmp.SelectSingleNode("Fields/Field[@Name='user-03']").SelectSingleNode("Value").InnerText;
                if (!sub.Contains(subMod))
                {
                    sub.Add(subMod);
                }
            }

            foreach (string tmp in sub)
            {
                Console.WriteLine("modulemapping.Add(\"{0}\", \"\");", tmp);
            }

        }
        [Test]
        public void getMethodNames()
        {
            Assembly assembly = Assembly.LoadFrom(@"C:\Sastry Poranki\code\IVC-Master\IVCDriveTests\bin\Debug\IVCDriveTests.dll");
            Type[] types = assembly.GetTypes();

            var thl = assembly.GetReferencedAssemblies();

            var pl = assembly.GetTypes();
        }
        [Test]
        public void getRegressionTests()
        {
            //jProject jp = zp.getProject("IDRIVE");
            //jVersion jv = jp.versions.Find(t => t.name.Contains("1.55"));
            //Console.WriteLine(zp.getRegressionTests(jv.name).Count);
        }
        [Test]
        public void MoveMBZephyr()
        {
            List<string> submodules = new List<string>();
            submodules.Add("Service POS");
            foreach (string submodule in submodules)
            {

                List<string> designsteps = new List<string>();
                //String submodule = System.Uri.EscapeUriString("Parts POS");
                string url = "http://dsalm.ds.ad.adp.com:8080/qcbin/rest/domains/DSI/projects/DSIAUTOLINE/tests?page-size=max&fields=id&query={user-07[DriveOEM];user-11[\"MB\"]}";

                jProject project = zp.getProject("IDRIVE");
                //jVersion currentVersion = project.versions.Find(t => t.name.Equals("N1.50 May-2016"));

                //1438
                Stream alltests = alm.get(url);
                XmlDocument alltestXML = new XmlDocument();
                alltestXML.Load(alltests);

                //zTest jr = alm.Alm2Zephyr("4877", ref designsteps, project);
                //jr.fields.versions.Add(currentVersion);
                //zTest test = zp.createTest(jr);
                //zp.AddTestSteps(test.id, designsteps);

                foreach (XmlNode tmp in alltestXML.SelectNodes("//Entities/Entity"))
                {
                    //zp = new zapi("siddarer", "SI45$%dI");//update this with username and password
                    //zp.reAuthenticate();
                    designsteps = new List<string>();
                    string testid = tmp.SelectSingleNode("Fields/Field[@Name='id']").SelectSingleNode("Value").InnerText;
                    Console.WriteLine(testid);
                    zTest jr = alm.Alm2Zephyr(testid, ref designsteps, project);
                    jr.fields.Manufacturer.Add(new jManufacturer("Mercedes"));
                    Console.WriteLine("{0} : {1}", jr.fields.description, jr.fields.Manufacturer[0].value);
                    ////jr.fields.versions.Add(currentVersion);
                    zTest test = zp.createTest(jr);
                    zp.AddTestSteps(test.id, designsteps);
                    designsteps.Clear();
                }
            }
        }

        [Test]
        public void findTSRSteps()
        {

            List<string> output = new List<string>();
            zTestList ztl = zp.getIssuesFromFilter("10679");
            foreach (zTest tmp in ztl.issues)
            {
                output.Add(zp.EstimateAutomationTasks(tmp.key));
            }

            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Console.WriteLine("");
            foreach (string tmpTest in output)
            {
                Console.WriteLine(tmpTest);
            }
            Console.WriteLine("");
            Console.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
        }

        [Test]
        public void createEATTests()
        {
            string filename = "eatlive.csv";



            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                string[] params1 = line.Split(',');

                zTest test = new zTest();
                test.fields.summary = string.Format("{0}", params1[3]);
                test.fields.labels.Add(params1[2]);
                test.fields.components.Add(new jComponent(params1[1]));
                test.fields.description = params1[4];
                zp.createTest(test);
            }



        }

        [Test]
        public void changeTitle()
        {


            foreach (string key in File.ReadAllLines("change.txt"))
            {

                //   string key = "IDRIVE-18333";
                zTest currentTest = zp.checkIfTestExists(key);
                TestStatus updatestatus = new TestStatus();
                updatestatus.value = "Abandoned";

                if (currentTest != null)
                {
                    Dictionary<string, TestStatus> update = new Dictionary<string, TestStatus>();
                    Dictionary<string, Dictionary<string, TestStatus>> finalUpdate = new Dictionary<string, Dictionary<string, TestStatus>>();

                    update.Add("customfield_10600", updatestatus);
                    finalUpdate.Add("fields", update);
                    zp.updateTest(currentTest, JsonConvert.SerializeObject(finalUpdate));
                }
            }

        }

        [Test]
        public void removeCloneinSummary()
        {
            zTest parentIssue = zp.getSingleIssue("IDRIVE-19833");
            foreach (zTest subTask in parentIssue.fields.subtasks)
            {
                zp.RemoveCloneFromSummary(subTask);
                Console.WriteLine("Updated for Subtask : {0}", subTask.key);
            }

        }

        [Test]
        public void Misc()
        {

            string text = File.ReadAllText("steps_17487.txt");
            List<zTestStep> all = JsonConvert.DeserializeObject<List<zTestStep>>(text);
            zp.AddTestStepsbyScriptID("VMDD15420002", text);

        }

        [Test]
        public void RemoveEpicLink_AddStoryLink()
        {

            string filename = "smoke.txt";

            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);

            while (!sr.EndOfStream)
            {

                string issuec = sr.ReadLine();
                zTest currentTest = zp.getSingleIssue(issuec);
                Dictionary<string, string> update = new Dictionary<string, string>();
                Dictionary<string, Dictionary<string, string>> finalUpdate = new Dictionary<string, Dictionary<string, string>>();
                update.Add("customfield_10005", null);
                finalUpdate.Add("fields", update);
                zp.updateTest(currentTest, JsonConvert.SerializeObject(finalUpdate));




                createIssueLink tmp = new createIssueLink();
                tmp.outwardIssue = new LinkIssue();
                tmp.inwardIssue = new LinkIssue();
                tmp.inwardIssue.key = issuec;
                tmp.outwardIssue.key = "IDRIVE-20399";
                tmp.type = new issuelinktype();
                tmp.type.id = "10003";

                zp.AddIssueLink(tmp);
            }
        }

        [Test]
        public void getReleaseTestingEffort()
        {
            zTestList releaseTestingEpic = zp.getIssuesByEpicLink("IDRIVE-8997");

            foreach (zTest tmpIssue in releaseTestingEpic.issues)
            {
                int relatedIssues = tmpIssue.fields.issuelinks.Count;
                foreach (Issuelink tmpLink in tmpIssue.fields.issuelinks)
                {
                    string currentKey = tmpLink.inwardIssue.key;
                    zTest relatedIssue = zp.getSingleIssue(currentKey);
                    Console.WriteLine("{0}^{1}^{2}^{3}^{4}^{5}^{6}^{7}", "IDRIVE-8997", "Release Testing Epic", tmpIssue.key, tmpIssue.fields.summary, relatedIssue.key, relatedIssue.fields.issuetype.name, relatedIssue.fields.summary, relatedIssue.fields.aggregatetimespent);
                }
            }

        }
    }
}
