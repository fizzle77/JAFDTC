// ********************************************************************************************************************
//
// Settings.cs : jafdtc application settings
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;


namespace JAFDTC.Utilities
{
    /// <summary>
    /// class underlying settings data. an instance of this class is persisted to storage.
    /// </summary>
    public sealed class SettingsData
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VersionJAFDTC { get; set; }

        public bool IsSkipDCSLuaInstall { get; set; }
        
        public Dictionary<string, DCSLuaManager.DCSLuaVersion> VersionDCSLua { get; set; }

        public int LastAirframeSelection { get; set; }

        public int LastConfigSelection { get; set; }

        public string WingName { get; set; }

        public string Callsign { get; set; }

        public bool IsAlwaysOnTop { get; set; }

        public int TCPPortTx { get; set; }
        
        public int UDPPortRx { get; set; }

        public Dictionary<AirframeTypes, int> CommandDelaysMs { get; set; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SettingsData()
        {
            VersionJAFDTC = "";

            IsSkipDCSLuaInstall = false;
            VersionDCSLua = new Dictionary<string, DCSLuaManager.DCSLuaVersion>();

            LastAirframeSelection = 0;
            LastConfigSelection = -1;
            WingName = "";
            Callsign = "";
            IsAlwaysOnTop = false;

            // NOTE: these need to be kept in sync with corresponding port number values in Lua.
            //
            TCPPortTx = 42001;
            UDPPortRx = 42002;

            CommandDelaysMs = new Dictionary<AirframeTypes, int>()
            {
                [AirframeTypes.A10C] = 200,
                [AirframeTypes.AH64D] = 200,
                [AirframeTypes.AV8B] = 200,
                [AirframeTypes.F15E] = 80,
                [AirframeTypes.F16C] = 200,
                [AirframeTypes.FA18C] = 200,
                [AirframeTypes.M2000C] = 200,
                [AirframeTypes.F14AB] = 200,
            };
        }
    }
    
	/// <summary>
    /// TODO: document
    /// </summary>
    public static class Settings
	{
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public static bool IsVersionUpdated { get; set; }

        private static SettingsData _currentSettings = null;

        // ------------------------------------------------------------------------------------------------------------
        //
        // accessors for settings
        //
        // ------------------------------------------------------------------------------------------------------------
        
        public static string VersionJAFDTC
		{
			get => _currentSettings.VersionJAFDTC;
			set
			{
				_currentSettings.VersionJAFDTC = value;
                FileManager.WriteSettings(_currentSettings);
            }
        }

        public static bool IsSkipDCSLuaInstall
        {
            get => _currentSettings.IsSkipDCSLuaInstall;
            set
            {
                _currentSettings.IsSkipDCSLuaInstall = value;
                FileManager.WriteSettings(_currentSettings);
            }
        }

        public static Dictionary<string, DCSLuaManager.DCSLuaVersion> VersionDCSLua
        {
            get => _currentSettings.VersionDCSLua;
        }

        public static void SetVersionDCSLua(string key, DCSLuaManager.DCSLuaVersion version)
        {
            _currentSettings.VersionDCSLua[key] = version;
            FileManager.WriteSettings(_currentSettings);
        }

        public static string WingName
        {
            get => _currentSettings.WingName;
            set
            {
                _currentSettings.WingName = value;
                FileManager.WriteSettings(_currentSettings);
            }
        }

        public static string Callsign
        {
            get => _currentSettings.Callsign;
            set
            {
                _currentSettings.Callsign = value;
                FileManager.WriteSettings(_currentSettings);
            }
        }

        public static bool IsAlwaysOnTop
        {
            get => _currentSettings.IsAlwaysOnTop;
            set
            {
                _currentSettings.IsAlwaysOnTop = value;
                FileManager.WriteSettings(_currentSettings);
            }
        }

        public static int LastAirframeSelection
        {
            get => _currentSettings.LastAirframeSelection;
            set
            {
                _currentSettings.LastAirframeSelection = value;
                FileManager.WriteSettings(_currentSettings);
            }
        }

        public static int LastConfigSelection
        {
            get => _currentSettings.LastConfigSelection;
            set
            {
                _currentSettings.LastConfigSelection = value;
                FileManager.WriteSettings(_currentSettings);
            }
        }

        public static int TCPPortTx
		{
			get => _currentSettings.TCPPortTx;
			set
			{
				_currentSettings.TCPPortTx = value;
                FileManager.WriteSettings(_currentSettings);
            }
        }

		public static int UDPPortRx
		{
			get => _currentSettings.UDPPortRx;
			set
			{
				_currentSettings.UDPPortRx = value;
                FileManager.WriteSettings(_currentSettings);
            }
        }

        public static Dictionary<AirframeTypes, int> CommandDelaysMs
		{
			get => _currentSettings.CommandDelaysMs;
		}

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // TODO: document
        public static void Preflight()
        {
            Settings.IsVersionUpdated = false;

            _currentSettings = FileManager.ReadSettings();

            // TODO: handle settings version updates here if necessary...

            if (Settings.VersionJAFDTC != Globals.VersionJAFDTC)
            {
                Settings.IsVersionUpdated = true;
                Settings.IsSkipDCSLuaInstall = false;
            }
            Settings.VersionJAFDTC = VersionJAFDTC;
        }
	}
}
