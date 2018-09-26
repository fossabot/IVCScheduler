using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Logger;
using Newtonsoft.Json;
using ARIIVC.Scheduler.JsonReps;
using System.Xml;
using System.Net;
using System.IO;
using ARIIVC.Utilities;

namespace ARIIVC.Scheduler
{
    public class Jenkins
    {
        string serverbaseurl = "http://c05drddrv89.dslab.ad.adp.com:8080";
        RestCall restapi;
        public Jenkins()
        {
            restapi = new RestCall();
        }

        public Jenkins(string anotherUrl)
        {
            restapi = new RestCall();
            serverbaseurl = anotherUrl;
        }

        public List<TestLog> TestScheduled()
        {
            List<TestLog> scheduleLogs = new List<TestLog>();
            scheduleLogs.Add(BuildTestLogForTestScheduled());
            return scheduleLogs;
        }

        private TestLog BuildTestLogForTestScheduled()
        {
            StringBuilder logMessage = new StringBuilder();
            logMessage.AppendLine("Scheduled test from Jenkins");
            List<string> environmentVariablesToLog = new List<string>()
            {
                "BUILD_URL",
                "COMPUTERNAME",
                "NODE_NAME",
                "appserver",
                "pack",
                "testname",
                "submodule",
                "product",
                "label"
            };
            foreach (var environmentVariable in environmentVariablesToLog)
            {
                logMessage.AppendLine(LogMessageForEnvironmentVariables(environmentVariable));
            }
            TestLog scheduleLog = new TestLog(LogType.Step.ToString(), "Scheduled Test", logMessage.ToString(), true);
            return scheduleLog;
        }

        private string LogMessageForEnvironmentVariables(string environmentVariable)
        {
            string value = Environment.GetEnvironmentVariable(environmentVariable) ?? "NULL";
            string logMessage = "Environment variable - " + environmentVariable + " set to " + value;
            return logMessage;
        }

        public List<jenkinsNode> getIdleNodes()
        {
            restapi.Url = serverbaseurl + "/computer/api/json";
            jenkinsNodes allNodes = JsonConvert.DeserializeObject<jenkinsNodes>(restapi.Get());
            return allNodes.computer.FindAll(t => t.idle && (!t.offline) && t.displayName.StartsWith("gbh-ivc-pc"));
        }
        public jenkinsNode GetOneNode(string nodeName)
        {
            restapi.Url = serverbaseurl + "/computer/" + nodeName + "/api/json";
            return JsonConvert.DeserializeObject<jenkinsNode>(restapi.Get());
        }

        public void changeNodeLabel(List<jenkinsNode> nodes, string label)
        {
            foreach (jenkinsNode singleNode in nodes)
            {

                restapi.Url = serverbaseurl + string.Format("/computer/{0}/config.xml", singleNode.displayName);
                string response = restapi.Get().Replace("version=\"1.1\"", "version=\"1.0\"");
                XmlDocument responseXML = new XmlDocument();
                responseXML.LoadXml(response);
                responseXML.SelectSingleNode("//slave//label").InnerText = label;
                HttpWebRequest updateRequest = (HttpWebRequest)HttpWebRequest.Create(restapi.Url);
                updateRequest.Method = "POST";
                updateRequest.ContentType = "application/xml;type=collection";
                byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(responseXML.OuterXml.ToString());
                updateRequest.ContentLength = requestBytes.Length;
                Stream requestStream = updateRequest.GetRequestStream();
                requestStream.Write(requestBytes, 0, requestBytes.Length);
                requestStream.Close();
                WebResponse updateResponse = updateRequest.GetResponse();
                StreamReader sr1 = new StreamReader(updateResponse.GetResponseStream(), System.Text.Encoding.Default);
                string postResponse = sr1.ReadToEnd();
                sr1.Close();
                updateResponse.Close();
                Console.WriteLine("Updated the label of the node {0} : {1}", singleNode.displayName, label);
            }
        }

        public void changeNodeLabel(jenkinsNode singleNode, string label)
        {

            restapi.Url = serverbaseurl + string.Format("/computer/{0}/config.xml", singleNode.displayName);
            string response = restapi.Get().Replace("version=\"1.1\"", "version=\"1.0\"");
            XmlDocument responseXML = new XmlDocument();
            responseXML.LoadXml(response);
            responseXML.SelectSingleNode("//slave//label").InnerText = label;
            HttpWebRequest updateRequest = (HttpWebRequest)HttpWebRequest.Create(restapi.Url);
            updateRequest.Method = "POST";
            updateRequest.ContentType = "application/xml;type=collection";
            byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(responseXML.OuterXml.ToString());
            updateRequest.ContentLength = requestBytes.Length;
            Stream requestStream = updateRequest.GetRequestStream();
            requestStream.Write(requestBytes, 0, requestBytes.Length);
            requestStream.Close();
            WebResponse updateResponse = updateRequest.GetResponse();
            StreamReader sr1 = new StreamReader(updateResponse.GetResponseStream(), System.Text.Encoding.Default);
            string postResponse = sr1.ReadToEnd();
            sr1.Close();
            updateResponse.Close();
            Console.WriteLine("Updated the label of the node {0} : {1}", singleNode.displayName, label);
        }

        public void TriggerJob(string JobName, Dictionary<string,string> arguments)
        {
            List<string> sb = new List<string>();
            foreach (string src in arguments.Keys)
            {
                sb.Add(string.Format("{0}={1}", src,arguments[src]));
            }

            string paramsInput = string.Join("&", sb);

            #region Schedule to call again after 15 mins
            string url1 = String.Format("{0}/job/{1}/buildWithParameters?delay=5sec&{2}", serverbaseurl, JobName, paramsInput);
            Console.WriteLine("URL built is {0}", url1);
            WebRequest call = (HttpWebRequest)HttpWebRequest.Create(url1);
            call.Method = "POST";
            Console.WriteLine("\n\n\n");
            WebResponse callResponse = call.GetResponse();
            #endregion
        }
        public void TriggerJobByToken(string jobName, Dictionary<string, string> arguments, string token)
        {
            List<string> sb = new List<string>();
            foreach (string src in arguments.Keys)
            {
                sb.Add(string.Format("{0}={1}", src, arguments[src]));
            }

            string paramsInput = string.Join("&", sb);

            try
            {
                string url = String.Format("{0}/buildByToken/buildWithParameters?job={1}&token={2}&{3}", serverbaseurl, jobName, token, paramsInput);
                Console.WriteLine("URL built is {0}", url);
                WebRequest call = (HttpWebRequest)WebRequest.Create(url);
                call.Method = "POST";
                Console.WriteLine("\n\n\n");
                call.GetResponse();

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured : " + e);
            }
        }

        public void TriggerJobWithDelay(string JobName, string delayInSeconds, Dictionary<string, string> arguments)
        {
            List<string> sb = new List<string>();
            foreach (string src in arguments.Keys)
            {
                sb.Add(string.Format("{0}={1}", src, arguments[src]));
            }

            string paramsInput = string.Join("&", sb);

            #region Schedule to call again after 15 mins
            string url1 = String.Format("{0}/job/{1}/buildWithParameters?delay={3}sec&{2}", serverbaseurl, JobName, paramsInput, delayInSeconds);
            Console.WriteLine("URL built is {0}", url1);
            WebRequest call = (HttpWebRequest)HttpWebRequest.Create(url1);
            call.Method = "POST";
            Console.WriteLine("\n\n\n");
            WebResponse callResponse = call.GetResponse();
            #endregion
        }

        public XmlDocument Get_Jenkins_Job_Config(string jobName)
        {
            string jobUrl = serverbaseurl + "//view//IVC//job//" + jobName + "/config.xml";
            RestCall restCall = new RestCall() { Url = jobUrl };
            string output = restCall.Get();

            if (!string.IsNullOrEmpty(output))
            {
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(output);
                return xDoc;
            }
            else
            {
                return null;
            }
        }

        public void Update_Jenkins_Job_Config(string jobName, XmlDocument xDoc)
        {
            string jobUrl = serverbaseurl + "//view//IVC//job//" + jobName + "/config.xml";
            RestCall restCall = new RestCall() { Url = jobUrl };
            string output = restCall.Post(xDoc.OuterXml);
        }

        public void Update_Trigger_Time_JenkinsJob(string jobName, string timestring)
        {
            XmlDocument xDoc = Get_Jenkins_Job_Config(jobName);

            if (xDoc != null)
            {
                XmlNode targetNode = xDoc.SelectSingleNode("/project/triggers/hudson.triggers.TimerTrigger/spec");
                if (targetNode != null)
                {
                    targetNode.InnerText = timestring;
                    Update_Jenkins_Job_Config(jobName, xDoc);
                }
            }

            
        }
    }

}

