using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Scheduler.Tests
{
    [TestFixture]
    public class LoggerTests
    {
        [Test]
        public void TestLogging()
        {
            SchedulerLogger.Log("18-07-2017_MT-044127AM", "Logging a message 2");
        }
    }
}
