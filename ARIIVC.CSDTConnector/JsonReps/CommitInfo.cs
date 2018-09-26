using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.CSDTConnector.JsonReps
{

    public class CommitInfo
    {
        [JsonProperty("info")]
        public List<String> info { get; set; }
    }

}
