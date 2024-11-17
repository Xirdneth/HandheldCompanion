using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watchdog.ServerLib;
using Watchdog.Utilities;

namespace HandheldCompanion.Watchdog
{
    public class AppWatcher
    {
        private Logger _logger;
        private Configuration _configuration;
        private ConfigurationSerializer<Configuration> _configurationSerializer;
        private ApplicationWatcher applicationWatcher;
        public AppWatcher()
        {
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

            _logger.Debug("HandheldCompanion.Watchdog.Server Started");
        }
    }
}
