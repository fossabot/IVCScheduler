using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ARIIVC.Scheduler.JsonReps
{
    public class ProductFeature
    {
        [JsonProperty("productdescription")]
        public string ProductDescription { get; set; }

        [JsonProperty("drivemodule")]
        public string DriveModule { get; set; }

        [JsonProperty("enabled")]
        public string Enabled { get; set; }
    }
}
