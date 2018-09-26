using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace ARIIVC.Scheduler
{
    [Verb("install-check", HelpText = "Check if release is installed")]
    public class InstallCheckOptions
    {
        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }
    }
    [Verb("check-releases", HelpText = "Check if a new release is created")]
    public class CheckforReleasesOptions
    {
        [Option("pack", Required = false, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = false, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }
    }
    [Verb("create-dashboard", HelpText = "Trigger the main process")]
    public class CreateDashboardOptions
    {
        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("modules", Required = true, HelpText = "Module names (comma separated list)")]
        public string Modules { get; set; }

        [Option("includerrt", Required = true, HelpText = "Flag for RRT scripts")]
        public string IncludeRrtScripts { get; set; }

        [Option("createwithoutjiraapi", Required = true, HelpText = "Create dashboard without Jira API")]
        public string CreateWithoutJiraApi { get; set; }
    }
    [Verb("reserve-slaves", HelpText = "Reserve slaves for test execution")]
    public class ReserveSlaveOptions
    {
        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

    }
    [Verb("get-tests", HelpText = "Perform install checks")]
    public class GetTestsOptions
    {
        [Option("pack", Required = true, HelpText = "Pack on which the installation check is to be run")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product on which the installation check is to be run")]
        public string Product { get; set; }

        [Option("modules", Required = true, HelpText = "Modules that are to be executed", Separator = ',')]
        public string Modules { get; set; }
    }    
    [Verb("scrum-automation-run", HelpText = "Perform install checks")]
    public class ScrumAutomationRunOptions
    {
        [Option("pack", Required = true, HelpText = "Pack on which the installation check is to be run")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product on which the installation check is to be run")]
        public string Product { get; set; }

        [Option("group", Required = true, HelpText = "Group under which tests were created in Dashboard Scheduler", Separator = ',')]
        public string Group { get; set; }

        [Option("name", Required = true, HelpText = "Name of the single scheduler block in a group", Separator = ',')]
        public string Name { get; set; }

        [Option("testset", Required = true, HelpText = "Test Set name for Dashboard Update", Separator = ',')]
        public string TestsetName { get; set; }
    }    
    [Verb("trigger-module", HelpText = "Trigger the tests for a specific module")]
    public class TriggerModuleOptions
    {
        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("module", Required = true, HelpText = "Module names (comma separated list)")]
        public string Module { get; set; }
    }
    [Verb("trigger-submodule", HelpText = "Trigger the tests for a specific module")]
    public class TriggerSubModuleOptions
    {
        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("submodule", Required = true, HelpText = "Module names (comma separated list)")]
        public string SubModule { get; set; }
    }
    [Verb("trigger-test", HelpText = "Trigger the tests")]
    public class TriggerTest
    {
        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("submodule", Required = true, HelpText = "Submodule names (comma separated list)")]
        public string SubModule { get; set; }

        [Option("testname", Required = true, HelpText = "Single runnable entity for Nunit")]
        public string Test { get; set; }
    }
    [Verb("trigger-nightly-regression", HelpText = "Trigger the execution of all the regression tests")]
    public class TriggerNightlyRegression
    {
        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }      
    }
    [Verb("trigger-ordered-submodules", HelpText = "Trigger the execution of submodules in an order")]
    public class TriggerOrderedSubmodules
    {
        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("order", Required = true, HelpText = "Order Set to pick from runconfig")]
        public int Order { get; set; }
    }
    [Verb("regression-run-test", HelpText = "Trigger single execution process")]
    public class RunSingleTest
    {
        [Option("testname", Required = true, HelpText = "test or comma seperated tests")]
        public string Test { get; set; }

        [Option("testsetname", Required = true, HelpText = "test set name for dashboard")]
        public string TestSetName { get; set; }

        [Option("appserver", Required = true, HelpText = "Drive Application Server to Run tests")]
        public string AppServer { get; set; }
    }
    [Verb("regression-run-sequential", HelpText = "Trigger single execution process")]
    public class RunSequentialTest
    {
        [Option("submodule", Required = true, HelpText = "name of the submodule that needs to run in sequential")]
        public string Test { get; set; }

        [Option("testsetname", Required = true, HelpText = "test set name for dashboard")]
        public string TestSetName { get; set; }

        [Option("appserver", Required = true, HelpText = "test set name for dashboard")]
        public string AppServer { get; set; }
    }
    [Verb("regression-set-labels", HelpText = "set the labels for all 47 machines")]
    public class RegressionSetLabels
    {

    }
    [Verb("pack-setup", HelpText = "Run the pack setup scripts on specified pack")]
    public class PackSetupOptions
    {
        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("category", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Category { get; set; }

        [Option("appserver", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Server { get; set; }
    }
    [Verb("single-bvt", HelpText = "Run the single pack setup script on specified app server")]
    public class RunSingleBvtOptions
    {
        [Option("testsetname", Required = true, HelpText = "test set name for dashboard")]
        public string TestSetName { get; set; }

        [Option("script", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Script { get; set; }

        [Option("appserver", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string AppServer { get; set; }
    }
    [Verb("site-status-update", HelpText = "Getting the status from Ring Master")]
    public class SiteStatusOptions
    {
        [Option("versionid", Required = true, HelpText = "version id of release")]
        public string versionId { get; set; }
    }
    [Verb("nightly-run", HelpText = "running the application nightly")]
    public class NightlyRunOptions
    {
        [Option("sitename", Required = true, HelpText = "sitename information")]
        public string sitename { get; set; }

        [Option("testsetname", Required = true, HelpText = "Testsetname information")]
        public string TestSetName { get; set; }

        [Option("module", Required = true, HelpText = "Testsetname information")]
        public string module { get; set; }

        [Option("region", Required = true, HelpText = "region information")]
        public string region { get; set; }

    }
    [Verb("releasetesting-dashboard", HelpText = "create releasetesting dashboard")]
    public class releasetestingdashboard
    {
        [Option("defaultversion", Required = true, HelpText = "version id information")]
        public string defaultversion { get; set; }
    }
    [Verb("execution-snapshot", HelpText = "Get Execution Snapshot")]
    public class ExecutionSnapshotOptions
    {
        [Option("pack", Required = true, HelpText = "Pack for which snapshot is taken")]
        public string Pack { get; set; }
    }
    [Verb("releasetesting", HelpText = "Release Testing End to End automation options")]
    public  class ReleaseTesting
    {
        //Dashboard required parameter
        [Option("defaultversion", Required = false, HelpText = "version id information")]
        public string DefaultVersion { get; set; }

        //sitestatus required parameter
        [Option("versionid", Required = false, HelpText = "version id of release")]
        public string VersionId { get; set; }

        //Nightly run requried parameter
        [Option("sitename", Required = false, HelpText = "sitename information")]
        public string SiteName { get; set; }

        [Option("testsetname", Required = false, HelpText = "Testsetname information")]
        public string TestsetName { get; set; }

        [Option("module", Required = false, HelpText = "Module information")]
        public string Module { get; set; }

        [Option("region", Required = false, HelpText = "region information")]
        public string region { get; set; }
    }
    [Verb("deploytests", HelpText = "To run test on single site")]
    public class RunDeploymentTestsOptions
    {
        [Option("host", Required = true, HelpText = "Host")]
        public string Host { get; set; }

        [Option("service", Required = true, HelpText = "Service")]
        public string Service { get; set; }

        [Option("envcode", Required = true, HelpText = "Environment Code")]
        public string EnvCode { get; set; }

        [Option("releaseversion", Required = true, HelpText = "Release Version")]
        public string ReleaseVersion { get; set; }

        [Option("updateid", Required = true, HelpText = "Update Id")]
        public string UpdateId { get; set; }

        [Option("mreposerverurl", Required = true, HelpText = "Mrepo Server Url")]
        public string MrepoServerUrl { get; set; }

        [Option("runuser", Required = true, HelpText = "Run User")]
        public string RunUser { get; set; }
    }
    [Verb("addbctstoreldashboard", HelpText = "To add tests")]
    public class AddBCTSToReleaseDashBoardOptions
    {
        [Option("repackname", Required = true, HelpText = "Pack Name")]
        public string PackName { get; set; }

        [Option("groupname", Required = true, HelpText = "Group Name")]
        public string GroupName { get; set; }

        [Option("customername", Required = true, HelpText = "Customer Name")]
        public string CustomerName { get; set; }

        [Option("label", Required = true, HelpText = "Label")]
        public string Label { get; set; }

        [Option("testsetname", Required = true, HelpText = "Test Set Name")]
        public string TestSetName { get; set; }
    }
    [Verb("infosys-smoke-trigger", HelpText = "Smoke test trigger for infosys site")]
    public class InfosysSmokeTrigger
    {
        [Option("server", Required = true, HelpText = "server details of the application")]
        public string Server { get; set; }

        [Option("service", Required = true, HelpText = "service details of the application")]
        public string Service { get; set; }

        [Option("rpmversion", Required = true, HelpText = "RPM version of the application")]
        public string RpmVersion { get; set; }

        [Option("envcode", Required = true, HelpText = "EnvironmentCode of the application")]
        public string EnvironmentCode { get; set; }

        [Option("portnumber", Required = true, HelpText = "port number of the application")]
        public string PortNumber { get; set; }

        [Option("username", Required = true, HelpText = "EnvironmentCode of the application")]
        public string UserName { get; set; }

        [Option("password", Required = true, HelpText = "EnvironmentCode of the application")]
        public string Password { get; set; }

    }
    [Verb("get-feature-info", HelpText = "Get the product feature info from the master pack")]
    public class GetProductFeatureOptions
    {
        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("scriptname", Required = true, HelpText = "script to get the product feature info")]
        public string Script { get; set; }
    }
    [Verb("get-frc", HelpText = "Get first run count")]
    public class GetFirstRunCountOptions
    {
        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("subscribers", Required = true, HelpText = "Mail Subscribers")]
        public string Subscribers { get; set; }
    }
    [Verb("mongodb-cleanup", HelpText = "Clean up MongoDB data")]
    public class MongoDbCleanUpOptions
    {
        [Option("day", Required = true, HelpText = "Day")]
        public string Day { get; set; }

        [Option("month", Required = true, HelpText = "Month")]
        public string Month { get; set; }

        [Option("year", Required = true, HelpText = "Year")]
        public string Year { get; set; }
    }
    [Verb("regression-sanity-checks", HelpText = "Perform sanity checks on IVC packs")]
    public class PerformSanityChecksOptions
    {
        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("systemversion", Required = true, HelpText = "System Version")]
        public string SystemVersion { get; set; }

        [Option("updateid", Required = true, HelpText = "Update ID")]
        public string UpdateId { get; set; }

        [Option("slave", Required = true, HelpText = "Slave server")]
        public string Slave { get; set; }
    }
    [Verb("zephyr-tests-puller", HelpText = "Pull all tests from zephyr")]
    public class ZephyrTestsPullerOptions
    {

    }

    [Verb("create-browser", HelpText = "Create browser dashboard")]
    public class CreateBrowserDashboardOptions
    {
        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("pack", Required = true, HelpText = "Pack name")]
        public string Pack { get; set; }
    }
}
