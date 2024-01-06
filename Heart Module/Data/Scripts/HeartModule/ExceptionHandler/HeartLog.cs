using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heart_Module.Data.Scripts.HeartModule.ExceptionHandler
{
    public class HeartLog
    {
        TextWriter writer;

        public HeartLog()
        {
            writer = MyAPIGateway.Utilities.WriteFileInLocalStorage("debug.log", typeof(HeartLog));
            writer.WriteLine("LogStart");
            writer.Flush();
        }

        public void Log(string message)
        {
            writer.WriteLine($"{DateTime.Now:HH:mm:ss}: {message}");
            writer.Flush();
        }

        public void LogException(Exception ex, Type callingType, string prefix = "")
        {
            Log(prefix + $"Exception in {callingType.FullName}! {ex.Message}\n{ex.StackTrace}");
        }
    }
}
