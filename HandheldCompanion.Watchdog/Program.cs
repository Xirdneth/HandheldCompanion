using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace HandheldCompanion.Watchdog
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        private static Dictionary<string, string> Applications = new Dictionary<string, string>();
        static void Main()
        {
            //Uncomment below depending on what you want to do

            //Deploy / Install Service
            //================================================
            Applications.Add("HandheldCompanion", @"D:\Game Projects\HandheldCompanion\bin\Debug\net8.0-windows10.0.19041.0\HandheldCompanion.exe");

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1(Applications)
            };
            ServiceBase.Run(ServicesToRun);

            // Local testing
            //=============================================
            //AppWatcher appWatcher = new AppWatcher();
            ////appWatcher.Listen("----Name Of Application----", @"----Path To EXE to watch----");
            //appWatcher.Listen("HandheldCompanion", @"D:\Game Projects\HandheldCompanion\bin\Debug\net8.0-windows10.0.19041.0\HandheldCompanion.exe");

            //Task.Delay(-1).GetAwaiter().GetResult();
        }
    }
}
