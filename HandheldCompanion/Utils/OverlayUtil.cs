using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using HandheldCompanion.Controls;
using HandheldCompanion.Managers;
using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Views;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HandheldCompanion.Utils
{
    public class OverlayUtil : IDisposable
    {
        public StickyWindow Myoverlaywin;
        public Graphics Mygfx;

        private Dictionary<string, SolidBrush> _brushes;
        private Dictionary<string, Font> _fonts;
        private Dictionary<string, Image> _images;
        public static IntPtr gameclient;
        //private OverlayUtil overlayinstance = new OverlayUtil();
        public static bool ShowOverlayText = false;
        public static string myText = "Overlay is loaded!";
        private static bool StopTimer = false;
        private static int timeschecked = 0;
        public OverlayUtil(Lazy<IPerformanceManager> performanceManager, Lazy<IProcessManager> processManager, Lazy<IToastManager> toastManager)
        {
            this.performanceManager = performanceManager;
            this.processManager = processManager;
            this.toastManager = toastManager;
            processManager.Value.ForegroundChanged += ProcessManager_ForegroundChanged;
        }

        public void CreateOverlay()
        {
            _brushes = new Dictionary<string, SolidBrush>();
            _fonts = new Dictionary<string, Font>();
            _images = new Dictionary<string, Image>();

            Mygfx = new Graphics()
            {
                MeasureFPS = false,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true,
                WindowHandle = IntPtr.Zero
            };

            Myoverlaywin = new StickyWindow(gameclient, Mygfx)
            {
                FPS = 30,
                IsTopmost = true,
                IsVisible = true,
                AttachToClientArea = true,
                BypassTopmost = true
            };

            Myoverlaywin.DestroyGraphics += Window_DestroyGraphics;
            Myoverlaywin.DrawGraphics += Window_DrawGraphics;
            Myoverlaywin.SetupGraphics += Window_SetupGraphics;
        }

        private void Window_SetupGraphics(object sender, SetupGraphicsEventArgs e)
        {
            var gfx = e.Graphics;

            if (e.RecreateResources)
            {
                foreach (var pair in _brushes) pair.Value.Dispose();
                foreach (var pair in _images) pair.Value.Dispose();
            }

            _brushes["transparent"] = gfx.CreateSolidBrush(0, 0, 0, 0);
            _brushes["redsemi"] = gfx.CreateSolidBrush(255, 0, 0, 160);
            _brushes["orangesemi"] = gfx.CreateSolidBrush(253, 106, 2, 160);
            _brushes["white"] = gfx.CreateSolidBrush(255, 255, 255);
            _brushes["blacksemi"] = gfx.CreateSolidBrush(0, 0, 0, 100);

            if (e.RecreateResources) return;

            _fonts["corbel"] = gfx.CreateFont("Corbel", 16);

        }

        private void Window_DestroyGraphics(object sender, DestroyGraphicsEventArgs e)
        {
            foreach (var pair in _brushes) pair.Value.Dispose();
            foreach (var pair in _fonts) pair.Value.Dispose();
            foreach (var pair in _images) pair.Value.Dispose();
        }

        private void Window_DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;
            gfx.ClearScene();

            DrawText(gfx);
        }

        private void DrawText(Graphics gfx)
        {
            var overlayText = new StringBuilder()
                   .Append(myText)
                   .ToString();
            gfx.DrawTextWithBackground(_fonts["corbel"], _brushes["white"], _brushes["blacksemi"], 1280 / 2, 720 / 2, overlayText);
        }

        //private void TimerCheckWindows()
        //{
        //    DispatcherTimer timer = new DispatcherTimer
        //    {
        //        Interval = new TimeSpan(0, 0, 1)
        //    };

        //    timer.Tick += (sender, EventArgs) =>
        //    {
        //        if (StopTimer)
        //        {
        //            timer.Stop();
        //        }

        //        if (StopTimer == false && WindowHelper.GetForegroundWindow() != this.Myoverlaywin.Handle)
        //        {
        //            WindowHelper.EnableBlurBehind(gameclient);
        //            this.Myoverlaywin.Recreate();
        //        }
        //    };
        //    timer.Start();
        //}

        private void ProcessManager_ForegroundChanged(ProcessEx proc, ProcessEx back)
        {
            try
            {
                Thread.Sleep(1500);
                var handel = WindowHelper.GetForegroundWindow();
                if (proc.Process.MainModule.ModuleName == "bg3_dx11.exe")
                {
                    LogManager.LogInformation($"Recreating overlay sleeping 3s");
                    timeschecked++;
                    
                    
                    myText = "Recreated overlay: " + timeschecked;
                    WindowHelper.EnableBlurBehind(gameclient);
                    
                    this.Myoverlaywin.Recreate();
                    //toastManager.Value.SendToast($"Overlay Recreated");
                    LogManager.LogInformation($"Recreated overlay");
                }
            }
            catch
            {
            }
        }

        public IntPtr GetProcessHandle(string client)
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                var processes = Process.GetProcessesByName(client);
                if (processes.Length > 0)
                {
                    handle = processes[0].MainWindowHandle;
                }
                else
                {
                }
            }
            catch
            {
            }
            return handle;
        }

        public void Button_On()
        {
            //gameclient = processManager.Value.GetForegroundProcess().MainWindowHandle;
            List<Process> windowhandels = GetWindowHandles();
            gameclient = windowhandels.Where(x => x.ProcessName == "bg3_dx11").FirstOrDefault().MainWindowHandle;
            this.CreateOverlay();
            this.Myoverlaywin.ParentWindowHandle = gameclient;
            this.Myoverlaywin.Create();

            ShowOverlayText = true;
            StopTimer = false;

            this.Myoverlaywin.Show();
            
        }

        public static List<Process> GetWindowHandles()
        {
            List<Process> List = new List<Process>();

            foreach (Process window in Process.GetProcesses())
            {
                window.Refresh();

                if (window.MainWindowHandle != IntPtr.Zero)
                {
                    List.Add(window);
                }
            }

            return List;
        }

        private void Button_Off(object sender, RoutedEventArgs e)
        {
            StopTimer = true;

            this.Myoverlaywin.Dispose();
            this.Mygfx.Dispose();
        }

        ~OverlayUtil()
        {
            Dispose(false);
        }

        #region IDisposable Support
        private bool disposedValue;
        private readonly Lazy<IPerformanceManager> performanceManager;
        private readonly Lazy<IProcessManager> processManager;
        private readonly Lazy<IToastManager> toastManager;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Myoverlaywin.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}