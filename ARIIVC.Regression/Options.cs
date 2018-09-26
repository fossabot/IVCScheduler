using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace ARIIVC.Regression
{
    [Verb("trigger-packsetup", HelpText = "Trigger Pack Setup on a Pack")]
    public class TriggerPackSetup
    {
        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }        
    }

    [Verb("rr-impact", HelpText = "Get Impacted tests for a ring releases")]
    public class RingReleaseImpactedTests
    {
        [Option("product", Required = true, HelpText = "Product name (Drive / Rev8)")]
        public string Product { get; set; }

        [Option("pack", Required = true, HelpText = "Pack name (Live / Pilot / MT)")]
        public string Pack { get; set; }

        [Option("rrupdateid", Required = true, HelpText = "Ring Release Update ID")]
        public string RRUpdateId { get; set; }
    }
}
