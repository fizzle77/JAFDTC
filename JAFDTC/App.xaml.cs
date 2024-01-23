// ********************************************************************************************************************
//
// App.xaml.cs -- ui c# for main application
//
// Copyright(C) 2023-2024 ilominar/raven
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
// Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
// option) any later version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
// implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
// for more details.
//
// You should have received a copy of the GNU General Public License along with this program.  If not, see
// <https://www.gnu.org/licenses/>.
//
// ********************************************************************************************************************

using JAFDTC.Models;
using JAFDTC.UI.App;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace JAFDTC
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // windoze interfaces & data structs
        //
        // ------------------------------------------------------------------------------------------------------------

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, WindowShowStyle nCmdShow);

        // Full enum def is at https://github.com/dotnet/pinvoke/blob/main/src/User32/User32+WindowShowStyle.cs
        public enum WindowShowStyle : uint
        {
            SW_NORMAL = 1,
            SW_MAXIMIZE = 3,
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- public properties

        public MainWindow Window { get; private set; }

        public bool IsAppStartupGood { get; private set; }

        public IConfiguration CurrentConfig { get; set; }

        public double DCSLastLat { get; private set; }

        public double DCSLastLon { get; private set; }

        // ---- private properties

        private DispatcherTimer CheckDCSTimer { get; set; }

        private System.DateTimeOffset LastDCSExportCheck { get; set; }

        private bool IsUploadInFlight { get; set; }

        private long LastDCSExportPacketCount { get; set; }

        private bool IsJAFDTCPinnedToTop { get; set; }

        private bool UploadPressed { get; set; }

        private long UploadPressedTimestamp { get; set; }

        private bool IncPressed { get; set; }

        private bool DecPressed { get; set; }

        private long IncDecPressedTimestamp { get; set; }

        private string CurMarker { get; set; }

        // ---- public events, posts change/validation events

        // NOTE: these can be called from non-ui threads but may trigger ui actions. we will dispatch the handler
        // NOTE: invocations on a ui thread to avoid the chaos that will ensue.

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        private AirframeTypes _dcsActiveAirframe;
        public AirframeTypes DCSActiveAirframe
        {
            get => _dcsActiveAirframe;
            set
            {
                if (_dcsActiveAirframe != value)
                {
                    _dcsActiveAirframe = value;
                    OnPropertyChanged(nameof(DCSActiveAirframe));
                }
            }
        }

        /// <summary>
        /// reading IsDCSRunning will implicitly first set the property to the correct current value based on the state
        /// of dcs. this property does not have an explict set handler and generates property change events.
        /// </summary>
        private bool _isDCSRunning;
        public bool IsDCSRunning
        {
            get
            {
                if (_isDCSRunning && (Process.GetProcessesByName("DCS").Length == 0))
                {
                    _isDCSRunning = false;
                    OnPropertyChanged(nameof(IsDCSRunning));
                }
                else if (!_isDCSRunning && (Process.GetProcessesByName("DCS").Length > 0))
                {
                    _isDCSRunning = true;
                    OnPropertyChanged(nameof(IsDCSRunning));
                }
                return _isDCSRunning;
            }
        }

        /// <summary>
        /// reading IsDCSExporting will implicitly first set the property to the correct current value based on the
        /// state of dcs. this property does not have an explict set handler and generates property change events.
        /// </summary>
        private bool _isDCSExporting;
        public bool IsDCSExporting
        {
            get
            {
                if ((System.DateTimeOffset.Now - LastDCSExportCheck) > System.TimeSpan.FromSeconds(1.0))
                {
                    if (_isDCSExporting && (TelemDataRx.Instance.NumPackets == LastDCSExportPacketCount))
                    {
                        DCSActiveAirframe = AirframeTypes.None;
                        _isDCSExporting = false;
                        OnPropertyChanged(nameof(IsDCSExporting));
                    }
                    else if(!_isDCSExporting && (TelemDataRx.Instance.NumPackets != LastDCSExportPacketCount))
                    {
                        _isDCSExporting = true;
                        OnPropertyChanged(nameof(IsDCSExporting));
                    }
                    LastDCSExportPacketCount = TelemDataRx.Instance.NumPackets;
                }
                LastDCSExportCheck = System.DateTimeOffset.Now;
                return _isDCSExporting;
            }
        }

        /// <summary>
        /// reading IsDCSAvailable will implicitly first set the property to the correct current value based on the
        /// state of dcs. this property does not have an explict set handler and generates property change events.
        /// </summary>
        private bool _isDCSAvailable;
        public bool IsDCSAvailable
        {
            get
            {
                bool isAvail = (DCSLuaManager.IsLuaInstalled() && IsDCSRunning && IsDCSExporting);
                if (_isDCSAvailable != isAvail)
                {
                    _isDCSAvailable = isAvail;
                    OnPropertyChanged(nameof(IsDCSAvailable));
                }
                return _isDCSAvailable;
            }
        }

        // ---- private properties, read-only

        private readonly Dictionary<string, AirframeTypes> _dcsToJAFDTCTypeMap;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes the singleton application object. this is the first line of authored code executed, and as
        /// such is the logical equivalent of main() or WinMain(). preflight the file manager and settings before
        /// initializing the "dcs available" timer.
        /// </summary>
        public App()
        {
            InitializeComponent();

            DCSLastLat = 0.0;
            DCSLastLon = 0.0;

            LastDCSExportCheck = System.DateTimeOffset.Now;
            LastDCSExportPacketCount = 0;
            UploadPressedTimestamp = 0;
            IncDecPressedTimestamp = 0;

            _dcsToJAFDTCTypeMap = new Dictionary<string, AirframeTypes>()
            {
                ["A10C"] = AirframeTypes.A10C,
                ["AH64D"] = AirframeTypes.AH64D,
                ["AV8B"] = AirframeTypes.AV8B,
                ["F14AB"] = AirframeTypes.F14AB,
                ["F15E"] = AirframeTypes.F15E,
                ["F16CM"] = AirframeTypes.F16C,
                ["FA18C"] = AirframeTypes.FA18C,
                ["M2000C"] = AirframeTypes.M2000C,
            };

            try
            {
                IsAppStartupGood = false;

                FileManager.Preflight();
                Settings.Preflight();

                IsJAFDTCPinnedToTop = Settings.IsAlwaysOnTop;
                IsUploadInFlight = false;

                TelemDataRx.Instance.TelemDataReceived += TelemDataReceiver_DataReceived;
                TelemDataRx.Instance.Start();

                WyptCaptureDataRx.Instance.Start();

                CheckDCSTimer = new DispatcherTimer();
                CheckDCSTimer.Tick += CheckDCSTimer_Tick;
                CheckDCSTimer.Interval = new System.TimeSpan(0, 0, 10);

                IsAppStartupGood = true;
            }
            catch (System.Exception ex)
            {
                FileManager.Log($"App:App exception {ex}");
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // configuration upload
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// upload given configuration to dcs. the configuration is only uploaded if dcs is available, the configuration
        /// is valid, and the current dcs airframe matches the airframe of the configuration.
        /// </summary>
        public void UploadConfigurationToJet(IConfiguration cfg)
        {
            if (!IsDCSAvailable || (cfg == null) || (cfg.Airframe != DCSActiveAirframe) || !cfg.UploadAgent.Load())
            {
                Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    StatusMessageTx.Send("No Configuration Selected");
                    General.PlayAudio("ux_error.wav");
                });
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // export data processing
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// process markers from the dcs event stream. play a sound to indicate uploading has started or ended based
        /// on the marker. actions are carried out on the main thread via dispatch.
        /// </summary>
        private void ProcessMarker(TelemDataRx.TelemData data)
        {
            if (!IsUploadInFlight && !string.IsNullOrEmpty(data.Marker))
            {
                IsUploadInFlight = true;
                CurMarker = "";
                Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    General.PlayAudio("ux_action.wav");
                });
            }
            else if (IsUploadInFlight && !string.IsNullOrEmpty(data.Marker) && (data.Marker != CurMarker))
            {
                CurMarker = data.Marker;
                Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    StatusMessageTx.Send($"Setup {data.Marker}% Complete");
                });
            }
            else if (IsUploadInFlight && string.IsNullOrEmpty(data.Marker))
            {
                IsUploadInFlight = false;
                Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
                {
                    StatusMessageTx.Send("Avionics Setup Complete");
                    General.PlayAudio("ux_action.wav");
                    await Task.Delay(100);
                    General.PlayAudio("ux_action.wav");
                });
            }
        }

        /// <summary>
        /// process upload commands from the dcs event stream. triggers a configuration upload once the upload button
        /// has been pressed for the specified amount of time.
        /// </summary>
        private void ProcessUploadCommand(TelemDataRx.TelemData data)
        {
            if (IsUploadInFlight)
            {
                UploadPressed = false;
                UploadPressedTimestamp = 0;
            }
            else
            {
                if (!UploadPressed && (data.CmdUpload == "1") && (UploadPressedTimestamp == 0))
                {
                    UploadPressedTimestamp = DateTime.Now.Ticks;
                }
                if (data.CmdUpload == "0")
                {
                    UploadPressedTimestamp = 0;
                }

                UploadPressed = data.CmdUpload == "1";

                System.TimeSpan timespan = new(DateTime.Now.Ticks - UploadPressedTimestamp);
                if ((UploadPressedTimestamp != 0) && UploadPressed && (timespan.TotalMilliseconds > 250))
                { 
                    UploadPressedTimestamp = 0;
                    Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                    {
                        UploadConfigurationToJet(CurrentConfig);
                    });
                }
            }
        }

        /// <summary>
        /// process the pin/unpin commands from the dcs event stream. these change the order of the window stack
        /// to keep jafdtc always on top or allow it to be lowered into the background.
        /// </summary>
        private void ProcessWindowStackCommand(TelemDataRx.TelemData data)
        {
            bool isUpdateWindowLayer = false;
            if (data.CmdToggle == "1")
            {
                IsJAFDTCPinnedToTop = !IsJAFDTCPinnedToTop;
                isUpdateWindowLayer = true;
            }
            else if (!IsJAFDTCPinnedToTop && (data.CmdShow == "1"))
            {
                IsJAFDTCPinnedToTop = true;
                isUpdateWindowLayer = true;
            }
            else if (IsJAFDTCPinnedToTop && (data.CmdHide == "1"))
            {
                IsJAFDTCPinnedToTop = false;
                isUpdateWindowLayer = true;
            }
            if (isUpdateWindowLayer)
            {
                Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    if (IsJAFDTCPinnedToTop)
                    {
                        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(Window);
                        ShowWindow(hWnd, WindowShowStyle.SW_NORMAL);
                        Window.ConfigListPage.RebuildInterfaceState();
                    }
                    (Window.AppWindow.Presenter as OverlappedPresenter).IsAlwaysOnTop = IsJAFDTCPinnedToTop;
                    if (!IsJAFDTCPinnedToTop)
                    {
                        // TODO: reset cockpit "always on top" control to "not always on top" (eg, FLIR GAIN/LVL/AUTO in viper)?
                        Window.AppWindow.MoveInZOrderAtBottom();
                        Process[] arrProcesses = Process.GetProcessesByName("DCS");
                        if (arrProcesses.Length > 0)
                        {
                            IntPtr ipHwnd = arrProcesses[0].MainWindowHandle;
                            Thread.Sleep(100);
                            SetForegroundWindow(ipHwnd);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// process the increment and decrement commands from the dcs event stream. these change the currently selected
        /// configuration and (optionally) inform the user of the new configuration.
        /// </summary>
        private void ProcessIncrDecrCommands(TelemDataRx.TelemData data)
        {
            long curTicks = DateTime.Now.Ticks;
            System.TimeSpan timeSpan = new(curTicks - IncDecPressedTimestamp);

            if (!IncPressed && (data.CmdIncr == "1"))
            {
                IncPressed = true;
            }
            else if (IncPressed && (data.CmdIncr == "0"))
            {
                IncDecPressedTimestamp = curTicks;
                IncPressed = false;
                Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    Window.ConfigListPage?.PreviousConfiguration((timeSpan.TotalMilliseconds > 4000));
                });
            }

            if (!DecPressed && (data.CmdDecr == "1"))
            {
                DecPressed = true;
            }
            else if (DecPressed && (data.CmdDecr == "0"))
            {
                IncDecPressedTimestamp = curTicks;
                DecPressed = false;
                Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    Window.ConfigListPage?.NextConfiguration((timeSpan.TotalMilliseconds > 4000));
                });
            }
        }

        /// <summary>
        /// handle an inbound telemetry data packet from dcs. this packet provides information on which airframe is
        /// curerently active, state of the dtc, and cockpit control state that we use to trigger dtc actions.
        /// </summary>
        private void TelemDataReceiver_DataReceived(TelemDataRx.TelemData data)
        {
            if (Window != null)
            {
                DCSActiveAirframe = (_dcsToJAFDTCTypeMap.ContainsKey(data.Model)) ? _dcsToJAFDTCTypeMap[data.Model]
                                                                                  : AirframeTypes.None;
                DCSLastLat = (double.TryParse(data.Lat, out double lat)) ? lat : 0.0;
                DCSLastLon = (double.TryParse(data.Lat, out double lon)) ? lon : 0.0;

                ProcessMarker(data);
                ProcessUploadCommand(data);
                ProcessWindowStackCommand(data);
                ProcessIncrDecrCommands(data);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // events
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// application launched: create the main window, set it up, and activate it to get this show on the road.
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Window = new MainWindow();
            Window.Activated += MainWindow_Activated;
            Window.Activate();
        }

        /// <summary>
        /// check dcs timer ticks: update IsDCSAvailable state based on lua installation, dcs process running, and
        /// indications dcs export is running. force regeneration of dcs state.
        /// </summary>
        private void CheckDCSTimer_Tick(object sender, object args)
        {
            _ = IsDCSAvailable;
        }

        /// <summary>
        /// window activated: when a window is activated or deactivated, start or stop (respectively) the dcs state
        /// check timer to monitor that state for the rest of the ui. force regeneration of dcs state.
        /// </summary>
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                CheckDCSTimer?.Stop();
            }
            else
            {
                _ = IsDCSAvailable;
                CheckDCSTimer?.Start();
            }
        }
    }
}
