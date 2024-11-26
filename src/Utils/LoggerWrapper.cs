using System;
using Service.LogicCommon.Utils;

namespace Service.LogicCommon.Utils
{
    internal class LoggerWrapper : ILoggerWrapper
    {
        public void Debug(string message)
        {
            Console.WriteLine($"DEBUG {message}");
        }

        public void Error(string message)
        {
            Console.WriteLine($"ERROR {message}");
        }

        public void Info(string message)
        {
            Console.WriteLine($"INFO {message}");
        }
    }
}