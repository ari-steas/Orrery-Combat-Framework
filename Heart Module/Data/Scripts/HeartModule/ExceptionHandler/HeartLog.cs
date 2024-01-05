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
        }

        public void Log(string message)
        {
            writer.WriteLineAsync($"{DateTime.Now:HH:mm:ss}: {message}");
        }

        public void LogException(Exception ex, Type callingType, string prefix = "")
        {
            Log(prefix + $"Exception in {callingType.FullName}! {ex.Message}\n{ex.StackTrace}");
        }
    }
}
