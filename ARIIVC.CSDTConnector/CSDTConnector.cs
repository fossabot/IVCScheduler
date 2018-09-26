using ARIIVC.CSDTConnector.JsonReps;
using ARIIVC.Utilities;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Text.RegularExpressions;
using ARIIVC.Scheduler;
using System.Diagnostics;
using System.Xml;
using ARIIVC.Scheduler.JsonReps;
using System.Threading;
using ARIIVC.Utilities.JsonRepo;

namespace ARIIVC.CSDTConnector
{
    public class CSDTConnector
    {
        public static string deploymentserver = "gbhsremrepo01.gbh.dsi.adp.com";
        public static string csdtRest = "http://" + deploymentserver + "/csdt/rest";
        public static string mongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
        public const string _ivc_repo_id = "dev";
        public const string _ivc_package_type = "bin";

        public string GetLatestAvailablePackage(string repoID, string RPMType)
        {
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            queryParams.Add("_repoid", repoID);
            queryParams.Add("type", RPMType);
            string arg = JsonConvert.SerializeObject(queryParams);
            var url = string.Format("{0}/packages/latest_package?query={1}", csdtRest, arg);
            RestCall call = new RestCall { Url = url };
            string jsonoutput = call.Get();
            PackageInformation version = JsonConvert.DeserializeObject<PackageInformation>(jsonoutput);
            return version.version;
        }

        public List<IVCHostInformation> GetIVCHostDetails()
        {

            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            queryParams.Add("environment", "IVC");
            string arg = JsonConvert.SerializeObject(queryParams);
            var url = string.Format("{0}/hosts/get?query={1}", csdtRest, arg);
            RestCall call = new RestCall { Url = url };
            string jsonoutput = call.Get();
            List<IVCHostInformation> ivcHosts = JsonConvert.DeserializeObject<List<IVCHostInformation>>(jsonoutput);
            return ivcHosts;
        }

        public static List<string> CommitList(int versionBuildFrom, int versionBuildTo)
        {
            List<string> commit_ids = new List<string>();
            var csdt_url = "http://gbhsremrepo01.gbh.dsi.adp.com/csdt/rest/builds/";
            for (int i = versionBuildFrom; i <= versionBuildTo; i++)
            {
                Console.WriteLine("****************Extracting commits for: " + i + "**************");
                var url = csdt_url + "package_changelog?query={ \"version\":\"dev-" + i + "\"}";
                RestCall call = new RestCall() { Url = url };
                string jsonoutput = call.Get();
                CommitInfo commit = JsonConvert.DeserializeObject<CommitInfo>(jsonoutput);
                foreach (var commitmessage in commit.info)
                {
                    if (commitmessage.Contains("IDRIVE") || commitmessage.Contains("REV8") || commitmessage.Contains("OTHER"))
                    {
                        string com_msg = commitmessage.Split(' ')[1];
                        com_msg = Regex.Replace(com_msg, "[^\\w\\._]", "");
                        commit_ids.Add(com_msg);
                    }
                }
            }
            return commit_ids;
        }
     
        public List<IVCHostDeployementProgress> CheckDeployementStatus(List<IVCHostInformation> relevantHosts)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<string> hosts = relevantHosts.Select(tt => tt.ip_addr).ToList();
            
            string deployProgressUrl = csdtRest + "/hosts/recent_deployment_status";
            RestCall restCall = new RestCall() { Url = deployProgressUrl };
            string jsonoutput = restCall.Get();

            List<IVCHostDeployementProgress> updates = JsonConvert.DeserializeObject<List<IVCHostDeployementProgress>>(jsonoutput);
            List<IVCHostDeployementProgress>  relevantProgress = updates.FindAll(yy => hosts.Contains(yy.ip_addr) && yy.completed);

            var successInstall = relevantProgress.FindAll(pp => !(string.Join(":", pp.status).ToLower().Contains("skipped") || string.Join(":", pp.status).ToLower().Contains("skipped")));

            while ((successInstall.Count != relevantHosts.Count || sw.Elapsed.Minutes < 15))
            {
                Thread.Sleep(60000);
                jsonoutput = restCall.Get();

                updates = JsonConvert.DeserializeObject<List<IVCHostDeployementProgress>>(jsonoutput);
                relevantProgress = updates.FindAll(yy => hosts.Contains(yy.ip_addr) && yy.completed);
                successInstall = relevantProgress.FindAll(pp => !(string.Join(":", pp.status).ToLower().Contains("skipped") || string.Join(":", pp.status).ToLower().Contains("skipped")));

            }
            return successInstall;
        }

        public List<IVCHostInformation> GetActiveIVCHostDetails(string pack)
        {
            DashboardConnector dc = new DashboardConnector();
            List<IVCHostInformation> hosts = new List<IVCHostInformation>();
            List<ivc_appserver> allAppServers = dc.GetAllAppServersForPackSetup(pack);            
            List<IVCHostInformation> allhosts = GetIVCHostDetails();
            hosts = allhosts.Where(t => allAppServers.Any(t2 => t2.hostname == t.name)).ToList();
            return hosts;
            //This will return active hosts for a given pack
        }

        public static string GetInstalledRpmNumber(string hostName)
        {
            var url = csdtRest + "hosts/get_build_info?query={ \"ip_addr\":" + "\"" + hostName + "\"" + "," + "\"path\":" + "\"" + "/user1/live/" + "\"" + "}";
            Console.WriteLine("****************Extracting commits for: " + hostName + "**************");
            RestCall call = new RestCall() { Url = url };
            string xmlresponse = call.Get();
            XmlDocument responseXML = new XmlDocument();
            responseXML.LoadXml(xmlresponse);
            string buildVersion = responseXML.SelectSingleNode("//BuildInfo//System").InnerText;
            return buildVersion;
        }

        public void DeployLatestPackage(string pack)
        {
            DashboardConnector connector = new DashboardConnector();

            //get latest RPM version
            string rpmVersion = GetLatestAvailablePackage(_ivc_repo_id, _ivc_package_type);

            //get the host details
            List<IVCHostInformation> activehosts = GetActiveIVCHostDetails(pack);

            //Deploy the latest package
            DeployPackage(pack, rpmVersion);

            //check the deployement status
            List<IVCHostDeployementProgress> deployStatus = CheckDeployementStatus(activehosts);

            //connect to mongo and update the servers
            MongoConnector mg = new MongoConnector(database: "accelerator");
            mg.UpdateAppServers(deployStatus);

            string lastsucessfulbuild = connector.GetLastInstalledRpmNumber(pack);
            AcceleratorMongo ac = new AcceleratorMongo();
            ac.UpdateBuildVersion(Convert.ToInt32(lastsucessfulbuild.Split('-')[1]), Convert.ToInt32(rpmVersion.Split('-')[1]));
        }

        public void DeployPackage(string pack, string rpmversion)
        { 

            List<IVCHostInformation> hostinfo = GetActiveIVCHostDetails(pack);

            foreach (var host in hostinfo)
            {

                if ( host.ip_addr != "100.124.198.84" || host.ip_addr != "100.124.198.83")
                {
                    DeployPackageInfo detemp = new DeployPackageInfo();
                    detemp.ip_addr = host.ip_addr;
                    detemp.region = host._region_id;
                    detemp.environment = "IVC";
                    detemp.service = host.service;
                    detemp.version = rpmversion;
                    detemp.repo = host._repoid;
                    detemp.username = "AGENT";
                    detemp.password = "";
                    detemp.schema_refresh = "No";
                    detemp.ivc_deploy_env = "IVC";
                    detemp.package_type = _ivc_package_type;
                   
                    RestCall call = new RestCall()
                    {
                        Url = string.Format("http://gbhsremrepo01.gbh.dsi.adp.com/csdt/rest/hosts/deploy_package")
                    };
                    call.PostCSDT(JsonConvert.SerializeObject(detemp));
                }
            }
        }

        public List<string> GetRingUpdateIssues(string packname, string updateId)
        {
            MongoDriver driver = new MongoDriver();
            ivc_pack_details targetPack = driver.Releases.GetPackInformation(packname);

            List<string> jiraIssues = new List<string>();
            string url = "http://releng/rediary/dynamic/getReleaseQA.php?url=" + targetPack.RingUrl + "&updateid=" + updateId;
            RestCall rest = new RestCall()
            {
                Url = url
            };

            string output = rest.Get();
            var lo = JObject.Parse(output);
            var th = lo.ToJson();
            List<JToken> allTokens = lo.Root.Children().ToList();

            foreach (JProperty property in lo.Properties())
            {
                var currentRecord = JsonConvert.DeserializeObject<RingReleaseInformation>(property.Value.ToString());
                var issue = currentRecord.ISSUEDETAILS.Replace("J_", string.Empty).Replace("/", "-");
                jiraIssues.Add(issue);
            }
            return jiraIssues;
        }

       

        [Test]
        public void tstGetLatestAvailablePackage()
        {
            DeployLatestPackage("MT");
        }

    }
}
