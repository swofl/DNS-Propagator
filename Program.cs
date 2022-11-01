using NLog;
using NLog.Config;
using System;
using System.ServiceProcess;

namespace DNS_Change_Propagator
{
    static class Program
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            LoadLoggingConfiguration();

            _log.Info("Service starting up...");

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };

            try
            {
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }

        private static void LoadLoggingConfiguration()
        {
            var config = new LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "logs\\log.txt",
                CreateDirs = true,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.DateAndSequence,
                ArchiveOldFileOnStartup = true,
                MaxArchiveFiles = 5,
                Encoding = System.Text.Encoding.UTF8,
                // Default layout with forcing invariant culture for messages, especially for stacktrace
                Layout = NLog.Layouts.Layout.FromString("${longdate} ${level:uppercase=true}: ${message:withexception=true}")
            };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logconsole);
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);

            // Apply config
            LogManager.Configuration = config;
        }
    }
}
