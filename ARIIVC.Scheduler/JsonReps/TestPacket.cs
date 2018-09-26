using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.JsonReps
{
    public class testpacket
    {
        public string suitename;
        public string submodule;
        public int counter;
        public int duration;
        public testpacket(string suite, string sub, int count, int dur)
        {
            suitename = suite;
            submodule = sub;
            counter = count;
            duration = dur;

        }
    }

}
