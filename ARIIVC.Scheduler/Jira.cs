using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using zephyrapi;

namespace ARIIVC.Scheduler
{
    public class Jira
    {
        private readonly string _userName;
        private readonly string _password;
        private readonly string _url;
        private readonly zapi _zephryApi;


        public Jira(string userName, string password, string url)
        {
            _userName = userName;
            _password = password;
            _url = url;
            _zephryApi = new zapi(_userName, _password);
        }

        public void Update(string jiraItemId)
        {
            zTest jt = _zephryApi.getSingleIssue(jiraItemId);
            string itemType = jt.fields.issuetype.name;
            switch (itemType)
            {
                case "Test":
                    UpdateScriptIdInParent(jiraItemId);
                    break;
                case "Automation":
                    CheckTestStatusAndCloseTask(jiraItemId);
                    break;
            }
        }

        public void UpdateScriptIdInParent(string JirasubtaskID)
        {
            zTest jt = _zephryApi.getSingleIssue(JirasubtaskID);
            string scriptId = jt.fields.ScriptID;
            string taskId = jt.key;
            string jiraParentId = jt.fields.issuelinks[0].outwardIssue.id;
            string comment = "The test case Id " + taskId + "is automated with scriptId " + scriptId;
            string jiraCommentUrl = string.Format("https://projects.cdk.com/rest/api/2/issue/{0}/comment", jiraParentId);
            UpdateComment(jiraCommentUrl, comment);
        }

        public void CheckTestStatusAndCloseTask(string jirasubtaskId)
        {
            bool canClose = true;
            string jiraURL = string.Format("https://projects.cdk.com/rest/api/2/issue/{0}", jirasubtaskId);
            var json = zapi.getEntity(jiraURL);
            zTest zt = JsonConvert.DeserializeObject<zTest>(json);
            var results = JsonConvert.DeserializeObject<dynamic>(json);
            //check the status for all the sub-tasks and decide if you want to close this one
            for (int i = 0; i < results.fields.issuelinks.Count; i++)
            {
                if (results.fields.issuelinks[i].inwardIssue.fields.status.name == "To Do")
                {
                    canClose = false;
                    break;
                }
            }

            if (canClose)
            {
                string closeparentTaskurl = string.Format("https://projects.cdk.com/rest/api/2/issue/{0}/transitions",
                    jirasubtaskId);
                CloseIssue(closeparentTaskurl);
                Console.WriteLine("Closed parent task");
                Console.WriteLine("Job completed");
            }

        }

        public string UpdateComment(string url, String comment)
        {
            HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
            var jsonString = string.Empty;
            try
            {
                Dictionary<string, string> testComment = new Dictionary<string, string>();
                testComment.Add("body", comment);
                string testJsonString = JsonConvert.SerializeObject(testComment);
                jsonString = _zephryApi.postEntity(url, testJsonString);
            }

            catch (Exception e)
            {
                Console.WriteLine("Exception occured", e);
            }
            return jsonString;
        }

        public void CloseIssue(string url)
        {
            String ChangeStats = "{\"transition\": {\"id\": \"51\"}}";
            HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
            var jsonString = string.Empty;
            try
            {
                HttpWebRequest req1 = (HttpWebRequest) WebRequest.Create(url);
                byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(ChangeStats);
                req.Method = "POST";
                req.ProtocolVersion = HttpVersion.Version11;
                req.ContentType = "application/json";
                req.Accept = "application/json";
                req.KeepAlive = true;
                req.AllowAutoRedirect = true;
                req.Headers["Authorization"] = "Basic " +
                                               Convert.ToBase64String(Encoding.Default.GetBytes(_userName + ":" + _password));
                req.ContentLength = requestBytes.Length;
                Stream requestStream = req.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
                HttpWebResponse res = (HttpWebResponse) req.GetResponse();
                StreamReader sr = new StreamReader(res.GetResponseStream(), System.Text.Encoding.Default);
                string backstr = sr.ReadToEnd();
                sr.Close();
                res.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured", e);
            }
        }

        public List<zTest> TestsInCurrentPackAndVersion(string pack, string versionId)
        {
            zapi zp = new zapi(_userName, _password, _url);
            jProject jp = zp.getProject("IDRIVE");
            jVersion currentVersion = jp.versions.Find(t => t.name.Contains(versionId));
            List<zTest> ztl = zp.getRegressionTests(currentVersion.name, pack);
            StringBuilder append = new StringBuilder();
            foreach (zTest tmp in ztl)
            {
                append.AppendLine(tmp.fields.labels[0]);
            }
            return ztl;
        }

        public jProject Project(string projectKey)
        {
            jProject jp = _zephryApi.getProject("IDRIVE");
            return jp;
        }

        public List<zTest> DeploymentTestsInCodeVersion(string codeVersion)
        {
            return _zephryApi.getDeploymentTests(codeVersion);
        }

        public List<zTest> TestsByLabel(string label)
        {
            return _zephryApi.getTestsBylabel(label);
        }


        public string CreateTestCycle(zTestCycle ztc)
        {
            string cycleId = _zephryApi.createTestCycle(ztc);
            return cycleId;
        }


        public List<zTest> TestsByModule(string filter, string pack, string commaSeparatedModules)
        {
            List<zTest> ztl = _zephryApi.getRegressionTestsByModule(filter, pack, commaSeparatedModules);
            return ztl;
        }

        public void  AddTestsList(string projectId, string cycleId, List<zTest> testList)
        {
            _zephryApi.AddTestsbyList(projectId, cycleId, testList);
        }

        public zExecutions TestSetExecutionId(string testSetId)
        {
            return _zephryApi.getExecutionId(testSetId);

        }

    }

}
