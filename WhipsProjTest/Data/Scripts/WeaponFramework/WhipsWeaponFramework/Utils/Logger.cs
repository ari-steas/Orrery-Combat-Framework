using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Utils;

namespace Whiplash.Utils
{
    public class Logger
    {
        public static Logger Default;

        public enum Severity { Info = 1, Warning = 2, Error = 4 };

        string _fileName;
        string _messageTag;
        StringBuilder _log = new StringBuilder();
        TextWriter _writer;

        const string LOG_MESSAGE_FORMAT = "{0} | {1}";
        const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss.ffff";

        public static void CreateDefault(string fileName, string messageTag)
        {
            Default = new Logger(fileName, messageTag);
        }

        public Logger(string fileName, string messageTag)
        {
            _fileName = fileName;
            _messageTag = messageTag;
            _writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(_fileName, typeof(Logger));

            this.WriteLine("Log created");
        }

        public void WriteLine(string line, Severity severity = Severity.Info, bool writeToGameLog = true)
        {
            string formattedLine = string.Format(LOG_MESSAGE_FORMAT, GetSeverityString(severity), line);
            _writer.WriteLine($"{DateTime.UtcNow.ToString(DATE_FORMAT)} | {formattedLine}");
            _writer.Flush();

            if (writeToGameLog)
            {
                MyLog.Default.WriteLine($"{_messageTag} | {formattedLine}");
            }
        }

        public void Close()
        {
            this.WriteLine("Log closed");
            _writer.Close();
        }

        private string GetSeverityString(Severity severity)
        {
            switch(severity)
            {
                case Severity.Info:
                    return "Info   ";
                case Severity.Warning:
                    return "Warning";
                case Severity.Error:
                    return "ERROR  ";
                default:
                    return "MISSING";
            }
        }
    }
}
