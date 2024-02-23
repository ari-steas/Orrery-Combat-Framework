using Sandbox.ModAPI;
using System;
using System.IO;

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

        public void Close()
        {
            writer.Close();
        }

        public void Log(string message)
        {
            writer.WriteLine($"{DateTime.UtcNow:HH:mm:ss}: {message}");
            writer.Flush();
        }

        public void LogException(Exception ex, Type callingType, string prefix = "")
        {
            if (ex == null)
            {
                Log("Null exception! CallingType: " + callingType.FullName);
                return;
            }

            Log(prefix + $"Exception in {callingType.FullName}! {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
        }

        public void LogException(n_SerializableError ex, Type callingType, string prefix = "")
        {
            if (ex == null)
            {
                Log("Null exception! CallingType: " + callingType.FullName);
                return;
            }

            Log(prefix + $"Exception in {callingType.FullName}! {ex.ExceptionMessage}\n{ex.ExceptionStackTrace}");
        }
    }
}
