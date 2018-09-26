using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace ARIIVC.PackRefresh
{
    [Verb("refresh-ivc-packs", HelpText = "Refresh Ivc Packs")]
    public class RefreshIvcPacks
    {
        [Option("pack", Required = true, HelpText = "Pack Name")]
        public string pack { get; set; }

        [Option("config", Required = true, HelpText = "Pack Configuration")]
        public string config { get; set; }
    }

    [Verb("refresh-slave", HelpText = "Refresh a slave pack")]
    public class RefreshASlavePack
    {
        [Option("server", Required = true, HelpText = "slave pack server")]
        public string server { get; set; }
    }
}
