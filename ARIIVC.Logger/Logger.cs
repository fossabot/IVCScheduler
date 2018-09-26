using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ARIIVC.Logger
{
        /// <summary>
        /// Defines the logger class that can be used for logging purposes
        /// This is implemented as a singleton so all the logs go to same stream through out the test execution
        /// </summary>
        public class Logger
        {
            private static volatile Logger _instance;
            private readonly string _logFileName;
            public static IntPtr MainWndHandle;
            private static object _syncRoot = new Object();
            private Logger()
            {
                CreateLogsDirectory();
                _logFileName = GenerateLogFileName();
            }

            private void CreateLogsDirectory()
            {
                if (!Directory.Exists("logs"))
                    Directory.CreateDirectory("logs");
            }

            private string GenerateLogFileName()
            {
                var serverName = ConfigurationManager.AppSettings["server"] ?? "SERVER";
                var serviceName = ConfigurationManager.AppSettings["service"] ?? "SERVICE";
                var localMachineName = System.Environment.MachineName;
                var currentTimeStamp = DateTime.UtcNow.ToString("u");
                var fileName =
                    string.Format("{0}_{1}_{2}_{3}", serverName, serviceName, localMachineName, currentTimeStamp)
                        .Replace(':', '_') + ".log";
                fileName = Path.Combine("logs", fileName);
                return fileName;
            }

            public static Logger Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        lock (_syncRoot)
                        {
                            if (_instance == null)
                                _instance = new Logger();
                        }
                    }
                    return _instance;
                }
            }

            /// <summary>
            /// This method logs a message to a file
            /// </summary>
            /// <param name="logType">Indicates what type of information it is</param>
            /// <param name="logMessage">message that needs to be logged</param>
            public void Log2File(string logType, string logMessage)
            {
                string logMessagetoWrite = string.Format("[{0}]\t{1}\t{2}", DateTime.UtcNow.ToString("u"), logType, logMessage);
                Write2File(ReplaceNewLinesWithTab(logMessagetoWrite));
            }

            private void Write2File(string logMessageToWrite)
            {
                var log = !File.Exists(_logFileName) ? new StreamWriter(_logFileName) : File.AppendText(_logFileName);
                log.WriteLine(logMessageToWrite);
                log.Close();
            }
            private string ReplaceNewLinesWithTab(string message)
            {
                var exceptionMessageString = message.Replace('\n', '\t');
                exceptionMessageString = exceptionMessageString.Replace('\r', '\t');
                return exceptionMessageString;
            }
            public void Debug(string message)
            {
                Log2File("DEBUG", message);
            }
            public void Debug(string message, Exception exception)
            {
                Debug(String.Format("{0}, Exception: {1}", message, exception));
            }
            public void Error(string message, Exception exception)
            {
                Err(String.Format("{0}, Exception: {1}", message, exception));
            }
            public void Err(string message)
            {
                Log2File("ERROR", message);
            }
            public void Pass(string message)
            {
                Log2File("PASS", message);
            }
            public void Fail(string message)
            {
                Log2File("FAIL", message);
            }
            public void Report(string message)
            {
                Log2File("REPORT", message);
            }
            public void Info(string message)
            {
                Log2File("INFO", message);
            }

            public void Fwrite(string filepath, string info, bool clear)
            {
                if (clear == true)
                {
                    File.WriteAllText(@filepath, String.Empty);
                }
                using (StreamWriter writer = new StreamWriter(filepath, true))
                {
                    writer.WriteLine(info);
                }
            }
        }

        public class TestLog
        {
            public string LogType { get; set; }
            public string Message { get; set; }
            public string Data { get; set; }
            public bool Result { get; set; }
            public TestLog(String logType, String message, String data, bool result)
            {
                this.LogType = logType;
                this.Message = message;
                this.Data = data;
                this.Result = result;
            }
        }

        public enum LogType
        {
            Header = 1,
            Err = 2,
            Info = 3,
            Success = 4,
            Start = 5,
            Finish = 6,
            Enter = 7,
            Debug = 8,
            Pass = 9,
            Fail = 10,
            Report = 11,
            IvcCheck = 12,
            Step = 13
        }


    
}
