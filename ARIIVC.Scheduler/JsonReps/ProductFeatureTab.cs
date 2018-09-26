using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    public class ProductFeatureTab
    {
        public List<ProductFeature> All { get; set; }

        public List<ProductFeature> Available { get; set; }

        public List<ProductFeature> New { get; set; }

        public List<ProductFeature> Pilot { get; set; }
    }
}
