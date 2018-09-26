using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Utilities;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.IO.Compression;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace zephyrapi
{
    #region Classes representing Jira/Zephyr JSON

    public class LinkIssue
    {
        public string key { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class zTest
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfDefault]
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("key")] public string key { get; set; }

        [BsonElement("id")] public string id { get; set; }

        [BsonElement("self")] public string self { get; set; }

        [BsonElement("fields")] public Fields fields { get; set; }

        public zTest()
        {
            fields = new Fields();
            fields.issuetype.name = "Test";
            fields.project.key = "IDRIVE";
        }

        public bool hasComponent(string component)
        {
            return fields.components.Exists(t => t.name.Equals(component));
        }

        public bool hasLabel(string label)
        {
            return fields.labels.Exists(t => t.Equals(label));
        }

        public void printSummary()
        {
            Console.WriteLine("{0} : {1} : {2} : {3} : {4}", fields.project.key, fields.ScriptID, fields.summary,
                fields.issuetype.name, fields.labels[0]);
        }
    }

    [BsonIgnoreExtraElements]
    public class jAttachment
    {
        [BsonElement("self")] public string self { get; set; }

        [BsonElement("id")] public string id { get; set; }

        [BsonElement("filename")] public string filename { get; set; }

        [BsonElement("created")] public string created { get; set; }

        [BsonElement("size")] public int size { get; set; }

        [BsonElement("mimeType")] public string mimeType { get; set; }

        [BsonElement("content")] public string content { get; set; }
    }

    public class zTestList
    {
        public string startAt { get; set; }
        public string maxResults { get; set; }
        public string total { get; set; }
        public List<zTest> issues { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Issuelink
    {
        [BsonElement("id")] public string id { get; set; }

        [BsonElement("self")] public string self { get; set; }

        [BsonElement("outwardIssue")] public zTest outwardIssue { get; set; }

        [BsonElement("inwardIssue")] public zTest inwardIssue { get; set; }

        [BsonElement("type")] public issuelinktype type { get; set; }
    }

    public class createIssueLink
    {
        public LinkIssue outwardIssue { get; set; }
        public LinkIssue inwardIssue { get; set; }
        public issuelinktype type { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class jManufacturer
    {
        [BsonElement("self")] public string self { get; set; }

        [BsonElement("value")] public string value { get; set; }

        [BsonElement("id")] public string id { get; set; }

        public jManufacturer(string manf)
        {
            value = manf;
        }
    }

    [BsonIgnoreExtraElements]
    public class issuelinktype
    {
        [BsonElement("id")] public string id { get; set; }

        [BsonElement("name")] public string name { get; set; }

        [BsonElement("inward")] public string inward { get; set; }

        [BsonElement("outward")] public string outward { get; set; }

        [BsonElement("self")] public string self { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Fields
    {
        [BsonElement("project")] public jProject project { get; set; }

        [BsonElement("teststatus")]
        [JsonProperty(PropertyName = "customfield_10600")]
        public TestStatus teststatus { get; set; } //production instance

        [BsonElement("ScriptID")]
        [JsonProperty(PropertyName = "customfield_10602")]
        public string ScriptID { get; set; } //production instance

        [BsonElement("epiclink")]
        [JsonProperty(PropertyName = "customfield_10005")]
        public string epiclink { get; set; }

        [BsonElement("SuiteID")]
        [JsonProperty(PropertyName = "customfield_10700")]
        public string SuiteID { get; set; }

        [BsonElement("Manufacturer")] 
        [JsonProperty(PropertyName = "customfield_10310")]
        public List<jManufacturer> Manufacturer;

        [BsonIgnore] [JsonIgnore] public List<jAttachment> attachment;

        [BsonElement("summary")] public string summary { get; set; }

        [BsonElement("description")] public string description { get; set; }

        [BsonElement("issuetype")] public jIssueType issuetype { get; set; }

        [BsonElement("components")] public List<jComponent> components;

        [BsonElement("labels")] public List<string> labels;

        [BsonElement("creator")] public Owner creator { get; set; }

        public bool ShouldSerializecreator()
        {
            return false;
        }

        [BsonElement("subtasks")] public List<zTest> subtasks { get; set; }

        public Fields()
        {
            project = new jProject();
            issuetype = new jIssueType();
            components = new List<jComponent>();
            labels = new List<string>();
            versions = new List<jVersion>();
            //teststatus = new TestStatus();
            Manufacturer = new List<jManufacturer>();
            attachment = new List<jAttachment>();
        }

        [BsonElement("issuelinks")] public List<Issuelink> issuelinks { get; set; }

        public bool ShouldSerializeissuelinks()
        {
            return false;
        }

        [BsonElement("versions")] public List<jVersion> versions { get; set; }

        [BsonElement("aggregatetimespent")] public string aggregatetimespent { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class TestStatus
    {
        [BsonElement("self")] public string self { get; set; }

        [BsonElement("value")] public string value { get; set; }

        [BsonElement("id")] public string id { get; set; }
    }

    public class zTestStep
    {
        public string step { get; set; }
        public string data { get; set; }
        public string result { get; set; }
        public int id { get; set; }
        public zTestStep(string ts, string td, string tr)
        {
            step = ts;
            data = td;
            result = tr;
        }
    }
    public class zTestCycle
    {
        public string endDate { get; set; }
        public string description { get; set; }
        public int versionId { get; set; }
        public string name { get; set; }
        public int projectId { get; set; }
        public string startDate { get; set; }

        public zTestCycle(int projectID, string cyclename, string cycledesc, string start = "", string end = "", int versionID = -1)
        {
            projectId = projectID;
            name = cyclename;
            description = cycledesc;
            startDate = start;
            endDate = end;
            versionId = versionID;

        }
    }

    [BsonIgnoreExtraElements]
    public class jComponent
    {
        [BsonElement("self")] public string self { get; set; }

        [BsonElement("id")] public string id { get; set; }

        [BsonElement("name")] public string name { get; set; }

        [BsonElement("isAssigneeTypeValid")] public bool isAssigneeTypeValid { get; set; }

        public jComponent(string compname)
        {
            name = compname;
        }
    }

    [BsonIgnoreExtraElements]
    public class jIssueType
    {
        [BsonElement("self")] public string self { get; set; }

        [BsonElement("id")] public string id { get; set; }

        [BsonElement("description")] public string description { get; set; }

        [BsonElement("iconUrl")] public string iconUrl { get; set; }

        [BsonElement("name")] public string name { get; set; }

        [BsonElement("subtask")] public bool subtask { get; set; }

        [BsonElement("avatarId")] public int? avatarId { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class jVersion
    {
        [BsonElement("self")] public string self { get; set; }

        [BsonElement("id")] public string id { get; set; }

        [BsonElement("description")] public string description { get; set; }

        [BsonElement("name")] public string name { get; set; }

        [BsonElement("archived")] public bool archived { get; set; }

        [BsonElement("released")] public bool released { get; set; }

        [BsonElement("projectId")] public int projectId { get; set; }

        public jVersion(string versionname)
        {
            name = versionname;
        }
    }

    [BsonIgnoreExtraElements]
    public class jProject
    {
        [BsonElement("expand")] public string expand { get; set; }

        [BsonElement("self")] public string self { get; set; }

        [BsonElement("id")] public string id { get; set; }

        [BsonElement("key")] public string key { get; set; }

        [BsonElement("description")] public string description { get; set; }

        [BsonElement("components")] public List<jComponent> components { get; set; }

        [BsonElement("issueTypes")] public List<jIssueType> issueTypes { get; set; }

        [BsonElement("assigneeType")] public string assigneeType { get; set; }

        [BsonElement("versions")] public List<jVersion> versions { get; set; }

        [BsonElement("name")] public string name { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Owner
    {
        [BsonElement("self")] public string self { get; set; }

        [BsonElement("key")] public string key { get; set; }

        [BsonElement("name")] public string name { get; set; }

        [BsonElement("displayName")] public string displayName { get; set; }

        [BsonElement("active")] public bool active { get; set; }
    }

    public class jFilter
    {
        public string self { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public Owner owner { get; set; }
        public string jql { get; set; }
        public string viewUrl { get; set; }
        public string searchUrl { get; set; }
        public bool favourite { get; set; }
    }
    public class zCreateExecution
    {
        public string cycleId { get; set; }
        public string issueId { get; set; }
        public string projectId { get; set; }
        public string versionId { get; set; }

        public zCreateExecution(string cycle, string issue, string project, string version)
        {
            cycleId = cycle;
            issueId = issue;
            projectId = project;
            versionId = version;
        }
    }
    public class zCreateBulkExecution
    {
        public List<zCreateExecution> issues { get; set; }
    }
    public class zExecute
    {
        public int id { get; set; }
        public int orderId { get; set; }
        public string executionStatus { get; set; }
        public string executedOn { get; set; }
        public string executedBy { get; set; }
        public string executedByDisplay { get; set; }
        public string comment { get; set; }
        public string htmlComment { get; set; }
        public int cycleId { get; set; }
        public string cycleName { get; set; }
        public int versionId { get; set; }
        public string versionName { get; set; }
        public int projectId { get; set; }
        public string createdBy { get; set; }
        public string modifiedBy { get; set; }
        public int issueId { get; set; }
        public string issueKey { get; set; }
        public string summary { get; set; }
        public string issueDescription { get; set; }
        public string label { get; set; }
        public string component { get; set; }
        public string projectKey { get; set; }
    }
    public class zExecutions
    {
        public List<zExecute> executions { get; set; }
        public string getExecutionId(int testid)
        {
            zExecute ze = executions.Find(t => t.issueId.Equals(testid));
            if (ze != null)
                return Convert.ToString(ze.id);
            else
                return "1";
        }
    }


    public class subgroup
    {
        public string name { get; set; }
        public string eta { get; set; }
        public List<sConfig> tests;
        public subgroup()
        {
            tests = new List<sConfig>();
        }
    }

    public class group
    {
        public string name { get; set; }
        public List<subgroup> subgroups;
        public group()
        {
            subgroups = new List<subgroup>();
        }
    }

    


    public class sConfig
    {
        public int order;
        public string module;
        public string submodule;
        public string runconfig;
        public Boolean sequential;
        public Boolean rlcon;
        public string chunkSize;
        public string subscribers;

    }
    #endregion
    public class zapi
    {
        string restURL;
        string user;
        string password;
        public zapi(string jirauser, string jirapass, string jirabase = null)
        {
            user = jirauser;
            password = jirapass;

            if (string.IsNullOrEmpty(jirabase))
                restURL = "https://projects.cdk.com";
            else
                restURL = jirabase;

        }

        #region REST Operations
        public static string getEntity(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            string str = string.Empty;
            try
            {
                request.Method = "GET";
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("svc_autoline_ivc" + ":" + "6xL@tCdw]/"));
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    str = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception occured", exception.StackTrace);
            }
            request.GetResponse().Dispose();
            return str;
        }
        public string updateEntity(string url, string jsonstring)
        {
            string str = string.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                byte[] bytes = Encoding.UTF8.GetBytes(jsonstring);
                request.Method = "PUT";
                request.ProtocolVersion = HttpVersion.Version11;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.KeepAlive = true;
                request.AllowAutoRedirect = true;
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(user + ":" + password));
                request.ContentLength = bytes.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Close();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
                str = reader.ReadToEnd();
                reader.Close();
                response.Close();
                return str;
            }
            catch (Exception exception)
            {
                Console.WriteLine("Cannot add : {0} : {1}", jsonstring, exception.StackTrace);
                return str;
            }
        }
        public string deleteEntity(string url)
        {
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            string str = string.Empty;
            try
            {
                request.Method = "DELETE";
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(user + ":" + password));
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                str = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();

            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception occured", exception);
            }
            request.GetResponse().Dispose();
            return str;
        }
        public string postEntity(string url, string jsonstring)
        {
            Console.WriteLine("++++++++++++++++++++++++++++++++++");
            Console.WriteLine("URL : " + url);
            Console.WriteLine(jsonstring);
            Console.WriteLine("++++++++++++++++++++++++++++++++++");

            string str = string.Empty;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                byte[] bytes = Encoding.UTF8.GetBytes(jsonstring);
                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version11;
                request.ContentType = "application/json";
                request.Accept = "application/json";
                request.KeepAlive = true;
                request.AllowAutoRedirect = true;
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(user + ":" + password));
                request.ContentLength = bytes.Length;
                Stream requestStream = request.GetRequestStream();
                requestStream.Write(bytes, 0, bytes.Length);
                requestStream.Close();
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default);
                str = reader.ReadToEnd();
                reader.Close();
                response.Close();
                return str;
            }
            catch (WebException exception)
            {

                //Console.WriteLine("Cannot add : {0} : {1}", jsonstring, exception.StackTrace);
                HttpWebResponse objResponse = exception.Response as HttpWebResponse;
                return str;
            }
        }
        #endregion
        #region Zephyr re-usable methods

        public jProject getProject(string projKey)
        {
            string projecturl = restURL + "/rest/api/2/project/" + projKey;
            string jsonop = getEntity(projecturl);
            jProject projDetails = JsonConvert.DeserializeObject<jProject>(jsonop);
            return projDetails;
        }
        public zTest createTest(zTest jiraticket)
        {
            if (!string.IsNullOrEmpty(jiraticket.fields.ScriptID))
            {
                zTest existingTest = checkIfTestExists(jiraticket.fields.ScriptID);
                if (existingTest != null)
                {
                    return existingTest;
                }
            }

            string issueURL = restURL + "/rest/api/2/issue";
            string testJsonString = JsonConvert.SerializeObject(jiraticket);
            Console.WriteLine(testJsonString);
            string jsonOutput = postEntity(issueURL, testJsonString);
            Console.WriteLine(jsonOutput);
            dynamic jsonText = JValue.Parse(jsonOutput);
            Console.WriteLine("======================================");
            Console.WriteLine(jsonText);
            string selfurl = (string)jsonText.self;
            if (!string.IsNullOrEmpty(selfurl))
            {
                zTest returnTest = JsonConvert.DeserializeObject<zTest>(getEntity(selfurl));
                Console.WriteLine("{0} : {1}", returnTest.id, returnTest.key);
                return returnTest;
            }
            else
                return null;


        }
        public void updateTest(zTest jiraticket, string jsonupdate)
        {
            string issueURL = restURL + "/rest/api/2/issue/" + jiraticket.key;
            string testJsonString = JsonConvert.SerializeObject(jiraticket);
            Console.WriteLine(testJsonString);
            string jsonOutput = updateEntity(issueURL, jsonupdate);
            Console.WriteLine(jsonOutput);
        }

        public zTest searchTestByscriptID(string scriptID)
        {
            //"Epic Link" = "IDRIVE-8997"
            string jql = string.Format("project = {0} and issuetype = {1} and \"Script ID\" ~ {2}", "IDRIVE", "Test", scriptID);
            string url = url = string.Format("https://projects.cdk.com/rest/api/2/search?jql={0}", jql);
            string jsonoutput = getEntity(url);
            if (!string.IsNullOrEmpty(jsonoutput))
            {
                zTestList testlist = JsonConvert.DeserializeObject<zTestList>(jsonoutput);
                if (testlist.issues.Count > 0)
                    return JsonConvert.DeserializeObject<zTestList>(jsonoutput).issues[0];
                else
                    return null;
            }
            else
            {
                return null;
            }

        }

        public zTestList getIssuesByEpicLink(string epicID)
        {

            string jql = string.Format("project = {0} and \"Epic Link\" = \"{1}\"", "IDRIVE", epicID);
            string url = url = string.Format("https://projects.cdk.com/rest/api/2/search?jql={0}", jql);
            string jsonoutput = getEntity(url);
            if (!string.IsNullOrEmpty(jsonoutput))
            {
                return JsonConvert.DeserializeObject<zTestList>(jsonoutput);
            }
            else
            {
                return null;
            }

        }
        public void createTest(string projectid, string component)
        {
            string issueURL = restURL + "/rest/api/2/issue";

            Dictionary<string, string> testData = new Dictionary<string, string>();


            Dictionary<string, string> temp = new Dictionary<string, string>();
            temp.Add("name", "IDRIVE");

            testData.Add("project", JsonConvert.SerializeObject(temp));
            testData.Add("summary", "Test created from ZAPI API");

            temp = new Dictionary<string, string>();
            temp.Add("name", "Test");
            testData.Add("issuetype", JsonConvert.SerializeObject(temp));

            temp = new Dictionary<string, string>();
            temp.Add("name", "Accounts");
            List<Dictionary<string, string>> tempList = new List<Dictionary<string, string>>();
            tempList.Add(temp);
            testData.Add("components", JsonConvert.SerializeObject(tempList));

            Dictionary<string, string> finalData = new Dictionary<string, string>();
            finalData.Add("fields", JsonConvert.SerializeObject(testData));
            string testJsonString = JsonConvert.SerializeObject(finalData);

        }
        public static string getlocal(string url)
        {
            string backstr = "";
            try
            {
                //Console.WriteLine(url);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.ProtocolVersion = HttpVersion.Version11;
                req.ContentType = "application/json";
                req.KeepAlive = true;
                req.AllowAutoRedirect = true;
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
                backstr = sr.ReadToEnd();
                sr.Close();
                res.Close();
                return backstr;
            }
            catch (Exception postExcep)
            {
                Console.WriteLine(postExcep.StackTrace);
                return backstr;
            }
        }
        public static string postlocal(string url, string data)
        {
            string backstr = "";
            try
            {
                //Console.WriteLine(url);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(data);
                req.Method = "POST";
                req.ProtocolVersion = HttpVersion.Version11;
                req.ContentType = "application/json";
                req.KeepAlive = true;
                req.AllowAutoRedirect = true;
                req.ContentLength = requestBytes.Length;
                Stream requestStream = req.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
                backstr = sr.ReadToEnd();
                sr.Close();
                res.Close();
                return backstr;
            }
            catch (Exception postExcep)
            {
                Console.WriteLine("Cannot add : {0} : {1}", data, postExcep.StackTrace);
                return backstr;
            }
        }
        public void AddTestSteps(string testid, List<zTestStep> steps)
        {

            //Clean up the test steps if exist
            deleteTestSteps(testid);

            string url = restURL + "/rest/zapi/latest/teststep/" + testid;
            foreach (zTestStep zTmp in steps)
            {
                Console.WriteLine("Adding {0}", zTmp.step);
                Dictionary<string, string> dSteps = new Dictionary<string, string>();
                dSteps.Add("step", zTmp.step);
                dSteps.Add("data", zTmp.data);
                dSteps.Add("result", zTmp.result);
                string result = postEntity(url, JsonConvert.SerializeObject(dSteps));
            }
        }
        public void AddTestSteps(string testid, string stepsjson)
        {
            List<zTestStep> steps = JsonConvert.DeserializeObject<List<zTestStep>>(stepsjson);
            AddTestSteps(testid, steps);
        }
        public void AddTestSteps(string testid, List<string> steps)
        {
            //clear the test steps
            deleteTestSteps(testid);

            string url = restURL + "/rest/zapi/latest/teststep/" + testid;
            foreach (string zTmp in steps)
            {
                string[] arr = zTmp.Split('^');
                Dictionary<string, string> dSteps = new Dictionary<string, string>();
                dSteps.Add("step", arr[0]);
                dSteps.Add("data", "");

                if (arr.Length > 1)
                    dSteps.Add("result", arr[1]);
                else
                    dSteps.Add("result", "");

                string result = postEntity(url, JsonConvert.SerializeObject(dSteps));
                Console.WriteLine(result);
            }
        }
        public string createTestCycle(zTestCycle testCycle)
        {
            jProject jp = getProject("IDRIVE");
            string issueURL = restURL + "/rest/zapi/latest/cycle";
            string testJsonString = JsonConvert.SerializeObject(testCycle);
            Console.WriteLine(testJsonString);
            string jsonOutput = postEntity(issueURL, testJsonString);
            dynamic jsonText = JValue.Parse(jsonOutput);
            string testid = (string)jsonText.id;
            return testid;
        }
        public string AddTests(string projectid, string cycleId, string searchId)
        {
            string url = restURL + "/rest/zapi/latest/execution/addTestsToCycle";
            Dictionary<string, string> addTests = new Dictionary<string, string>();
            addTests.Add("cycleId", cycleId);
            addTests.Add("method", "2");
            addTests.Add("projectId", projectid);
            addTests.Add("searchId", searchId);
            string jsonstring = JsonConvert.SerializeObject(addTests);
            string result = postEntity(url, jsonstring);
            return result;
        }
        public string AddTestsbyList(string projectid, string cycleId, List<zTest> zTestList)
        {
            string url = restURL + "/rest/zapi/latest/execution/addTestsToCycle";
            Dictionary<string, object> addTests = new Dictionary<string, object>();
            addTests.Add("cycleId", cycleId);
            addTests.Add("method", "1");
            addTests.Add("projectId", projectid);

            List<string> issues = new List<string>();
            foreach (zTest tmp in zTestList)
            {
                issues.Add(tmp.key);
            }

            addTests.Add("issues", JToken.Parse(JsonConvert.SerializeObject(issues.ToArray()).ToString()));
            string jsonstring = JsonConvert.SerializeObject(addTests);

            Console.WriteLine(jsonstring);
            string result = postEntity(url, jsonstring);
            return result;
        }
        public zTestList getIssuesFromFilter(string filterId)
        {
            jFilter F = getJiraFilterDetails(filterId);
            return getIssuesfromURL(F.searchUrl);
        }
        public jFilter getJiraFilterDetails(string filterId)
        {
            string url = restURL + "/rest/api/2/filter/" + filterId;
            string jsontext = getEntity(url);
            jFilter jiraFilter = JsonConvert.DeserializeObject<jFilter>(jsontext);
            return jiraFilter;


        }
        public zExecutions getExecutionId(string testID)
        {
           
            string url = restURL + "/rest/zapi/latest/execution/" + "?cycleId=" + testID;
            string jsontext = getEntity(url);
            var obj = JObject.Parse(jsontext);
            //var url2 = (string)obj.SelectToken("executions.id");
            //Console.WriteLine(url2.ToString());
            //JsonConvert.DeserializeObject<zTestList>(jsontext);
            var results = JsonConvert.DeserializeObject<dynamic>(jsontext);
            zExecutions aa = JsonConvert.DeserializeObject<zExecutions>(jsontext);

            //return jiraFilter;
            return aa;
        }
        public zTestList getIssuesfromURL(string url)
        {
            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            return ztl;
        }
        public Dictionary<string, zExecute> createExecution(zCreateExecution zCE)
        {
            string issueURL = restURL + "/rest/zapi/latest/execution";
            string testJsonString = JsonConvert.SerializeObject(zCE);
            Console.WriteLine(testJsonString);
            string jsonOutput = postEntity(issueURL, testJsonString);
            Console.WriteLine(jsonOutput);
            return JsonConvert.DeserializeObject<Dictionary<string, zExecute>>(jsonOutput);
        }
        public List<zTestStep> getTeststeps(string testid)
        {
            string url = restURL + "/rest/zapi/latest/teststep/" + testid;
            string jsonoutput = getEntity(url);
            List<zTestStep> ztsl = JsonConvert.DeserializeObject<zTestStep[]>(jsonoutput).ToList();
            return ztsl;

        }
        public void deleteTestSteps(string testid)
        {
            foreach (zTestStep tmp in getTeststeps(testid))
            {
                deleteEntity(restURL + "/rest/zapi/latest/teststep/" + testid + "/" + tmp.id);
            }
        }
        public zTest getSingleIssue(string issueid)
        {
            string url = restURL + "/rest/api/2/issue/" + issueid;
            string json = getEntity(url);
            return JsonConvert.DeserializeObject<zTest>(json);
        }
        public zTest checkIfTestExists(string scriptID)
        {
            string url = restURL + "/rest/api/2/search?jql=project = IDRIVE AND issuetype = Test AND \"Script ID\" ~ " + scriptID;
            string json = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(json);

            if (ztl != null)
            {
                if (ztl.total.Equals("0"))
                    return null;
                else
                    return ztl.issues[0];
            }
            else
            {
                return null;
            }
        }
        public void UpdateStatus(string execid)
        {
            string url = restURL + "/rest/zapi/latest/execution/" + execid + "/execute";
            Dictionary<string, int> testCycle = new Dictionary<string, int>();
            testCycle.Add("status", 2);
            //cred.Add("password", pass);
            string testJsonString = JsonConvert.SerializeObject(testCycle);
            updateEntity(url, testJsonString);
            //Console.WriteLine(testJsonString);
        }
        public List<zTest> getRegressionTests(string codeversion, string pack = null)
        {
            string url = "";
            List<zTest> searchResults = new List<zTest>();
            string JobMapsComponent = "'IVC Job Maps'";
            if (pack.ToLower().Equals("jobmaps"))
                url = restURL + string.Format("/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND component = {3}", "IDRIVE", "Test Status", codeversion, JobMapsComponent);
            else if (pack.ToLower().Equals("mt"))
                url = restURL + string.Format("/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND (\"{1}\" = Automated OR \"{1}\" = Merged) AND affectedVersion <= \"{2}\" AND component != {3}", "IDRIVE", "Test Status", codeversion, JobMapsComponent);
            else
                url = restURL + string.Format("/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND component != {3}", "IDRIVE", "Test Status", codeversion, JobMapsComponent);
            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            searchResults.AddRange(ztl.issues);

            int totalResults = Convert.ToInt32(ztl.total);
            int pageLength = Convert.ToInt32(ztl.maxResults);
            int totalpages = totalResults / pageLength + 1;

            Console.WriteLine("{0} : {1} : {2}", totalResults, pageLength, totalpages);

            int i = 1;
            for (i = 1; i < totalpages; i++)
            {
                if (pack.ToLower().Equals("jobmaps"))
                    url = restURL + string.Format("/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND component = {3}", "IDRIVE", "Test Status", codeversion, JobMapsComponent);
                else if (pack.ToLower().Equals("mt"))
                    url = restURL + string.Format("/rest/api/2/search?maxResults=100&startAt={3}&jql=project = {0} AND issuetype = Test AND (\"{1}\" = Automated OR \"{1}\" = Merged) AND affectedVersion <= \"{2}\" AND component != {4}", "IDRIVE", "Test Status", codeversion, i * pageLength, JobMapsComponent);
                else
                    url = restURL + string.Format("/rest/api/2/search?maxResults=100&startAt={3}&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND component != {4}", "IDRIVE", "Test Status", codeversion, i * pageLength, JobMapsComponent);
                Console.WriteLine(url);
                issueslist = getEntity(url);
                ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
                searchResults.AddRange(ztl.issues);
                Console.WriteLine("{0} : {1} : {2}", ztl.startAt, ztl.maxResults, ztl.total);
            }
            return searchResults;

        }
        public List<zTest> getRegressionTestsByModule(string codeversion, string pack = null, string Modules = null)
        {
            string url = "";
            List<zTest> searchResults = new List<zTest>();

            if (pack.ToLower().Equals("mt") || pack.ToLower().Equals("profiler"))
            {
                url = restURL + string.Format(
                          "/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND (\"{1}\" = Automated OR \"{1}\" = Merged) AND affectedVersion <= \"{2}\" AND component in ({3}) AND \"Script ID\" is not EMPTY AND \"Suite ID\" is not EMPTY AND labels is not EMPTY",
                          "IDRIVE", "Test Status", codeversion, Modules);
            }
            else
            {
                url = restURL + string.Format(
                          "/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND component in ({3}) AND \"Script ID\" is not EMPTY AND \"Suite ID\" is not EMPTY AND labels is not EMPTY",
                          "IDRIVE", "Test Status", codeversion, Modules);
            }

            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            searchResults.AddRange(ztl.issues);

            int totalResults = Convert.ToInt32(ztl.total);
            int pageLength = Convert.ToInt32(ztl.maxResults);
            int totalpages = totalResults / pageLength + 1;

            Console.WriteLine("{0} : {1} : {2}", totalResults, pageLength, totalpages);

            int i = 1;
            for (i = 1; i < totalpages; i++)
            {
                if (pack.ToLower().Equals("mt") || pack.ToLower().Equals("profiler"))
                {
                    url = restURL + string.Format(
                              "/rest/api/2/search?maxResults=100&startAt={4}&jql=project = {0} AND issuetype = Test AND (\"{1}\" = Automated OR \"{1}\" = Merged) AND affectedVersion <= \"{2}\" AND component in ({3}) AND \"Script ID\" is not EMPTY AND \"Suite ID\" is not EMPTY AND labels is not EMPTY",
                              "IDRIVE", "Test Status", codeversion, Modules, i * pageLength);
                }
                else
                {
                    url = restURL + string.Format(
                              "/rest/api/2/search?maxResults=100&startAt={4}&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND component in ({3}) AND \"Script ID\" is not EMPTY AND \"Suite ID\" is not EMPTY AND labels is not EMPTY",
                              "IDRIVE", "Test Status", codeversion, Modules, i * pageLength);
                }

                Console.WriteLine(url);
                issueslist = getEntity(url);
                ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
                searchResults.AddRange(ztl.issues);
                Console.WriteLine("{0} : {1} : {2}", ztl.startAt, ztl.maxResults, ztl.total);
            }
            return searchResults;

        }

        public List<zTest> getRegressionTestsWithRrt(string codeversion, string pack = null, string Modules = null)
        {
            string url;
            List<zTest> searchResults = new List<zTest>();

            if (pack.ToLower().Equals("mt"))
            {
                url = restURL + string.Format(
                          "/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND (\"{1}\" = Automated OR \"{1}\" = Merged) AND affectedVersion <= \"{2}\" AND component in ({3}) AND \"Script ID\" is not EMPTY AND \"Suite ID\" is not EMPTY AND labels is not EMPTY",
                          "IDRIVE", "Test Status", codeversion, Modules);
            }
            else
            {
                url = restURL + string.Format(
                          "/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND component in ({3}) AND \"Script ID\" is not EMPTY AND \"Suite ID\" is not EMPTY AND labels is not EMPTY",
                          "IDRIVE", "Test Status", codeversion, Modules);
            }

            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            searchResults.AddRange(ztl.issues);

            int totalResults = Convert.ToInt32(ztl.total);
            int pageLength = Convert.ToInt32(ztl.maxResults);
            int totalpages = totalResults / pageLength + 1;

            Console.WriteLine("{0} : {1} : {2}", totalResults, pageLength, totalpages);

            int i = 1;
            for (i = 1; i < totalpages; i++)
            {
                if (pack.ToLower().Equals("mt"))
                {
                    url = restURL + string.Format(
                              "/rest/api/2/search?maxResults=100&startAt={4}&jql=project = {0} AND issuetype = Test AND (\"{1}\" = Automated OR \"{1}\" = Merged) AND affectedVersion <= \"{2}\" AND component in ({3}) AND \"Script ID\" is not EMPTY AND \"Suite ID\" is not EMPTY AND labels is not EMPTY",
                              "IDRIVE", "Test Status", codeversion, Modules, i * pageLength);
                }
                else
                {
                    url = restURL + string.Format(
                              "/rest/api/2/search?maxResults=100&startAt={4}&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND component in ({3}) AND \"Script ID\" is not EMPTY AND \"Suite ID\" is not EMPTY AND labels is not EMPTY",
                              "IDRIVE", "Test Status", codeversion, Modules, i * pageLength);
                }

                Console.WriteLine(url);
                issueslist = getEntity(url);
                ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
                searchResults.AddRange(ztl.issues);
                Console.WriteLine("{0} : {1} : {2}", ztl.startAt, ztl.maxResults, ztl.total);
            }

            List<string> RrtSubmodules = new List<string>
            {
                "RRT.ReleaseTesting.Accounts",
                "RRT.ReleaseTesting.Aftersales",
                "RRT.ReleaseTesting.Environment",
                "RRT.ReleaseTesting.Vehicles"
            };

            url = restURL + string.Format(
                      "/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND \"{1}\" = Merged AND affectedVersion <= \"{2}\" AND component in ({3}) AND labels in ({4}) AND \"Script ID\" is not EMPTY AND \"Suite ID\" is not EMPTY",
                      "IDRIVE", "Test Status", codeversion, Modules, string.Join(",", RrtSubmodules));

            Console.WriteLine(url);
            string RrtIssueslist = getEntity(url);
            zTestList Rrztl = JsonConvert.DeserializeObject<zTestList>(RrtIssueslist);
            searchResults.AddRange(Rrztl.issues);


            return searchResults;

        }

        public List<zTest> getTestsByScriptIds(List<string> testList, string testStatus)
        {

            List<zTest> searchResults = new List<zTest>();
            string scriptIdsJql = "\"Script ID\" ~ ";
            scriptIdsJql += string.Join(" OR \"Script ID\" ~ ", testList);
            string url =
                restURL + string.Format(
                    "/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND \"Suite ID\" is not EMPTY AND labels is not EMPTY AND ({2})",
                    "IDRIVE", "Test Status", scriptIdsJql);

            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            searchResults.AddRange(ztl.issues);

            int totalResults = Convert.ToInt32(ztl.total);
            int pageLength = Convert.ToInt32(ztl.maxResults);
            int totalpages = totalResults / pageLength + 1;

            Console.WriteLine("{0} : {1} : {2}", totalResults, pageLength, totalpages);

            int i = 1;
            for (i = 1; i < totalpages; i++)
            {
                url = restURL + string.Format(
                          "/rest/api/2/search?maxResults=100&startAt={3}&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND \"Suite ID\" is not EMPTY AND labels is not EMPTY AND ({2})",
                          "IDRIVE", "Test Status", scriptIdsJql, i * pageLength);

                Console.WriteLine(url);
                issueslist = getEntity(url);
                ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
                searchResults.AddRange(ztl.issues);
                Console.WriteLine("{0} : {1} : {2}", ztl.startAt, ztl.maxResults, ztl.total);
            }
            return searchResults;
        }

        public List<zTest> getAllZephyrTests()
        {
            List<zTest> searchResults = new List<zTest>();
            var url = restURL + string.Format(
                             "/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND (\"{1}\" = Automated OR \"{1}\" = Merged)",
                             "IDRIVE", "Test Status");

            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            searchResults.AddRange(ztl.issues);

            int totalResults = Convert.ToInt32(ztl.total);
            int pageLength = Convert.ToInt32(ztl.maxResults);
            int totalpages = totalResults / pageLength + 1;

            Console.WriteLine("{0} : {1} : {2}", totalResults, pageLength, totalpages);

            for (int i = 1; i < totalpages; i++)
            {
                url = restURL + string.Format(
                          "/rest/api/2/search?maxResults=100&startAt={2}&jql=project = {0} AND issuetype = Test AND (\"{1}\" = Automated OR \"{1}\" = Merged)",
                          "IDRIVE", "Test Status", i * pageLength);
                Console.WriteLine(url);
                issueslist = getEntity(url);
                ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
                searchResults.AddRange(ztl.issues);
                Console.WriteLine("{0} : {1} : {2}", ztl.startAt, ztl.maxResults, ztl.total);
            }
            return searchResults;
        }

        public List<zTest> GetRegressionTestsForTesting(string codeversion, string pack = null, string Modules = null)
        {
            string url = "";
            string subModules = "POS_Service_VHC, POS_Stock_Checking, VS_Specification, IA.BSO, CRM_Loyalty, ACC_SA_Accrual, ACC_IA_Assetledger";
            List<zTest> searchResults = new List<zTest>();

            url = restURL + string.Format("/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND labels in ({3})", "IDRIVE", "Test Status", codeversion, subModules);

            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            searchResults.AddRange(ztl.issues);

            int totalResults = Convert.ToInt32(ztl.total);
            int pageLength = Convert.ToInt32(ztl.maxResults);
            int totalpages = totalResults / pageLength + 1;

            Console.WriteLine("{0} : {1} : {2}", totalResults, pageLength, totalpages);

            int i = 1;
            for (i = 1; i < totalpages; i++)
            {
                url = restURL + string.Format("/rest/api/2/search?maxResults=100&startAt={4}&jql=project = {0} AND issuetype = Test AND \"{1}\" = Automated AND affectedVersion <= \"{2}\" AND component in ({3})", "IDRIVE", "Test Status", codeversion, subModules, i * pageLength);
                Console.WriteLine(url);
                issueslist = getEntity(url);
                ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
                searchResults.AddRange(ztl.issues);
                Console.WriteLine("{0} : {1} : {2}", ztl.startAt, ztl.maxResults, ztl.total);
            }
            return searchResults;

        }
        public void AddTestStepsbyScriptID(string scriptID, string stepsjson)
        {
            //Get the test ID from Script ID
            zTest existingTest = checkIfTestExists(scriptID);
            if (existingTest != null)
            {
                
                List<zTestStep> steps = JsonConvert.DeserializeObject<List<zTestStep>>(stepsjson);
                Console.WriteLine("Came in adding test steps" + steps.Count + ":" + string.Join("^^", steps));
                AddTestSteps(existingTest.id, steps);
            }
        }
        public List<zTest> getDeploymentTests(string codeversion, bool EAT = false)
        {
            List<zTest> searchResults = new List<zTest>();

            string url;
            if (EAT)
                url = restURL + string.Format("/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND Labels in (ReleaseTesting.Smoke, Deployment.EAT) AND \"{1}\" = Automated", "IDRIVE", "Test Status");
            else
                url = restURL + string.Format("/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND Labels in (ReleaseTesting.Smoke) AND \"{1}\" = Automated", "IDRIVE", "Test Status");


            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            searchResults.AddRange(ztl.issues);
            return searchResults;

        }
        public List<zTest> getTestsBylabel(string label)
        {
            List<zTest> searchResults = new List<zTest>();

            string url;
            url = restURL + string.Format("/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND Labels in (\"{2}\") AND \"{1}\" = Automated", "IDRIVE", "Test Status", label);
            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            searchResults.AddRange(ztl.issues);
            return searchResults;

        }

        public void RemoveCloneFromSummary(zTest currentTest)
        {
            //   string key = "IDRIVE-18333";
            zTest target = checkIfTestExists(currentTest.key);
            if (currentTest != null)
            {
                Dictionary<string, string> update = new Dictionary<string, string>();
                Dictionary<string, Dictionary<string, string>> finalUpdate = new Dictionary<string, Dictionary<string, string>>();
                update.Add("summary", currentTest.fields.summary.Replace("CLONE - ", string.Empty));
                finalUpdate.Add("fields", update);
                updateTest(currentTest, JsonConvert.SerializeObject(finalUpdate));
            }



        }

        public virtual void createorupdateZtest()
        {

            zapi zp = new zapi("svc_autoline_ivc", "6xL@tCdw]/");
            zTest zt = new zTest();
            zt.fields.summary = TestContext.CurrentContext.Test.Properties["Description"].ToString();
            zt.fields.description = TestContext.CurrentContext.Test.Properties["Description"].ToString();
            zt.fields.components.Add(new jComponent("Vehicles"));
            zt.fields.labels.Add("Vehicles.BPO");
            zt.fields.project.key = "IDRIVE";
            zt.fields.issuetype.name = "Test";
            zt.fields.ScriptID = TestContext.CurrentContext.Test.Name;
            zt.fields.SuiteID = TestContext.CurrentContext.Test.Name;
            zt.fields.teststatus.value = "Automated";
            zt = zp.createTest(zt);
            List<zTestStep> hh = new List<zTestStep>();
            hh.Add(new zTestStep("Check the navigation", "Navigation is good", "FF>FF>fre"));
            zp.AddTestSteps(zt.id, hh);
        }
        public virtual void ExtractFile(string source)
        {
            string zPath = @"C:\Program Files\7-Zip\7zG.exe";// change the path and give yours 
            try
            {
                ProcessStartInfo pro = new ProcessStartInfo();
                pro.WindowStyle = ProcessWindowStyle.Normal;
                pro.FileName = zPath;
                pro.Arguments = "x \"" + source + "\"";
                Console.WriteLine("Command line :" + pro.Arguments);
                Process x = Process.Start(pro);

                x.WaitForExit();
            }
            catch (System.Exception Ex)
            {
                Console.WriteLine("ZIP problem " + Ex.Message + Ex.InnerException);
            }
        }
        public string EstimateAutomationTasks(string issuekey)
        {
            int totalsteps = 0;
            int validattachments = 0;
            zTest jt = getSingleIssue(issuekey);
            WebClient webClient = new WebClient();
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes("svc_autoline_ivc" + ":" + "6xL@tCdw]/"));
            webClient.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
            if (jt.fields.attachment.Count > 0)
            {
                int jk = 0;
                foreach (jAttachment ja in jt.fields.attachment)
                {
                    jk++;
                    bool go = false;
                    if (ja.mimeType.ToLower().Contains("x-7z-compressed"))
                    {
                        string targetfile = Path.Combine(@"C:\Sastry Poranki\code\", ja.filename);
                        Console.WriteLine(targetfile);
                        webClient.DownloadFile(ja.content, targetfile);
                        string filename = ja.filename;
                        string foldername = filename.Replace(".7z", "");
                        ExtractFile(targetfile);
                        go = true;
                        validattachments++;
                    }
                    else
                    {
                        if (ja.mimeType.ToLower().Contains("zip"))
                        {
                            string targetfile = @"C:\Sastry Poranki\code\" + issuekey.Replace("-", "") + jk + ".zip";
                            webClient.DownloadFile(ja.content, targetfile);
                            string filename = ja.filename;
                            string foldername = filename.Replace(".zip", "");
                            string zipPath = @"C:\Sastry Poranki\code\" + issuekey.Replace("-", "") + jk + ".zip";
                            ZipFile.ExtractToDirectory(zipPath, Directory.GetCurrentDirectory());
                            go = true;
                            validattachments++;
                        }
                    }

                    if (go)
                    {
                        string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "QTPscript_*.xml", SearchOption.AllDirectories);
                        if (files != null)
                        {
                            string fullfilename = files[0];
                            XmlDocument xd = new XmlDocument();
                            xd.Load(fullfilename);
                            Console.WriteLine("Steps for {0}-{1} : {2}", issuekey, jk, xd.DocumentElement.SelectNodes("Command").Count);
                            FileInfo fi = new FileInfo(fullfilename);
                            Directory.Delete(fi.Directory.FullName, true);
                            totalsteps = totalsteps + Convert.ToInt32(xd.DocumentElement.SelectNodes("Command").Count);
                        }
                    }
                }
            }


            #region Estimate Calculation
            double estimate = 0;
            if (totalsteps.Equals(0))
            {
                estimate = 0;
            }
            else
            {
                double admineffort = Convert.ToInt32(jt.fields.attachment.Count) * 0.5;
                int scriptingeffort = ((totalsteps / 40) + 1) * 8;
                estimate = Math.Round(admineffort) + scriptingeffort;
            }

            #endregion

            return string.Format ("Estimates for {0} : {1} : {2} : {3}", issuekey, validattachments, totalsteps, estimate);
        }

        public void AddIssueLink(createIssueLink link)
        {
                      
            string url = "https://projects.cdk.com/rest/api/2/issueLink";
            postEntity(url, JsonConvert.SerializeObject(link));
        }


        #endregion

        public List<zTest> GetBrowserTests(string label, string status)
        {
            List<string> testList = new List<string>
            {
                "Vmbr15167001",
                "Vmbr15167002",
                "Vmbr15167003",
                "Vmbr15167004",
                "Vmbr15167005",
                "Vmbr21167001"
            };
            List<zTest> searchResults = new List<zTest>();
            string scriptIdsJql = "\"Script ID\" ~ ";
            string version = "N1.68 Sep/Oct";
            scriptIdsJql += string.Join(" OR \"Script ID\" ~ ", testList);
            string url =
                restURL + string.Format(
                    "/rest/api/2/search?maxResults=100&jql=project = {0} AND issuetype = Test AND \"{1}\" = Merged AND \"Suite ID\" is not EMPTY AND labels is not EMPTY AND ({2})",
                    "IDRIVE", "Test Status", scriptIdsJql);
            string issueslist = getEntity(url);
            zTestList ztl = JsonConvert.DeserializeObject<zTestList>(issueslist);
            searchResults.AddRange(ztl.issues);
            return searchResults;
        }
    }
    public class almapi
    {
        Dictionary<string, string> submodulemapping;
        Dictionary<string, string> versionmapping;
        Dictionary<string, string> modulemapping;
        public static string almheader;
        string qcuser = "ivcauto1";
        string qcpass = "CDKcdk11";
        string qcurl = "http://dsalm.ds.ad.adp.com:8080/qcbin";
        public almapi()
        {
            //almheader = auth(qcurl + "/authentication-point/alm-authenticate", "<?xml version='1.0' encoding='utf-8'?><alm-authentication><user>" + qcuser + "</user><password>" + qcpass + "</password></alm-authentication>");

            submodulemapping = new Dictionary<string, string>();
            versionmapping = new Dictionary<string, string>();
            modulemapping = new Dictionary<string, string>();

            submodulemapping.Add("Service POS", "POS.Service");
            submodulemapping.Add("Vehicle Health Check", "POS.VHC");
            submodulemapping.Add("Purchase Control", "POS.PC");
            submodulemapping.Add("Workshop Loading", "POS.WL");
            submodulemapping.Add("Parts POS", "POS.Parts");
            submodulemapping.Add("Stock Checking", "POS.SC");

            submodulemapping.Add("Miscellaneous", "CRM.MISC");
            submodulemapping.Add("Customer Management", "CRM.Core");
            submodulemapping.Add("Franchise Data", "Vehicles.DD");
            submodulemapping.Add("Call List Manager", "CRM.CALL");
            submodulemapping.Add("Nominal Ledger", "SA.NL");
            submodulemapping.Add("Accruals", "SA.AC");
            submodulemapping.Add("Asset Register", "SA.AR");
            submodulemapping.Add("Purchase Ledger", "SA.PL");
            submodulemapping.Add("Sales Ledger", "SA.SL");
            submodulemapping.Add("Campaign Manager", "CRM.CAMP");
            submodulemapping.Add("Back Office", "Vehicles.BO");
            submodulemapping.Add("Front Office", "Vehicles.FO");

            submodulemapping.Add("Bulk Purchase Order", "Vehicles.BPO");
            submodulemapping.Add("Survey Manager", "CRM.Survey");
            submodulemapping.Add("Complaint Manager", "CRM.Complaint");
            submodulemapping.Add("Finance", "Vehicles.Finance");
            submodulemapping.Add("Accounts Payables", "IA.Payables");
            submodulemapping.Add("Bulk Sales Order", "Vehicles.BSO");
            submodulemapping.Add("General Ledger", "IA.GeneralLedger");
            submodulemapping.Add("Cashbook", "IA.Cashbook");
            submodulemapping.Add("Accounts Recievables", "IA.Recievables");
            submodulemapping.Add("Shipments", "Vehicles.Shipments");
            submodulemapping.Add("System Utilities", "Environments.SU");
            submodulemapping.Add("IA Back Office", "IA.BO");
            submodulemapping.Add("Asset Ledger", "IA.AssetLedger");
            submodulemapping.Add("IA Front Office", "IA.FO");
            submodulemapping.Add("IA ServicePOS", "IA.SPOS");


            versionmapping.Add("1.26", "N1.50 May-2016");
            versionmapping.Add("1.39", "N1.50 May-2016");
            versionmapping.Add("1.40", "N1.50 May-2016");
            versionmapping.Add("1.28", "N1.50 May-2016");
            versionmapping.Add("1.29", "N1.50 May-2016");
            versionmapping.Add("1.31", "N1.50 May-2016");
            versionmapping.Add("1.44", "N1.50 May-2016");
            versionmapping.Add("1.36", "N1.50 May-2016");
            versionmapping.Add("1.41", "N1.50 May-2016");
            versionmapping.Add("1.32", "N1.50 May-2016");
            versionmapping.Add("1.34", "N1.50 May-2016");
            versionmapping.Add("1.42", "N1.50 May-2016");
            versionmapping.Add("1.38", "N1.50 May-2016");
            versionmapping.Add("1.43", "N1.50 May-2016");
            versionmapping.Add("1.45", "N1.50 May-2016");
            versionmapping.Add("1.46", "N1.50 May-2016");
            versionmapping.Add("1.47", "N1.50 May-2016");
            versionmapping.Add("1.48", "N1.50 May-2016");
            versionmapping.Add("1.49", "N1.50 May-2016");
            versionmapping.Add("1.50", "N1.50 May-2016");
            versionmapping.Add("1.51", "N1.51 Jun-2016");
            versionmapping.Add("1.52", "N1.52 Jul-2016");
            versionmapping.Add("1.53", "N1.53 Aug-2016");
            versionmapping.Add("1.54", "N1.54 Sep-2016");
            versionmapping.Add("1.55", "N1.55 Oct-2016");

            modulemapping.Add("Aftersales", "Aftersales");
            modulemapping.Add("Customer Management", "CRM");
            modulemapping.Add("Franchise Data", "Vehicles");
            modulemapping.Add("Accounts", "Accounts");
            modulemapping.Add("Vehicle Management", "Vehicles");
            modulemapping.Add("Advanced Vehicle Management", "Vehicles");
            modulemapping.Add("Environments", "Environments");


        }
        public zTest Alm2Zephyr(string testid, ref List<string> designSteps, jProject jp)
        {
            //string url = "http://dsalm.ds.ad.adp.com:8080/qcbin/rest/domains/DSI/projects/DSIAUTOLINE/tests?page-size=max&fields=user-04,user-12,id,name,owner,user-10,user-01,user-02,user-03,user-06&query={status[automated];user-07[" + product + "];user-03[\"" + module + "\"];user-04[\"" + submodule + "\"]}";
            string url = "http://dsalm.ds.ad.adp.com:8080/qcbin/rest/domains/DSI/projects/DSIAUTOLINE/tests?page-size=max&fields=user-04,user-12,id,name,owner,user-10,user-01,user-02,user-03,description,user-06&query={id[" + testid + "]}";
            //1438
            Stream alltests = get(url);
            XmlDocument alltestXML = new XmlDocument();
            alltestXML.Load(alltests);
            zTest jiraticket = new zTest();

            foreach (XmlNode tmp in alltestXML.SelectNodes("//Entities/Entity"))
            {
                string testname = tmp.SelectSingleNode("Fields/Field[@Name='name']").SelectSingleNode("Value").InnerText;
                string scriptID = tmp.SelectSingleNode("Fields/Field[@Name='user-12']").SelectSingleNode("Value").InnerText;
                //string testid = tmp.SelectSingleNode("Fields/Field[@Name='id']").SelectSingleNode("Value").InnerText;
                string testdescription = StripHTML(tmp.SelectSingleNode("Fields/Field[@Name='description']").SelectSingleNode("Value").InnerText);
                string author = StripHTML(tmp.SelectSingleNode("Fields/Field[@Name='owner']").SelectSingleNode("Value").InnerText);
                string relevant = StripHTML(tmp.SelectSingleNode("Fields/Field[@Name='user-10']").SelectSingleNode("Value").InnerText);
                string F2US = StripHTML(tmp.SelectSingleNode("Fields/Field[@Name='user-01']").SelectSingleNode("Value").InnerText);
                string IVUS = StripHTML(tmp.SelectSingleNode("Fields/Field[@Name='user-02']").SelectSingleNode("Value").InnerText);
                string Module = StripHTML(tmp.SelectSingleNode("Fields/Field[@Name='user-03']").SelectSingleNode("Value").InnerText);
                string subMod = StripHTML(tmp.SelectSingleNode("Fields/Field[@Name='user-04']").SelectSingleNode("Value").InnerText);
                string suite = StripHTML(tmp.SelectSingleNode("Fields/Field[@Name='user-06']").SelectSingleNode("Value").InnerText);
                string codeBranch = StripHTML(tmp.SelectSingleNode("Fields/Field[@Name='user-10']").SelectSingleNode("Value").InnerText);


                jiraticket.fields.description = testdescription[0] + "\n F2_US : " + F2US;

                if (testdescription.Length > 200)
                {
                    jiraticket.fields.summary = testname;
                }
                else
                {
                    jiraticket.fields.summary = testdescription; // testname[0];
                }
                jiraticket.fields.issuetype.name = "Test";
                jProject jiraproject = new jProject();
                jiraproject.key = jp.key;
                jiraticket.fields.project = jiraproject;

                if (submodulemapping.Keys.Contains(subMod))
                    jiraticket.fields.labels.Add(submodulemapping[subMod]);
                else
                    jiraticket.fields.labels.Add(subMod.Replace(" ", "_"));

                if (modulemapping.Keys.Contains(Module))
                    jiraticket.fields.components.Add(new jComponent(modulemapping[Module]));
                else
                    jiraticket.fields.components.Add(new jComponent(modulemapping["Vehicles"]));

                jiraticket.fields.teststatus.value = "Requires Automation";
                jiraticket.fields.ScriptID = "";
                jiraticket.fields.SuiteID = "";

                string jiraversion = "N1.50 May-2016";
                if (versionmapping.Keys.Contains(codeBranch))
                {
                    jiraversion = versionmapping[codeBranch];
                }
                jVersion currentVersion = jp.versions.Find(t => t.name.Equals(jiraversion));
                jiraticket.fields.versions.Add(currentVersion);

                //Get the design steps
                string designstepsurl = "http://dsalm.ds.ad.adp.com:8080/qcbin/rest/domains/DSI/projects/DSIAUTOLINE/design-steps?fields=description,expected&query={parent-id[" + testid + "]}";
                Stream designStepsStream = get(designstepsurl);
                XmlDocument design = new XmlDocument();
                design.Load(designStepsStream);

                foreach (XmlNode tmp1 in design.SelectNodes("//Entities/Entity"))
                {
                    string designStep = StripHTML(tmp1.SelectSingleNode("Fields/Field[@Name='description']").SelectSingleNode("Value").InnerText);
                    string expected = "";
                    if (tmp1.SelectSingleNode("Fields/Field[@Name='expected']") != null)
                    {
                        expected = StripHTML(tmp1.SelectSingleNode("Fields/Field[@Name='expected']").SelectSingleNode("Value").InnerText);
                    }
                    designSteps.Add(designStep + "^" + expected);
                }
            }

            return jiraticket;


        }
        public static string StripHTML(string inputString)
        {
            if (inputString != null)
            {
                //return Regex.Replace(inputString, "<.*?>", string.Empty);
                string HTML_TAG_PATTERN = "<.*?>";
                return System.Text.RegularExpressions.Regex.Replace(inputString.ToString(), HTML_TAG_PATTERN, string.Empty).Replace("\n", String.Empty).Replace("\r", String.Empty).Replace("&nbsp;", string.Empty);
            }
            else
                return null;
        }
        public Stream get(string url)
        {
            //Console.WriteLine(url);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Accept = "application/xml";
            req.KeepAlive = true;
            req.AllowAutoRedirect = true;
            almheader = auth(qcurl + "/authentication-point/alm-authenticate", "<?xml version='1.0' encoding='utf-8'?><alm-authentication><user>" + qcuser + "</user><password>" + qcpass + "</password></alm-authentication>");
            req.Headers.Add("Cookie", almheader);
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream sr = res.GetResponseStream();
            return sr;
        }
        public static string auth(string url, string xml)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(xml.ToString());
            req.Method = "POST";
            req.ContentType = "application/xml";
            req.Accept = "application/xml";
            req.KeepAlive = true;
            req.AllowAutoRedirect = true;
            req.ContentLength = requestBytes.Length;
            Stream requestStream = req.GetRequestStream();
            requestStream.Write(requestBytes, 0, requestBytes.Length);
            requestStream.Close();
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
            string backstr = sr.ReadToEnd();
            string myheader = res.Headers.Get("Set-Cookie");
            sr.Close();
            res.Close();
            return myheader;
        }
        public string ReAuthenticate()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://dsalm.ds.ad.adp.com:8080/qcbin/rest/is-authenticated");
                request.Method = "GET";
                request.Accept = "application/xml";
                request.KeepAlive = true;
                request.AllowAutoRedirect = true;
                request.Headers.Add("Cookie", almheader);
                Console.WriteLine("Request : {0}", request.RequestUri.ToString());
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                if (response.ContentLength > 0)
                {
                    request.GetResponse();
                    //using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    //Output=rd.ReadToEnd();
                    //Console.WriteLine(Output);

                }
                request.Abort();
                return almheader;
            }
            catch (Exception ee)
            {
                Console.WriteLine("Session is expired, need to Reauthenticate again" + ee.StackTrace );

                almheader = auth(qcurl + "/authentication-point/alm-authenticate", "<?xml version='1.0' encoding='utf-8'?><alm-authentication><user>" + qcuser + "</user><password>" + qcpass + "</password></alm-authentication>");
                Console.WriteLine("Connected to REST");
                return almheader;
            }

        }
    }
   
}
    



