// ********************************************************************************************************************
//
// App.xaml.cs -- ui c# for main application
//
// Copyright(C) 2023 ilominar/raven
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
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change events.

        public MainWindow Window { get; private set; }

        public IConfiguration CurrentConfig { get; set; }

        private DispatcherTimer CheckDCSTimer { get; set; }

        private DateTimeOffset LastDCSExportCheck { get; set; }

        private bool IsUploadInFlight { get; set; }

        private long LastDCSExportPacketCount { get; set; }

        private bool IsJAFDTCPinnedToTop { get; set; }

        private bool UploadPressed { get; set; }

        private long UploadPressedTimestamp { get; set; }

        private bool IncPressed { get; set; }

        private bool DecPressed { get; set; }

        // ---- following properties post change events.

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

        // reading IsDCSRunning will implicitly first set the property to the correct current value based on the state
        // of dcs. this property does not have an explict set handler and generates property change events.
        //
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

        // reading IsDCSExporting will implicitly first set the property to the correct current value based on the
        // state of dcs. this property does not have an explict set handler and generates property change events.
        //
        private bool _isDCSExporting;
        public bool IsDCSExporting
        {
            get
            {
                if ((DateTimeOffset.Now - LastDCSExportCheck) > TimeSpan.FromSeconds(1.0))
                {
                    if (_isDCSExporting && (DataReceiver.NumPackets == LastDCSExportPacketCount))
                    {
                        DCSActiveAirframe = AirframeTypes.None;
                        _isDCSExporting = false;
                        OnPropertyChanged(nameof(IsDCSExporting));
                    }
                    else if(!_isDCSExporting && (DataReceiver.NumPackets != LastDCSExportPacketCount))
                    {
                        _isDCSExporting = true;
                        OnPropertyChanged(nameof(IsDCSExporting));
                    }
                    LastDCSExportPacketCount = DataReceiver.NumPackets;
                }
                LastDCSExportCheck = DateTimeOffset.Now;
                return _isDCSExporting;
            }
        }

        // reading IsDCSAvailable will implicitly first set the property to the correct current value based on the
        // state of dcs. this property does not have an explict set handler and generates property change events.
        //
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

        // ---- read-only fields

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

            LastDCSExportCheck = DateTimeOffset.Now;
            LastDCSExportPacketCount = 0;
            UploadPressedTimestamp = 0;

            _dcsToJAFDTCTypeMap = new Dictionary<string, AirframeTypes>()
            {
                ["A10C"] = AirframeTypes.A10C,
                ["AH64D"] = AirframeTypes.AH64D,
                ["AV8B"] = AirframeTypes.AV8B,
                ["F15E"] = AirframeTypes.F15E,
                ["F16CM"] = AirframeTypes.F16C,
                ["FA18C"] = AirframeTypes.FA18C,
                ["M2000C"] = AirframeTypes.M2000C
            };

            try
            {
                FileManager.Preflight();
                Settings.Preflight();
            }
            catch
            {
                // TODO: handle this "gracefully": if this fails, we're screwed...
            }

            DataReceiver.DataReceived += DataReceiver_DataReceived;
            DataReceiver.Start();

            CheckDCSTimer = new DispatcherTimer();
            CheckDCSTimer.Tick += CheckDCSTimer_Tick;
            CheckDCSTimer.Interval = new TimeSpan(0, 0, 10);

            IsJAFDTCPinnedToTop = Settings.IsAlwaysOnTop;

            IsUploadInFlight = false;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // configuration upload
        //
        // ------------------------------------------------------------------------------------------------------------

        // upload given configuration to dcs. the configuration is only uploaded if dcs is available, the configuration
        // is valid, and the current dcs airframe matches the airframe of the configuration.
        //
        public void UploadConfigurationToJet(IConfiguration cfg)
        {
            if (!IsDCSAvailable || (cfg == null) || (cfg.Airframe != DCSActiveAirframe) || !cfg.UploadAgent.Load())
            {
                Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    General.PlayAudio("ux_error.wav");
                });
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // export data processing
        //
        // ------------------------------------------------------------------------------------------------------------

        // process markers from the dcs event stream. play a sound to indicate uploading has started or ended based
        // on the marker. actions are carried out on the main thread via dispatch.
        //
        private void ProcessMarker(DataReceiver.Data data)
        {
            if (!IsUploadInFlight && !string.IsNullOrEmpty(data.Marker))
            {
                IsUploadInFlight = true;
                Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    General.PlayAudio("ux_action.wav");
                });
            }
            else if (IsUploadInFlight && string.IsNullOrEmpty(data.Marker))
            {
                IsUploadInFlight = false;
                Window.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
                {
                    General.PlayAudio("ux_action.wav");
                    await Task.Delay(100);
                    General.PlayAudio("ux_action.wav");
                });
            }
        }

        // process upload commands from the dcs event stream. triggers a configuration upload once the upload button
        // has been pressed for the specified amount of time.
        //
        private void ProcessUploadCommand(DataReceiver.Data data)
        {
            if (IsUploadInFlight)
            {
                UploadPressed = false;
                UploadPressedTimestamp = 0;
            }
            else
            {
                if (!UploadPressed && (data.Upload == "1") && (UploadPressedTimestamp == 0))
                {
                    UploadPressedTimestamp = DateTime.Now.Ticks;
                }
                if (data.Upload == "0")
                {
                    UploadPressedTimestamp = 0;
                }

                UploadPressed = data.Upload == "1";

                TimeSpan timespan = new(DateTime.Now.Ticks - UploadPressedTimestamp);
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

        // process the pin/unpin commands from the dcs event stream. these change the order of the window stack
        // to keep jafdtc always on top or allow it to be lowered into the background.
        //
        private void ProcessWindowStackCommand(DataReceiver.Data data)
        {
            bool isUpdateWindowLayer = false;
            if (data.ToggleJAFDTC == "1")
            {
                IsJAFDTCPinnedToTop = !IsJAFDTCPinnedToTop;
                isUpdateWindowLayer = true;
            }
            else if (!IsJAFDTCPinnedToTop && (data.ShowJAFDTC == "1"))
            {
                IsJAFDTCPinnedToTop = true;
                isUpdateWindowLayer = true;
            }
            else if (IsJAFDTCPinnedToTop && (data.HideJAFDTC == "1"))
            {
                IsJAFDTCPinnedToTop = false;
                isUpdateWindowLayer = true;
            }
            if (isUpdateWindowLayer)
            {
                Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    (Window.AppWindow.Presenter as OverlappedPresenter).IsAlwaysOnTop = IsJAFDTCPinnedToTop;
                    if (!IsJAFDTCPinnedToTop)
                    {
                        Window.AppWindow.MoveInZOrderAtBottom();
                    }
                });
            }
        }

        // process the increment and decrement commands from the dcs event stream. these change the currently selected
        // configuration and (optionally) inform the user of the new configuration.
        //
        private void ProcessIncrDecrCommands(DataReceiver.Data data)
        {
            if (!IncPressed && (data.Increment == "1"))
            {
                IncPressed = true;
            }
            else if (IncPressed && (data.Increment == "0"))
            {
                IncPressed = false;
                Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    Window.ConfigListPage?.PreviousConfiguration();
                });
            }

            if (!DecPressed && (data.Decrement == "1"))
            {
                DecPressed = true;
            }
            else if (DecPressed && (data.Decrement == "0"))
            {
                DecPressed = false;
                Window?.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    Window.ConfigListPage?.NextConfiguration();
                });
            }
        }

        // handle an inbound data packet from dcs. this packet provides information on which airframe is curerently
        // active, state of the dtc, and cockpit control state that we use to trigger dtc actions.
        //
        private void DataReceiver_DataReceived(DataReceiver.Data data)
        {
            if (Window != null)
            {
                DCSActiveAirframe = (_dcsToJAFDTCTypeMap.ContainsKey(data.Model)) ? _dcsToJAFDTCTypeMap[data.Model]
                                                                                  : AirframeTypes.None;

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

        // application launched: create the main window, set it up, and activate it to get this show on the road.
        //
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Window = new MainWindow();
            Window.Activated += MainWindow_Activated;
            Window.Activate();
        }

        // check dcs timer ticks: update IsDCSAvailable state based on lua installation, dcs process running, and
        // indications dcs export is running. force regeneration of dcs state.
        //
        private void CheckDCSTimer_Tick(object sender, object args)
        {
            _ = IsDCSAvailable;
        }

        // window activated: when a window is activated or deactivated, start or stop (respectively) the dcs state
        // check timer to monitor that state for the rest of the ui. force regeneration of dcs state.
        //
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
