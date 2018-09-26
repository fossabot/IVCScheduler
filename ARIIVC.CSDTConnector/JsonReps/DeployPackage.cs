using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.CSDTConnector.JsonReps
{
    public class DeployPackageInfo
    {
        public string ip_addr { get; set; }
        public string region { get; set; }
        public string environment { get; set; }
        public string service { get; set; }
        public string version { get; set; }
        public string repo { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string schema_refresh { get; set; }
        public string ivc_deploy_env { get; set; }
        public string package_type { get; set; }

    }
}

