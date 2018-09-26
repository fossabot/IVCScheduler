using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ARIIVC.Logger;
using ARIIVC.Utilities;
using ARIIVC.Scheduler;
using ARIIVC.Scheduler.JsonReps;
using ARIIVC.CSDTConnector;
using ARIIVC.CSDTConnector.JsonReps;
using CommandLine.Text;
using CommandLine;
using ARIIVC.Utilities.JsonRepo;

namespace ARIIVC.Regression
{
    class RegressionManager
    {
        static int Main(string[] args)
        {
            try
            {
                return Parser.Default.ParseArguments<TriggerPackSetup, RingReleaseImpactedTests>(args)
                    .MapResult(
                        (TriggerPackSetup opts) => Trigger_Pack_Setup(opts),
                        (RingReleaseImpactedTests opts) => Create_Impacted_Tests_RingRelease(opts),
                        errs => 1);
            }
            catch (Exception eeObj)
            {
                Console.WriteLine("Exception in Scheduler program : {0}", eeObj.Message);
                Console.WriteLine("Stack Trace  : {0}", eeObj.StackTrace);
                return 1;
            }
        }

        public static void ScheduleREJenkinsJob(string packname, string jobname, string timedelayinSecs, bool triggerInstall)
        {
            Jenkins jk = new Jenkins(GlobalConstants.RE_JENKINS_URL);
            switch (packname.ToLower())
            {
                case "prev":
                case "live":

                    break;

                case "pilot":
                case "ltst":

                    break;

            }
            Dictionary<string, string> JobArguments = new Dictionary<string, string>();
            JobArguments.Add("triggerinstall", triggerInstall.ToString());

            jk.TriggerJobWithDelay(GlobalConstants.PREV_DB_REFRESH_JOB, "900", JobArguments);
        }

        public static void CreateProfilerDataForRRStory(string story, string version,string product=null,string pack=null)
        {
            Jenkins jenkins = new Jenkins();
            Dictionary<string, string> arguments = new Dictionary<string, string>();
            arguments.Add("issue", story);
            arguments.Add("version", version);
            arguments.Add("product", product);
            arguments.Add("pack", pack);

            jenkins.TriggerJobWithDelay(GlobalConstants.PROFILERJOB_FOR_STORY, "15", arguments);
        }

        public static void rm_check_if_regression_complete()
        {
            DashboardConnector dc = new DashboardConnector();
            MongoDriver mongoDriver = new MongoDriver();
            string testsetName = mongoDriver.Releases.GetTestSetName("Drive", "Live");

            ivc_assoc_runs targetRun = mongoDriver.AssociatedRuns.GetAssociateRun(testsetName);

            ivc_trigger_info tc = new ivc_trigger_info();
            tc.RingRelease = targetRun.updateid;
            tc.RunCompleted = targetRun.time_mail;
            tc.SystemVersion = targetRun.systemversion;
            tc.PackName = targetRun.packname;
            tc.Date = DateTime.Now.ToShortDateString();

            if (targetRun.status.ToLower() == "completed")
            {
                DateTime patchInstallTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 50, 15, 00);
                DateTime currentTime = DateTime.Now;
                int currentHour = currentTime.Hour;
                if (currentHour <= 17)
                {
                    if (currentHour + 7 >= 24)
                        patchInstallTime = DateTime.Now.AddHours(7);

                    tc.DBRefresh = currentTime.AddMinutes(15).ToShortDateString() + currentTime.AddMinutes(15).ToShortTimeString();
                    tc.PackSetup = "Not Applicable";
                    tc.PatchInstall = patchInstallTime.ToShortDateString() + patchInstallTime.ToShortTimeString();
                }
                else
                {
                    tc.DBRefresh = "Not Applicable";
                    tc.PatchInstall = patchInstallTime.ToShortDateString() + patchInstallTime.ToShortTimeString();
                    tc.PackSetup = currentTime.AddMinutes(15).ToShortDateString() + currentTime.AddMinutes(15).ToShortTimeString();
                }

            }

            mongoDriver.TriggerInfo.AddRecord(tc);            
        }

        public static int Trigger_Pack_Setup(TriggerPackSetup opts)
        {
            //get the app servers
            DashboardConnector connector = new DashboardConnector();
            Jenkins jenkins = new Jenkins();
            List<ivc_appserver> appservers = connector.GetAllAppServersForPackSetup(opts.Product,opts.Pack);
            foreach (ivc_appserver tmpServer in appservers)
            {
                if (tmpServer.runconfig.ToLower() == "core" || tmpServer.runconfig.ToLower() == "ia")
                {
                    string category = "BVTIA";
                    if (tmpServer.runconfig.ToLower() == "core")
                    {
                        category = "BVTSA";
                    }

                    Dictionary<string, string> arguments = new Dictionary<string, string>();
                    arguments.Add("product", opts.Product);
                    arguments.Add("pack", opts.Pack);
                    arguments.Add("appserver", tmpServer.hostname);
                    arguments.Add("category", category);
                    jenkins.TriggerJob("REGRESSION_TRIGGER_PACK_SETUP", arguments);
                }
            }
            return 0;
        }

        public static int Create_Impacted_Tests_RingRelease(RingReleaseImpactedTests opts)
        {
            CSDTConnector.CSDTConnector csdt = new CSDTConnector.CSDTConnector();
            DashboardConnector dc = new DashboardConnector();
            ivc_recent_releases packRelease = dc.GetRecentReleaseInfoForProductPackRelid(opts.Product, opts.Pack,opts.RRUpdateId);

            List<string> RrIssues = csdt.GetRingUpdateIssues(opts.Pack, opts.RRUpdateId);
            CreateProfilerDataForRRStory(string.Join(",", RrIssues.Distinct().ToList()), packRelease.updateid,opts.Product,opts.Pack);

            return 0;
        }


    }
}
