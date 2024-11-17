using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Watchdog.ServerLib;
using Watchdog.Utilities;
using static System.Net.Mime.MediaTypeNames;

namespace HandheldCompanion.Watchdog
{
    public partial class Service1 : ServiceBase
    {
        private Logger _logger;
        private Configuration _configuration;
        private ConfigurationSerializer<Configuration> _configurationSerializer;
        private ApplicationWatcher applicationWatcher;
        private Dictionary<string, string> _applicationsToWatch;
        public Service1(Dictionary<string, string> applicationsToWatch)
        {    
            InitializeComponent();
            _applicationsToWatch = applicationsToWatch;
            _logger = LogManager.GetLogger("WatchdogServer");

            _configuration = new Configuration();
            _configurationSerializer = new ConfigurationSerializer<Configuration>("configuration.json", _configuration);
            _configuration = _configurationSerializer.Deserialize();
            applicationWatcher = new ApplicationWatcher(_logger);

        }

        public void Listen(string applicationName, string applicationPath)
        {
            applicationWatcher.AddMonitoredApplication(
               applicationName: applicationName,
               applicationPath: applicationPath,
               nonResponsiveInterval: 20,
               useHeartbeat: true,
               active: false);
            applicationWatcher.Deserialize(_configuration);

            _logger.Debug($"{applicationName} Added");
        }

        protected override void OnStart(string[] args)
        {

            foreach (var application in _applicationsToWatch)
            {
                Listen(application.Key, application.Value);
            }        

            _logger.Debug("HandheldCompanion.Watchdog.Server Started");
        }

        protected override void OnStop()
        {
            applicationWatcher.ApplicationHandlers.Clear();
            _logger.Debug("HandheldCompanion.Watchdog.Server Stopped");
        }
    }
}
