using Sandbox.ModAPI;
using System;
using System.IO;

namespace Heart_Module.Data.Scripts.HeartModule.ExceptionHandler
{
    public class HeartLog
    {
        TextWriter writer;
        private static HeartLog I;

        public static void Log(string message)
        {
            I._Log(message);
        }
        public static void LogException(Exception ex, Type callingType, string prefix = "")
        {
            I._LogException(ex, callingType, prefix);
        }
        public static void LogException(n_SerializableError ex, Type callingType, string prefix = "")
        {
            I._LogException(ex, callingType, prefix);
        }


        public HeartLog()
        {
            I?.Close();
            I = this;
            writer = MyAPIGateway.Utilities.WriteFileInLocalStorage("debug.log", typeof(HeartLog));
            writer.WriteLine("LogStart");
            writer.Flush();
        }

        public void Close()
        {
            writer.Close();
            I = null;
        }

        private void _Log(string message)
        {
            writer.WriteLine($"{DateTime.UtcNow:HH:mm:ss}: {message}");
            writer.Flush();
        }

        private void _LogException(Exception ex, Type callingType, string prefix = "")
        {
            if (ex == null)
            {
                _Log("Null exception! CallingType: " + callingType.FullName);
                return;
            }

            _Log(prefix + $"Exception in {callingType.FullName}! {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
        }

        private void _LogException(n_SerializableError ex, Type callingType, string prefix = "")
        {
            if (ex == null)
            {
                _Log("Null exception! CallingType: " + callingType.FullName);
                return;
            }

            _Log(prefix + $"Exception in {callingType.FullName}! {ex.ExceptionMessage}\n{ex.ExceptionStackTrace}");
        }
    }
}
