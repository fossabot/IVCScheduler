using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ARIIVC.Scheduler.Tests
{
    [TestFixture]
    public class NotificationTests
    {
        [Test]
        public void TestEmail()
        {
            MailTestResultNotification notification = new MailTestResultNotification()
            {
                DashBoardServerUrl = "http://gbhpdslivcweb01.dsi.ad.adp.com/",
                Drive = "Drive",
                IvcPack = "Live",
                NotificationTargets = "ashish.narmen@cdk.com;akash.sahu@cdk.com",
                DriveSprint = "Drive",
                IvcServer = "d-ivc-mt-app01.gbh.dsi.adp.com",
                SubModule = "POS_Parts",
                TestResultXmlFile = @"C:\Users\narmena\Desktop\nunit-test-result.xml",
                TestSuite = "PPOS15609009",
                TestSetName = "18-07-2017_Live-114504AM"

            };
            notification.Notify();
        }
    }
}
