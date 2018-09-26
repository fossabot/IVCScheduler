using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace ARIIVC.Logger
{
    public class Dashboard
    {
        public string XmlPath { get; set; }
        public string TestFixtureName { get; set; }
        public string Testname { get; set; }
        public string Att { get; set; }
        public string AttVal { get; set; }
        public string TestFixtureSetupDuration { get; set; }
        public string Duration { get; set; }
        public string Hostname { get; set; }
        public string Author { get; set; }
        public string Runner { get; set; }
        public string Logxml { get; set; }
        public string Mongorestapi { get; set; }
        public string testStartTime { get; set; }
        public string testEndTime { get; set; }

        private readonly bool _shouldUpdateDashboard;

        public Dashboard()
        {
            _shouldUpdateDashboard =
                Convert.ToBoolean(ConfigurationManager.AppSettings["updatedashboard"]);
        }

        public void Update()
        {
            Update(false);
        }

        public void Update(bool updateOnlyIfNoRun)
        {
            if (!_shouldUpdateDashboard)
            {
                Logger.Instance.Debug("Update of Dashboard is configured as False, Exiting");
                return;
            }
            List<IvcTestResult> currentTests = GetResutlsFromMongoDb();
            if (currentTests == null || currentTests.Count == 0)
            {
                Logger.Instance.Debug("Dashboard does not contain the current test, Exiting");
                return;
            }

            foreach (var test in currentTests)
            {
                if (updateOnlyIfNoRun && test.Status.ToLower() != "no run")
                {
                    return;
                } 
            }

            if (currentTests.Count > 1)
            {
                UpdateMongoDbWithMultipleResults(currentTests.First().Counter);
            }
            else
            {
                UpdateMongoDbWithResult(currentTests.First().Counter);
            }

        }

        private Dictionary<string, string> Query()
        {
            Dictionary<string, string> query = new Dictionary<string, string>()
            {
                {"packname", XmlPath.Split('_')[1].Split('-')[0]},
                {"testsetname", XmlPath.Split('.')[0]},
                {"name", Testname}
            };
            return query;
        }

        private List<IvcTestResult> GetResutlsFromMongoDb()
        {
            Dictionary<string, string> query = Query();
            string geturl = string.Format("{0}/results/get?query={1}", Mongorestapi, JsonConvert.SerializeObject(query));
            string jsonoutput = GetMongo(geturl);
            List<IvcTestResult> results = JsonConvert.DeserializeObject<List<IvcTestResult>>(jsonoutput);
            if (results.Count > 0)
            {
                if (AttVal.ToLower().Equals("failed"))
                {
                    foreach (var result in results)
                    {
                        result.Counter++;
                    }                    
                }
            }
            return results;
        }

        private void UpdateMongoDbWithResult(int failedCount)
        {
            Dictionary<string, string> query = Query();
            Dictionary<string, string> putMongo = new Dictionary<string, string>()
            {
                {Att, AttVal},
                {"testfixturename", TestFixtureName },
                {"testfixturesetupduration", TestFixtureSetupDuration },
                {"duration", Duration},
                {"host", Hostname},
                {"author", Author},
                {"runner", Runner},
                {"logs", Logxml},
                {"counter", Convert.ToString(failedCount)},
                {"teststarttime", testStartTime},
                {"testendtime", testEndTime}
            };
            Dictionary<string, string> jsonData = new Dictionary<string, string>();
            jsonData.Add("query", JsonConvert.SerializeObject(query));
            jsonData.Add("data", JsonConvert.SerializeObject(putMongo));
            string url = string.Format("{0}/results/update", Mongorestapi);
            this.PutMongoDb(url, JsonConvert.SerializeObject(jsonData));
        }

        private void UpdateMongoDbWithMultipleResults(int failedCount)
        {
            string _mongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
            IMongoClient _client = new MongoClient(_mongoUrl);
            IMongoDatabase _database = _client.GetDatabase("ivc");
            IMongoCollection<IvcTestResult> _results = _database.GetCollection<IvcTestResult>("results");

            string packName = XmlPath.Split('_')[1].Split('-')[0];

            var filterData = Builders<IvcTestResult>.Filter.Where(t =>
                t.Packname == packName && t.Testsetname == XmlPath && t.Name == Testname);

            var updateData = Builders<IvcTestResult>.Update.Set(Att, AttVal).Set("testfixturename", TestFixtureName)
                .Set("testfixturesetupduration", TestFixtureSetupDuration).Set("duration", Duration)
                .Set("host", Hostname).Set("author", Author).Set("runner", Runner).Set("logs", Logxml)
                .Set("counter", Convert.ToString(failedCount)).Set("teststarttime", testStartTime)
                .Set("testendtime", testEndTime);

            _results.UpdateMany(filterData, updateData);
        }

        protected string GetMongo(string url)
        {
            Logger.Instance.Debug(String.Format("Get operation on {0}", url));
            HttpWebRequest relRequest = WebRequest.Create(url) as HttpWebRequest;
            relRequest.Method = "GET";
            HttpWebResponse relResponse = relRequest.GetResponse() as HttpWebResponse;
            StreamReader relStream = new StreamReader(relResponse.GetResponseStream());
            string relOutput = relStream.ReadToEnd();
            relStream.Close();
            relResponse.Close();
            return relOutput;
        }

        protected string PutMongoDb(string url, string data)
        {
            string backstr = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                byte[] requestBytes = System.Text.Encoding.UTF8.GetBytes(data);
                req.Method = "PUT";
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
                Logger.Instance.Debug(string.Format("Cannot add - {0}", data), postExcep);
                return backstr;
            }
        }

        public string PostMongo(string url, string data)
        {
            string backstr = "";
            try
            {
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
                Logger.Instance.Debug(string.Format("Cannot add - {0}", data), postExcep);
                return backstr;
            }
        }
    }
}
