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
            //Applications.Add("HandheldCompanion", @"C:\Program Files\HandheldCompanion\HandheldCompanion.exe");

            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[]
            //{
            //    new Service1(Applications)
            //};
            //ServiceBase.Run(ServicesToRun);

            // Below is just for testing 
            //=============================================
            //AppWatcher appWatcher = new AppWatcher();
            //appWatcher.Listen("----Name Of Application----",@"----Path To EXE to watch----");
            //appWatcher.Listen("HandheldCompanion", @"C:\Program Files\HandheldCompanion\HandheldCompanion.exe");

            //Task.Delay(-1).GetAwaiter().GetResult();
        }
    }
}
