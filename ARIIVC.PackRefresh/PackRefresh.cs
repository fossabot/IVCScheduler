using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Utilities;
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
using System.IO;
using CommandLine;




namespace ARIIVC.PackRefresh
{
    public class PackRefresh
    {
        static string authToken = "";
        static string baseUrl = "http://gbh-int-a-01-mgmt.oraclei.cdk.com/api";
        static int Main(string[] args)
        {
            try
            {
                return Parser.Default.ParseArguments<RefreshIvcPacks, RefreshASlavePack>(args).MapResult((RefreshIvcPacks opts) => IVCPackRefresh(opts), (RefreshASlavePack opts) => RefreshASlavePack(opts), errs => 1);
            }
            catch (Exception eeObj)
            {
                Console.WriteLine("Exception in Scheduler program : {0}", eeObj.StackTrace);
                return 1;
            }

            
        }
        public static int IVCPackRefresh(RefreshIvcPacks opts)
        {

            CreateAuthenticationToken();
            Console.WriteLine("Authentication Toekn is : " + authToken);
           
            List<ivc_pack_db_details> dbDetails = JsonConvert.DeserializeObject<List<ivc_pack_db_details>>(File.ReadAllText("IvcPackDbDetails.json"));
            string instance = "GBINTN01AP";
            string schemaName = "DRIVE";
            string sourcePdbName, sourceMachine, sourceServiceName;
            string destPdbName, destMachine, destServiceName;

            foreach (ivc_pack_db_details dbDetail in dbDetails)
            {
                if (dbDetail.pack.Contains(opts.pack) && dbDetail.config.Contains(opts.config))
                {

                    sourcePdbName = dbDetail.master.pdbname;
                    sourceMachine = dbDetail.master.server;
                    sourceServiceName = dbDetail.master.servicename;

                    foreach (slavedetails slaveDetail in dbDetail.workers)
                    {

                        destPdbName = slaveDetail.pdbname;
                        destMachine = slaveDetail.server;
                        destServiceName = slaveDetail.servicename;

                        SnapClone(instance, sourcePdbName, destPdbName);
                        CreateACL(destPdbName, destMachine, schemaName, destServiceName);
                        DeleteACL(destPdbName, sourceMachine, schemaName, sourceServiceName);
                    }

                }
            }

            return 0;
        }

        public static void RefreshPDB(string instance, string sourcePdbName, string destPdbName)
        {

            Dictionary<string, string> bodydata = new Dictionary<string, string>();
            bodydata.Add("srcInstance", instance);
            bodydata.Add("destInstance", instance);
            bodydata.Add("srcPdbName", sourcePdbName);
            bodydata.Add("destPdbName", destPdbName);
            RestCall rest = new RestCall();
            rest.Url = "gbh-int-a-01-mgmt.oraclei.cdk.com/api/refresh";
            var body = JsonConvert.SerializeObject(bodydata);
            rest.Post(body, authToken);

        }

        public static void SnapClone(string instance, string sourcePdbName, string destPdbName)
        {
            Dictionary<string, string> bodydata = new Dictionary<string, string>();
            bodydata.Add("instance", instance);
            bodydata.Add("srcPdbName", sourcePdbName);
            bodydata.Add("destPdbName", destPdbName);
            RestCall rest = new RestCall();
            rest.Url = baseUrl + "/snapclone";
            var body = JsonConvert.SerializeObject(bodydata);
            string postResponse = rest.Post(body, authToken);


            Console.WriteLine(string.Format("Snapclone for {0} : {1} : {2}", sourcePdbName, destPdbName, postResponse));


        }

        public static void CreateACL(string destPdbName, string machine, string schemaName, string serviceName)
        {
            Dictionary<string, string> bodydata = new Dictionary<string, string>();
            bodydata.Add("machine", machine);
            bodydata.Add("schemaName", schemaName);
            bodydata.Add("serviceName", serviceName);
            bodydata.Add("backupPolicy", "21");
            bodydata.Add("locked", "N");
            RestCall rest = new RestCall();
            rest.Url = baseUrl + "/acl/" + destPdbName;
            var body = JsonConvert.SerializeObject(bodydata);
            string postResponse = rest.Post(body, authToken);

            Console.WriteLine(string.Format("Create ACL for {0} : {1} : {2} : {3}", machine, serviceName, schemaName, postResponse));
        }

        public static void DeleteACL(string destPdbName, string machine, string schemaName, string serviceName)
        {
            Dictionary<string, string> bodydata = new Dictionary<string, string>();
            bodydata.Add("machine", machine);
            bodydata.Add("schemaName", schemaName);
            bodydata.Add("serviceName", serviceName);          
            RestCall rest = new RestCall();
            rest.Url = baseUrl + "/acl/" + destPdbName;
            var body = JsonConvert.SerializeObject(bodydata);
            string postResponse = rest.Delete(body, authToken);

            Console.WriteLine(string.Format("Delete ACL for {0} : {1} : {2} : {3}", machine, serviceName, schemaName, postResponse));
        }

        public static void CreateAuthenticationToken()
        {
            string authenticationUrl = baseUrl + "/authenticate";
            Dictionary<string, string> authenticationBody = new Dictionary<string, string>();
            authenticationBody.Add("userId", "gb-ivc-api");
            authenticationBody.Add("passwd", "murakami99");
            RestCall rc = new RestCall() { Url = authenticationUrl };
            
            string postResponse = rc.Post(JsonConvert.SerializeObject(authenticationBody));
            var resp = JsonConvert.DeserializeObject<Dictionary<object, object>>(postResponse);
            authToken = resp["token"].ToString();

        }

        public static int RefreshASlavePack(RefreshASlavePack opts)
        {

            CreateAuthenticationToken();
            Console.WriteLine("Authentication Token is : " + authToken);

            List<ivc_pack_db_details> dbDetails = JsonConvert.DeserializeObject<List<ivc_pack_db_details>>(File.ReadAllText("IvcPackDbDetails.json"));
            string instance = "GBINTN01AP";
            string schemaName = "DRIVE";
            string sourcePdbName, sourceMachine, sourceServiceName;
            string destPdbName, destMachine, destServiceName;

            foreach (ivc_pack_db_details dbDetail in dbDetails)
            {
                sourcePdbName = dbDetail.master.pdbname;
                sourceMachine = dbDetail.master.server;
                sourceServiceName = dbDetail.master.servicename;

                foreach (slavedetails slaveDetail in dbDetail.workers)
                {
                    if ( opts.server.Contains(slaveDetail.server))
                    {
                        destPdbName = slaveDetail.pdbname;
                        destMachine = slaveDetail.server;
                        destServiceName = slaveDetail.servicename;

                        SnapClone(instance, sourcePdbName, destPdbName);
                        CreateACL(destPdbName, destMachine, schemaName, destServiceName);
                        DeleteACL(destPdbName, sourceMachine, schemaName, sourceServiceName);
                    }
                }
            }

            return 0;
        }

    }


}
