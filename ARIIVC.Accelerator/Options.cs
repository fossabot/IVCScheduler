using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace ARIIVC.Accelerator
{
    [Verb("update-impacted-tests", HelpText = "Check if release is installed")]
    public class UpdateImpactedTests
    {
        [Option("hash", Required = true, HelpText = "Hash of the commit made to the branch")]
        public string Hash { get; set; }

        [Option("version", Required = true, HelpText = "Code version to fetch the relevant impacted tests")]
        public string CodeVersion { get; set; }
    }

    [Verb("preprod-automation", HelpText = "Perform preprod automation")]
    public class ScrumAutomationOptions
    {
        [Option("pack", Required = true, HelpText = "Pack on which the installation check is to be run")]
        public string Pack { get; set; }

        [Option("product", Required = true, HelpText = "Product on which the installation check is to be run")]
        public string Product { get; set; }

        [Option("group", Required = true, HelpText = "Group under which tests were created in Dashboard Scheduler", Separator = ',')]
        public string Group { get; set; }
    }

    [Verb("trigger-single-test", HelpText = "Trigger single tests from the scheduled ones")]
    public class TriggerSingleTest
    {
        [Option("pack", Required = true, HelpText = "Pack on which the installation check is to be run")]
        public string Pack { get; set; }

        [Option("group", Required = true, HelpText = "Group under which tests were created in Dashboard Scheduler", Separator = ',')]
        public string Group { get; set; }

        [Option("testset", Required = true, HelpText = "Test Set name for Dashboard Update", Separator = ',')]
        public string TestsetName { get; set; }

        [Option("server", Required = true, HelpText = "Application server to run tests against")]
        public string AppServer { get; set; }

        [Option("testname", Required = true, HelpText = "Name of the single scheduler block in a group", Separator = ',')]
        public string TestName { get; set; }

        [Option("subscribers", Required = true, HelpText = "Who is intrested in the mails", Separator = ',')]
        public string Subscribers { get; set; }
    }

    [Verb("process-workflows", HelpText = "Process workflows that are not yet processed")]
    class ProcessWorkflowsOptions
    {
        [Option("product", Required = true, HelpText = "Product")]
        public string Product { get; set; }

        //[Option("dir", Required = true, HelpText = "Directory where files are present")]
        //public bool Dir { get; set; }

        [Option("version", Required = true, HelpText = "mt / live / pilot")]
        public string Version { get; set; }


    }

    [Verb("update-workflows", HelpText = "update workflows that are not yet processed")]
    class UpdateWorkFlows
    {
        [Option("product", Required = true, HelpText = "Product")]
        public string Product { get; set; }

        [Option("filename", Required = true, HelpText = "Filename to be processed")]
        public string FileName { get; set; }

        [Option("version", Required = true, HelpText = "mt / live / pilot")]
        public string Version { get; set; }


    }
    [Verb("process-test", HelpText = "process single test for accelerator")]
    class ProcessSingleTest
    {
        [Option("product", Required = true, HelpText = "Product")]
        public string Product { get; set; }

        [Option("filename", Required = true, HelpText = "Filename to be processed")]
        public string FileName { get; set; }

        [Option("version", Required = true, HelpText = "mt / live / pilot")]
        public string Version { get; set; }

        [Option("teststatus", Required = true, HelpText = "TRUE / FALSE")]
        public string TestStatus { get; set; }

    }

    [Verb("create-dashboard-profiler", HelpText = "Create dashboard for profiler")]
    public class CreateDashboardProfilerOptions
    {

        [Option("pack", Required = true, HelpText = "whicch pack to create tests for")]
        public string Pack { get; set; }

        [Option("createwithoutjiraapi", Required = true, HelpText = "Create dashboard without Jira API")]
        public string CreateWithoutJiraApi { get; set; }

        [Option("addoldtests", Required = true, HelpText = "Add tests from last dashboard")]
        public string AddOldTests { get; set; }
    }

    [Verb("create-dashboard-dmslite", HelpText = "Create dashboard for DMS Lite")]
    public class CreateDashboardDMSLiteOptions 
    {

        [Option("pack", Required = true, HelpText = "whicch pack to create tests for")]
        public string Pack { get; set; }

        [Option("createwithoutjiraapi", Required = true, HelpText = "Create dashboard without Jira API")]
        public string CreateWithoutJiraApi { get; set; }
        
    }

    [Verb("create-dashboard-rr", HelpText = "Create dashboard for profiler")]
    public class CreateDashboardRingReleaseOptions
    {
        [Option("product", Required = true, HelpText = "whicch product to create tests for")]
        public string Product { get; set; }

        [Option("pack", Required = true, HelpText = "whicch pack to create tests for")]
        public string Pack { get; set; }

    }
    [Verb("process-testset", HelpText = "Moves given testset to processed in workflows")]
    public class MoveTestsettoProcessed
    {
        [Option("testsetname", Required = true, HelpText = "Test set name")]
        public string Testsetname { get; set; }

        [Option("updateid", Required = true, HelpText = "Update ID ex: RUK176R0000001")]
        public string UpdateID { get; set; }
    }
    [Verb("preprod-create-dashboard", HelpText = "Create dashboard for preprod profiler")]
    public class PreProdProfilerCreateDashboardOptions
    {

    }

    [Verb("profiler-trigger-tests", HelpText = "Trigger profiler tests")]
    public class ProfilerTriggerTestsOptions
    {

    }

    [Verb("profiler-single-test", HelpText = "Trigger single execution process")]
    public class ProfilerSingleTestOptions
    {
        [Option("testname", Required = true, HelpText = "test or comma seperated tests")]
        public string Test { get; set; }

        [Option("testsetname", Required = true, HelpText = "test set name for dashboard")]
        public string TestSetName { get; set; }

        [Option("appserver", Required = true, HelpText = "Drive Application Server to Run tests")]
        public string AppServer { get; set; }
    }
    [Verb("workflows-codecut", HelpText = "Change the version for code cut")]
    public class WorkFlowsChangeVersion
    {
       
    }
    [Verb("profiler-notupdated-tests", HelpText = "Mail Tests not updated with profiler")]
    public class TestsnotUpdatedwithProfiler
    {
        [Option("version", Required = true, HelpText = "MT / Live / Pilot")]
        public string Version { get; set; }

        [Option("subscribers", Required = true, HelpText = "Mail Subscribers")]
        public string Subscribers { get; set; }
    }
    [Verb("profiler-ringreleases-getIssues", HelpText = "Get list of issues for a ring release")]
    public class GetIssuesList
    {
        [Option("Pack", Required = true, HelpText = "MT / Live / Pilot")]
        public string Pack { get; set; }

        [Option("UpdateId", Required = true, HelpText = "Update Id for a release")]
        public string updateId { get; set; }
    }
}
