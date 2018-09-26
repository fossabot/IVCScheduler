using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ARIIVC.Scheduler.JsonReps;
using IVCScheduler;
using Newtonsoft.Json;

namespace ARIIVC.Scheduler
{
    interface ITestResultNotification
    {
        string TestResultXmlFile { get; set; }
        string GenerateNotificationContent();
        void Notify();

    }

    public class MailTestResultNotification : ITestResultNotification
    {
        public string TestResultXmlFile { get; set; }
        public string IvcPack { get; set; }
        public string IvcServer { get; set; }
        public string TestSuite { get; set; }
        public string TestSetName { get; set; }
        public string NotificationTargets { get; set; }
        public string SubModule { get; set; }
        public string DashBoardServerUrl { get; set; }
        public string Drive { get; set; }
        public string DriveSprint { get; set; }

        public string GetResultStatusString()
        {
            XmlDocument resultDoc = new XmlDocument();
            resultDoc.Load(TestResultXmlFile);
            string status = "";
            try
            {
                status = resultDoc.DocumentElement.SelectSingleNode("test-suite").Attributes["success"].Value.ToLower()
                    .Equals("true")
                    ? "Passed"
                    : "Failed";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                status = "Failed";
            }
            return status;
        }

        public void Notify()
        {
            XmlDocument resultDoc = new XmlDocument();
            resultDoc.Load(TestResultXmlFile);
            string status = GetResultStatusString();
            string htmlBody = GenerateNotificationContent();
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("ivcauto@cdk.com");
            AttachLogs(mail);
            //mail.To.Add("Akash.Sahu@cdk.com");
            string[] splitemail = NotificationTargets.Split(';');
            foreach (string tmp in splitemail)
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    mail.To.Add(tmp);
                }
            }
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;

            //clause for differentiating hyd and gbh smtp servers
            if (Environment.MachineName.ToLower().Contains("gbh"))
                client.Host = "dsirelay.gbh.dsi.adp.com";
            else
                client.Host = "139.126.11.110";
            
            mail.Subject = String.Format("IVC Regression Results | {0} | {1} | {2} - {3}", IvcPack, TestSuite, SubModule,
                status);
            mail.Body = htmlBody;
            mail.IsBodyHtml = true;
            client.Send(mail);

        }
        public void SendRemainingCasesMail(List<ivc_test_result> testList)
        {
            string mailContent = GenerateRemainingcases(testList, IvcPack);
            MailMessage frcMail = new MailMessage();
            frcMail.From = new MailAddress("ivcauto.frc@cdk.com");
            string[] splitemail = NotificationTargets.Split(';');
            foreach (string tmp in splitemail)
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    frcMail.To.Add(tmp);
                }
            }
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;

            //clause for differentiating hyd and gbh smtp servers
            if (Environment.MachineName.ToLower().Contains("gbh"))
                client.Host = "dsirelay.gbh.dsi.adp.com";
            else
                client.Host = "139.126.11.110";

            frcMail.Subject = String.Format("Not Updated Status for {0} @{1:HH:mm tt}",IvcPack, DateTime.Now);
            frcMail.Body = mailContent;
            frcMail.IsBodyHtml = true;
            client.Send(frcMail);
        }
        public string GenerateRemainingcases(List<ivc_test_result> testList, string Pack)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(File.ReadAllText("FrcMail.css"));
            sb.Append(File.ReadAllText("FrcMail.js"));

            sb.AppendLine(String.Format(
                "<p style=\"color:Black; font-size:18px\">Last Updated Status for {0} @{1:HH:mm tt} :<br/></p>",
                Pack, DateTime.Now));

            sb.AppendLine("<br/>");
            sb.Append("<table border=\"1\" width=\"750\" cellpadding=\"1\"");

            int total = testList.Count;
            sb.Append(
                string.Format(
                    "<thead>\r\n<tr>\r\n<th>Test Cases</th>\r\n<th>Has Entry in Last Updated</th>\r\n<th>Not Updated : {0}</th>\r\n</tr>\r\n</thead>",
                    total));
            Dictionary<string, List<ivc_test_result>> byModules =
                testList.GroupBy(t => t.module).ToDictionary(tt => tt.Key, tt => tt.ToList());

            sb.Append("<tbody>\r\n");
            foreach (string module in byModules.Keys)
            {
                sb.Append(
                    string.Format(
                        "<tbody class=\"labels\">\r\n<tr>\r\n<td colspan=\"1\">\r\n<label for=\"{0}\">{1}</label>\r\n",
                        module.ToLower(), module));

                List<ivc_test_result> moduleTestList = testList.FindAll(t => t.module.Equals(module));
                total = moduleTestList.Count;

                sb.Append(string.Format(
                    "\r\n</td>\r\n<td></td>\r\n<td>{0}</td>\r\n", total));
                sb.Append("\r\n</tbody>");

                Dictionary<string, List<ivc_test_result>> bySubModules =
                    moduleTestList.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());

                sb.Append("<tbody class=\"hide\">");
                foreach (string submodule in bySubModules.Keys)
                {
                    List<ivc_test_result> subModuleTestList =
                        moduleTestList.FindAll(t => t.submodule.Equals(submodule));

                    total = subModuleTestList.Count;
                    sb.Append(
                        string.Format(
                            "<tr>\r\n<td>{0}</td>\r\n<td></td>\r\n<td>{1}</td>\r\n",
                            submodule, total));
                    foreach (ivc_test_result test in subModuleTestList)
                    {
                        sb.Append(
                        string.Format(
                            "<tr>\r\n<td>{0}</td>\r\n<td>{1}</td>\r\n<td></td>\r\n</tr>",
                            test.name, test.status));
                    }
                }

                sb.Append("</tbody>");
            }
            sb.Append("</tbody>\r\n");
            sb.Append("</table>\r\n");
            return sb.ToString();
        }
        public void AttachLogs(MailMessage mail)
        {
            if (GetResultStatusString() != "Passed" && Directory.EnumerateFileSystemEntries(Path.Combine(Directory.GetCurrentDirectory(), "logs")).Any())
            {
                foreach (string logFile in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "logs")))
                {
                    mail.Attachments.Add(new Attachment(logFile));
                }
            }
        }

        private string GetAttributefromXML(string param, string atrb)
        {
            XmlDocument myObjxml = new XmlDocument();
            myObjxml.Load(TestResultXmlFile);
            return myObjxml.SelectSingleNode(param).Attributes[atrb].Value;
        }

        public string GenerateNotificationContent()
        {
            string resultLink = null;
            try
            {
                XmlDocument myObjxml = new XmlDocument();
                myObjxml.Load(TestResultXmlFile);
                string runOnserver = GetAttributefromXML("//test-results//environment", "machine-name");
                StringBuilder stBuilder = new StringBuilder();
                stBuilder.AppendLine("<html><body><b>IVC Regression Test Results:</b>");
                stBuilder.AppendLine("<p style=\"font-family:verdana; color:Black; font-size:12px\">");
                switch (IvcPack)
                {
                    case "MT":
                    case "Pilot":
                    case "Live":
                    case "JobMaps":
                        resultLink = DashBoardServerUrl + "ivc/" + "?" + "pack=" + IvcPack + "&" + "group=" + Drive;
                        break;
                    default:
                        resultLink = DashBoardServerUrl + "ivc/" + "?" + "pack=" + IvcPack + "&" + "group=" + DriveSprint;
                        break;
                }
                stBuilder.AppendLine(String.Format("<p style=\"font-family:verdana; color:Black; font-size:12px\">Please find below the results summary.For more details @ <a href='{0}'>Click Here</a><br/></p>", resultLink));
                stBuilder.AppendLine("<table border=\"1\" width=\"40%\" cellpadding=\"5\" style=\"font-family:Verdana; font-size:13px\">");
                stBuilder.AppendLine("<tr><td><b><font color=#0478C1>Team</font></b></td>");
                stBuilder.AppendLine(String.Format("<td>{0}</td></tr>", IvcPack));
                stBuilder.AppendLine("<tr><td><b><font color=#0478C1>Pack</font></b></td>");
                stBuilder.AppendLine(String.Format("<td>{0}</td></tr>", IvcServer));
                stBuilder.AppendLine("<tr><td><b><font color=#0478C1>Test Set</font></b></td>");
                stBuilder.AppendLine(String.Format("<td>{0}</td></tr>", TestSetName));
                stBuilder.AppendLine("<tr><td><b><font color=#0478C1>Target Machine</font></b></td>");
                stBuilder.AppendLine(String.Format("<td>{0}</td></tr>", myObjxml.DocumentElement.SelectSingleNode("environment").Attributes["machine-name"].Value));
                stBuilder.AppendLine("<tr><td><b><font color=#0478C1>Run Timestamp</font></b></td>");
                stBuilder.AppendLine(String.Format("<td>{0}</td></tr>", GetAttributefromXML("//test-results", "date") + " " + GetAttributefromXML("//test-results", "time")));
                stBuilder.AppendLine("</table>");
                string testsExecuted = GetAttributefromXML("test-results", "total");
                string testsFailed = GetAttributefromXML("test-results", "failures");
                string testErrors = GetAttributefromXML("test-results", "errors");
                string testnorun = GetAttributefromXML("test-results", "not-run");
                string testsignored = GetAttributefromXML("test-results", "ignored");
                Double executionTime = Convert.ToDouble(GetAttributefromXML("//test-results//test-suite", "time")) / 60;
                int testsPassed = int.Parse(testsExecuted) - (int.Parse(testsFailed) + int.Parse(testErrors) + int.Parse(testnorun) + int.Parse(testsignored));
                string testServer = runOnserver;
                stBuilder.AppendLine("<br/>");
                if ((int.Parse(testsFailed) > 0) || (int.Parse(testErrors) > 0))
                    stBuilder.AppendLine("<table border=\"1\" width=\"80%\" cellpadding=\"5\" style=\"background-color:#FF6347; font-family:Verdana; font-size:13px\"><tr><td>");
                else
                    stBuilder.AppendLine("<table border=\"1\" width=\"80%\" cellpadding=\"5\" style=\"background-color:#C8EFA2; font-family:Verdana; font-size:13px\"><tr>");
                stBuilder.AppendLine("<td><b>Executed: " + testsExecuted + " </b></td>");
                stBuilder.AppendLine("<td><b>Success: " + testsPassed + "</b></td>");
                stBuilder.AppendLine("<td><b>Failure: " + testsFailed + " </b></td>");
                stBuilder.AppendLine("<td><b>Errors: " + testErrors + " </b></td>");
                stBuilder.AppendLine("<td><b>No Run: " + testnorun + " </b></td>");
                stBuilder.AppendLine("<td><b>Run Time: " + Math.Round(executionTime, 3) + " minutes </b></td></tr></table>");
                stBuilder.AppendLine("<br/><br/>");
                stBuilder.AppendLine("<table border=\"1\" width=\"80%\" cellpadding=\"1\" style=\"font-family:Verdana; font-size:13px\"");
                stBuilder.AppendLine("<td bgcolor='A9E8FA'><b>Testcase</b></td>");
                stBuilder.AppendLine("<td bgcolor='A9E8FA'><b>Description</b></td>");
                stBuilder.AppendLine("<td bgcolor='A9E8FA'><b>Status</b></td>");
                stBuilder.AppendLine("<td bgcolor='A9E8FA'><b>Execution Time</b></td>");
                foreach (XmlNode testnode in myObjxml.GetElementsByTagName("test-case"))
                {
                    bool oldlibrARY = (testnode.Attributes["name"].Value.ToLower().Contains("ivcdrivetests")) ? true : false;
                    int len = testnode.Attributes["name"].Value.Split('.').Length;
                    string testname = testnode.Attributes["name"].Value.Split('.')[len - 1];

                    string testdescription;

                    if (oldlibrARY)
                    {
                        testdescription = testnode.Attributes["description"].Value;
                    }
                    else
                    {
                        if (testnode.SelectSingleNode("properties/property[@name='Description']") != null)
                            testdescription = testnode.SelectSingleNode("properties/property[@name='Description']").Attributes["value"].Value;
                        else
                            testdescription = testnode.Attributes["description"].Value;
                    }

                    if (testnode.Attributes["result"].Value.ToLower().Equals("success"))
                        stBuilder.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td bgcolr:'#C8EFA2'><font color=green>{2}</font></td><td>{3}</td></tr>", testname, testdescription, testnode.Attributes["result"].Value, testnode.Attributes["time"].Value));
                    else if (testnode.Attributes["result"].Value.ToLower().Equals("error") || testnode.Attributes["result"].Value.ToLower().Equals("failure"))
                    {
                        stBuilder.AppendLine(string.Format("<tr><td>{0}</td>", testname));
                        stBuilder.AppendLine(string.Format("<td>{0}", testdescription));
                        switch (IvcPack)
                        {
                            case "MT":
                            case "Pilot":
                            case "Live":
                            case "JobMaps":
                                if (testnode.SelectSingleNode("failure").SelectSingleNode("message") != null)
                                {
                                    stBuilder.AppendLine(string.Format("<br/><br/>{0}", testnode.SelectSingleNode("failure").SelectSingleNode("message").InnerText));
                                    stBuilder.AppendLine(string.Format("<br/>{0}</td>", testnode.SelectSingleNode("failure").SelectSingleNode("stack-trace").InnerText));
                                }
                                break;
                        }
                        stBuilder.AppendLine(string.Format("<td><font color=red>{0}</font></td><td>{1}</td></tr>", testnode.Attributes["result"].Value, testnode.Attributes["time"].Value));

                        bool updateCounter = false;
                        if (testnode.SelectSingleNode("failure").SelectSingleNode("message").InnerText.Contains("TestFixtureSetUp failed"))
                            updateCounter = true;
                        //updateTestException(testlabpath, testnode.Attributes["name"].Value, string.Format("{0} \n {1}", testnode.SelectSingleNode("failure").SelectSingleNode("message").InnerText, testnode.SelectSingleNode("failure").SelectSingleNode("stack-trace").InnerText), updateCounter);
                    }
                    else
                    {
                        stBuilder.AppendLine(string.Format("<tr><td>{0}</td><td>{1}</td><td><font color=orange>{2}</font></td><td>{3}</td></tr>", testnode.Attributes["name"].Value.Split('.')[testnode.Attributes["name"].Value.Split('.').Length - 1], testdescription, testnode.Attributes["result"].Value, "0"));
                    }
                }

                stBuilder.AppendLine("</table>");
                stBuilder.AppendLine("<p style=\"font-family:verdana; color:Black; font-size:13px\"><br/><br/><b>Thanks,</b><br/>IVC Team<br/></p></body></html>");
                return stBuilder.ToString();
            }
            catch (Exception ee)
            {
                StringBuilder exception = new StringBuilder();
                exception.AppendLine(ee.Message + "<br/>" + ee.StackTrace);
                return exception.ToString();
            }
        }

        public void SendFrcMail(List<ivc_test_result> testList)
        {
            string mailContent = GenerateFrcMail(testList);
            MailMessage frcMail = new MailMessage();
            frcMail.From = new MailAddress("ivcauto.frc@cdk.com");
            string[] splitemail = NotificationTargets.Split(';');
            foreach (string tmp in splitemail)
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    frcMail.To.Add(tmp);
                }
            }
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;

            //clause for differentiating hyd and gbh smtp servers
            if (Environment.MachineName.ToLower().Contains("gbh"))
                client.Host = "dsirelay.gbh.dsi.adp.com";
            else
                client.Host = "139.126.11.110";

            frcMail.Subject = String.Format("Regression Execution Status for {0} @{1:HH:mm tt} : {2}",
                IvcPack, DateTime.Now, testList.FindAll(t => t.status.Equals("Passed")).Count);
            frcMail.Body = mailContent;
            frcMail.IsBodyHtml = true;
            client.Send(frcMail);
        }

        public string GenerateFrcMail(List<ivc_test_result> testList)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(File.ReadAllText("FrcMail.css"));
            sb.Append(File.ReadAllText("FrcMail.js"));

            sb.AppendLine(String.Format(
                "<p style=\"color:Black; font-size:18px\">Regression Execution Status for {0} @{1:HH:mm tt} :<br/></p>",
                IvcPack, DateTime.Now));

            sb.AppendLine(
                "<table border=\"1\" width=\"750\" cellpadding=\"1\" style=\"color:#ffffff;background-color:#2d5bb9;font-size:18px\"><tr>");
            sb.AppendLine(string.Format("<td><b>Executed by Scheduler: {0} </b></td>", testList.FindAll(t => t.runner.Equals("nunit-console-x86")).Count));
            sb.AppendLine(string.Format("<td><b>Executed Manually: {0} </b></td>", testList.FindAll(t => t.runner.Equals("nunit-x86")).Count));
            sb.Append("</tr></table>");

            sb.AppendLine("<br/>");
            sb.Append("<table border=\"1\" width=\"750\" cellpadding=\"1\"");

            int passed = testList.FindAll(t => t.status.Equals("Passed")).Count;
            int failed = testList.FindAll(t => t.status.Equals("Failed")).Count;
            int noRun = testList.FindAll(t => t.status.Equals("No Run")).Count;
            int total = testList.Count;
            sb.Append(
                string.Format(
                    "<thead>\r\n<tr>\r\n<th>Total Count</th>\r\n<th>{0} Passed</th>\r\n<th>{1} Failed</th>\r\n<th>{2} No Run</th>\r\n<th>{3} Total</th>\r\n</tr>\r\n</thead>",
                    passed, failed, noRun, total));
            Dictionary<string, List<ivc_test_result>> byModules =
                testList.GroupBy(t => t.module).ToDictionary(tt => tt.Key, tt => tt.ToList());

            sb.Append("<tbody>\r\n");
            foreach (string module in byModules.Keys)
            {
                sb.Append(
                    string.Format(
                        "<tbody class=\"labels\">\r\n<tr>\r\n<td colspan=\"1\">\r\n<label for=\"{0}\">{1}</label>\r\n",
                        module.ToLower(), module));

                List<ivc_test_result> moduleTestList = testList.FindAll(t => t.module.Equals(module));
                passed = moduleTestList.FindAll(t => t.status.Equals("Passed")).Count;
                failed = moduleTestList.FindAll(t => t.status.Equals("Failed")).Count;
                noRun = moduleTestList.FindAll(t => t.status.Equals("No Run")).Count;
                total = moduleTestList.Count;

                sb.Append(string.Format(
                    "\r\n</td>\r\n<td>{0}</td>\r\n<td>{1}</td>\r\n<td>{2}</td>\r\n<td>{3}</td>\r\n</tr>",
                    passed, failed, noRun, total));
                sb.Append("\r\n</tbody>");

                Dictionary<string, List<ivc_test_result>> bySubModules =
                    moduleTestList.GroupBy(t => t.submodule).ToDictionary(tt => tt.Key, tt => tt.ToList());

                sb.Append("<tbody class=\"hide\">");
                foreach (string submodule in bySubModules.Keys)
                {
                    List<ivc_test_result> subModuleTestList =
                        moduleTestList.FindAll(t => t.submodule.Equals(submodule));

                    passed = subModuleTestList.FindAll(t => t.status.Equals("Passed")).Count;
                    failed = subModuleTestList.FindAll(t => t.status.Equals("Failed")).Count;
                    noRun = subModuleTestList.FindAll(t => t.status.Equals("No Run")).Count;
                    total = subModuleTestList.Count;

                    sb.Append(
                        string.Format(
                            "<tr>\r\n<td>{0}</td>\r\n<td>{1}</td>\r\n<td>{2}</td>\r\n<td>{3}</td>\r\n<td>{4}</td>\r\n</tr>",
                            submodule, passed, failed, noRun, total));
                }

                sb.Append("</tbody>");
            }

            sb.Append("</tbody>\r\n");
            sb.Append("</table>\r\n");
            return sb.ToString();
        }

    }


    public class HipChatTestResultNotification : ITestResultNotification
    {
        public string TestResultXmlFile { get; set; }
        public string GenerateNotificationContent()
        {
            throw new NotImplementedException();
        }

        public void Notify()
        {
            throw new NotImplementedException();
        }
    }


}
