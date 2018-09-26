using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    public class jenkinsNodes
    {
        public List<jenkinsNode> computer;
        public jenkinsNodes()
        {
            computer = new List<jenkinsNode>();
        }
    }

}
