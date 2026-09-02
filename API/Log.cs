using Discord;
using System;

namespace NebulaBot.API
{
    public static class Log
    {
        private static Lock _InfoLock = new();
        private static Lock _CommandLock = new();
        private static Lock _DebugLock = new();
        private static Lock _VerboseLock = new();
        private static Lock _WarnLock = new();
        private static Lock _ErrorLock = new();
        private static Lock _FatalLock = new();


        /// <summary>
        /// Sends an Info level messages to the Bot console.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        public static void Info(string message)
        {
            lock (_InfoLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now}] [INFO]  {message}");
                Console.ForegroundColor = ConsoleColor.White;

            }
        }

        /// <summary>
        /// Used for Command feedback.
        /// </summary>
        /// <param name="message"></param>
        public static void Command(string message)
        {
            lock (_CommandLock)
                Console.WriteLine($"[{DateTime.Now.ToString()}] {message}", Console.ForegroundColor = ConsoleColor.Magenta);
        }

        /// <summary>
        /// Sends a Debug level messages to the Bot console.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        public static void Debug(string message)
        {
            lock (_DebugLock)
            {
                if (Config.Instance.LogLevel == LogSeverity.Debug)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"[{DateTime.Now}] [DEBUG]  {message}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }

        /// <summary>
        /// Sends a Verbose level messages to the Bot console.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        public static void Verbose(string message)
        {
            lock (_VerboseLock)
            {
                if (Config.Instance.LogLevel == LogSeverity.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"[{DateTime.Now}] [Verbose]  {message}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }

        /// <summary>
        /// Sends a Warn level messages to the Bot console.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        public static void Warn(string message)
        {
            lock (_WarnLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{DateTime.Now}] [WARN]  {message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /// <summary>
        /// Sends an Error level messages to the Bot console.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        public static void Error(string message)
        {
            lock (_ErrorLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now}] [ERROR]  {message}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }


        /// <summary>
        /// Throws a Fatal error level.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        /// <param name="err">The error description.</param>
        public static void Fatal(string message, string err)
        {
            lock (_FatalLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now.ToString()}] [FATAL]  {message}");
                Console.WriteLine($"[{DateTime.Now.ToString()}] [FATAL]  {err}");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}