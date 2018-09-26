using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace ARIIVC.Scheduler
{
    public class SchedulerLogger
    {
        private static string mongoUrl = "mongodb://c05drddrv969.dslab.ad.adp.com:27017";
        private static IMongoClient _client = new MongoClient(mongoUrl);
        private static IMongoDatabase _database = _client.GetDatabase("ivc");
        private static IMongoCollection<SchedulerLog> _schedulerLogs = _database.GetCollection<SchedulerLog>("scheduler_logs");
    

        public static void Log(string testset, string logMessage)
        {
            logMessage = FormatLogMessage(logMessage);
            SchedulerLog logEntry = new SchedulerLog() { TestsetName = testset, LogMessage = logMessage};
            _schedulerLogs.InsertOne(logEntry);
        }

        public static string FormatLogMessage(string logMessage)
        {
            string currentTimeStamp = DateTime.Now.ToLongTimeString();
            string hyperLinkedBuildUrl = string.Format("<a href=\"{0}console\">{1}-{2}</a>",
                Environment.GetEnvironmentVariable("BUILD_URL"), Environment.GetEnvironmentVariable("JOB_NAME"),
                Environment.GetEnvironmentVariable("BUILD_NUMBER"));
            string computerName = Environment.GetEnvironmentVariable("COMPUTERNAME");

            return string.Format("{0} | {1} | {2} | {3}", currentTimeStamp, hyperLinkedBuildUrl, computerName,
                logMessage);
        }

    }
    
    public class SchedulerLog
    {
        [BsonElement("testsetname")]
        public string TestsetName { get; set; }
        [BsonElement("logmessage")]
        public string LogMessage {get;set;}
        
    }
}
