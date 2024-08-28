// ********************************************************************************************************************
//
// Settings.cs : jafdtc application settings
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

using JAFDTC.Models.DCS;
using System.Collections.Generic;
using System.Diagnostics;

using static JAFDTC.Models.Base.NavpointInfoBase;
using static JAFDTC.Utilities.SettingsData;

namespace JAFDTC.Utilities
{
    /// <summary>
    /// class underlying jafdtc settings data. an instance of this class is serialized/deserialized to storage to
    /// persist the settings.
    /// </summary>
    public sealed class SettingsData
    {
        /// <summary>
        /// types of upload feedback.
        /// </summary>
        public enum UploadFeedbackTypes
        {
            AUDIO = 0,
            AUDIO_DONE = 1,
            AUDIO_PROGRESS = 2,
            AUDIO_LIGHTS = 3,
            LIGHTS = 4,
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string VersionJAFDTC { get; set; }

        public string SkipJAFDTCVersion { get; set; }

        public bool IsSkipDCSLuaInstall { get; set; }

        public Dictionary<string, DCSLuaManager.DCSLuaVersion> VersionDCSLua { get; }

        public int LastAirframeSelection { get; set; }

        public int LastConfigSelection { get; set; }

        public string LastStptFilterTheater { get; set; }

        public string LastStptFilterCampaign { get; set; }

        public string LastStptFilterTags { get; set; }

        public PointOfInterestTypeMask LastStptFilterIncludeTypes { get; set; }

        public string LastPoIFilterTheater { get; set; }

        public string LastPoIFilterCampaign { get; set; }

        public string LastPoIFilterTags { get; set; }

        public PointOfInterestTypeMask LastPoIFilterIncludeTypes { get; set; }

        public LLFormat LastPoICoordFmtSelection { get; set; }

        public string WingName { get; set; }

        public string Callsign { get; set; }

        public UploadFeedbackTypes UploadFeedback { get; set; }

        public bool IsNavPtImportIgnoreAirframe { get; set; }

        public bool IsAlwaysOnTop { get; set; }

        public bool IsNewVersCheckDisabled { get; set; }

        public int TCPPortCmdTx { get; }

        public int TCPPortCfgNameTx { get; }

        public int UDPPortTelRx { get; }

        public int UDPPortCapRx { get; }

        public Dictionary<AirframeTypes, int> CommandDelaysMs { get; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SettingsData()
        {
            VersionJAFDTC = "";
            SkipJAFDTCVersion = "";

            IsSkipDCSLuaInstall = false;
            VersionDCSLua = new Dictionary<string, DCSLuaManager.DCSLuaVersion>();

            LastAirframeSelection = 0;
            LastConfigSelection = -1;
            LastStptFilterTheater = "";
            LastStptFilterCampaign = "";
            LastStptFilterTags = "";
            LastStptFilterIncludeTypes = PointOfInterestTypeMask.ANY;
            LastPoIFilterTheater = "";
            LastPoIFilterCampaign = "";
            LastPoIFilterTags = "";
            LastPoIFilterIncludeTypes = PointOfInterestTypeMask.ANY;
            LastPoICoordFmtSelection = LLFormat.DDM_P3ZF;

            WingName = "";
            Callsign = "";
            UploadFeedback = UploadFeedbackTypes.AUDIO;
            IsNavPtImportIgnoreAirframe = false;
            IsAlwaysOnTop = false;
            IsNewVersCheckDisabled = false;

            // NOTE: the tx/rx ports need to be kept in sync with corresponding port numbers in Lua and cannot be
            // NOTE: changed without corresponding changes to the Lua files. they are readonly here.
            //
            TCPPortCmdTx = 42001;               // clickable cockpit commands to dcs
            UDPPortTelRx = 42002;               // telemetry from dcs
            UDPPortCapRx = 42003;               // waypoint capture from dcs
            TCPPortCfgNameTx = 42004;           // configuration name to dcs

            CommandDelaysMs = new Dictionary<AirframeTypes, int>()
            {
                [AirframeTypes.A10C] = 200,
                [AirframeTypes.AH64D] = 200,
                [AirframeTypes.AV8B] = 200,
                [AirframeTypes.F14AB] = 200,
                [AirframeTypes.F15E] = 200,
                [AirframeTypes.F16C] = 200,
                [AirframeTypes.FA18C] = 200,
                [AirframeTypes.M2000C] = 200,
            };
        }
    }
    
	/// <summary>
    /// wrapper class to provide access to jafdtc settings to interested parties. prior to any access to the settings,
    /// the Preflight() function should be called.
    /// </summary>
    public static class Settings
	{
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public static bool IsVersionUpdated { get; set; }

        // ---- private properties

        private static SettingsData _currentSettings = null;

        // ------------------------------------------------------------------------------------------------------------
        //
        // accessors for settings
        //
        // ------------------------------------------------------------------------------------------------------------

        // these accessors wrap the current settings object we track here. set accessors implicitly update the
        // persistent settings file via FileManager.WriteSettings() on changes.

        public static string VersionJAFDTC
        {
            get => _currentSettings.VersionJAFDTC;
            set
            {
                if (_currentSettings.VersionJAFDTC != value)
                {
                    _currentSettings.VersionJAFDTC = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static string SkipJAFDTCVersion
        {
            get => _currentSettings.SkipJAFDTCVersion;
            set
            {
                if (_currentSettings.SkipJAFDTCVersion != value)
                {
                    _currentSettings.SkipJAFDTCVersion = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static bool IsSkipDCSLuaInstall
        {
            get => _currentSettings.IsSkipDCSLuaInstall;
            set
            {
                if (_currentSettings.IsSkipDCSLuaInstall != value)
                {
                    _currentSettings.IsSkipDCSLuaInstall = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static Dictionary<string, DCSLuaManager.DCSLuaVersion> VersionDCSLua
        {
            get => _currentSettings.VersionDCSLua;
        }

        public static void SetVersionDCSLua(string key, DCSLuaManager.DCSLuaVersion version)
        {
            if (!_currentSettings.VersionDCSLua.ContainsKey(key) || (_currentSettings.VersionDCSLua[key] != version))
            {
                _currentSettings.VersionDCSLua[key] = version;
                FileManager.WriteSettings(_currentSettings);
            }
        }

        public static string WingName
        {
            get => _currentSettings.WingName;
            set
            {
                if (_currentSettings.WingName != value)
                {
                    _currentSettings.WingName = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static string Callsign
        {
            get => _currentSettings.Callsign;
            set
            {
                if (_currentSettings.Callsign != value)
                {
                    _currentSettings.Callsign = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static UploadFeedbackTypes UploadFeedback
        {
            get => _currentSettings.UploadFeedback;
            set
            {
                if (_currentSettings.UploadFeedback != value)
                {
                    _currentSettings.UploadFeedback = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static bool IsNavPtImportIgnoreAirframe
        {
            get => _currentSettings.IsNavPtImportIgnoreAirframe;
            set
            {
                if (_currentSettings.IsNavPtImportIgnoreAirframe != value)
                {
                    _currentSettings.IsNavPtImportIgnoreAirframe = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static bool IsAlwaysOnTop
        {
            get => _currentSettings.IsAlwaysOnTop;
            set
            {
                if (_currentSettings.IsAlwaysOnTop != value)
                {
                    _currentSettings.IsAlwaysOnTop = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static bool IsNewVersCheckDisabled
        {
            get => _currentSettings.IsNewVersCheckDisabled;
            set
            {
                if (_currentSettings.IsNewVersCheckDisabled != value)
                {
                    _currentSettings.IsNewVersCheckDisabled = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }
        
        public static int LastAirframeSelection
        {
            get => _currentSettings.LastAirframeSelection;
            set
            {
                if (_currentSettings.LastAirframeSelection != value)
                {
                    _currentSettings.LastAirframeSelection = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static int LastConfigSelection
        {
            get => _currentSettings.LastConfigSelection;
            set
            {
                if (_currentSettings.LastConfigSelection != value)
                {
                    _currentSettings.LastConfigSelection = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static string LastStptFilterTheater
        {
            get => _currentSettings.LastStptFilterTheater;
            set
            {
                if (_currentSettings.LastStptFilterTheater != value)
                {
                    _currentSettings.LastStptFilterTheater = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static string LastStptFilterCampaign
        {
            get => _currentSettings.LastStptFilterCampaign;
            set
            {
                if (_currentSettings.LastStptFilterCampaign != value)
                {
                    _currentSettings.LastStptFilterCampaign = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static string LastStptFilterTags
        {
            get => _currentSettings.LastStptFilterTags;
            set
            {
                if (_currentSettings.LastStptFilterTags != value)
                {
                    _currentSettings.LastStptFilterTags = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static PointOfInterestTypeMask LastStptFilterIncludeTypes
        {
            get => _currentSettings.LastStptFilterIncludeTypes;
            set
            {
                if (_currentSettings.LastStptFilterIncludeTypes != value)
                {
                    _currentSettings.LastStptFilterIncludeTypes = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static string LastPoIFilterTheater
        {
            get => _currentSettings.LastPoIFilterTheater;
            set
            {
                if (_currentSettings.LastPoIFilterTheater != value)
                {
                    _currentSettings.LastPoIFilterTheater = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static string LastPoIFilterCampaign
        {
            get => _currentSettings.LastPoIFilterCampaign;
            set
            {
                if (_currentSettings.LastPoIFilterCampaign != value)
                {
                    _currentSettings.LastPoIFilterCampaign = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static string LastPoIFilterTags
        {
            get => _currentSettings.LastPoIFilterTags;
            set
            {
                if (_currentSettings.LastPoIFilterTags != value)
                {
                    _currentSettings.LastPoIFilterTags = value;
                    FileManager.WriteSettings(_currentSettings);
                }
}
        }

        public static PointOfInterestTypeMask LastPoIFilterIncludeTypes
        {
            get => _currentSettings.LastPoIFilterIncludeTypes;
            set
            {
                if (_currentSettings.LastPoIFilterIncludeTypes != value)
                {
                    _currentSettings.LastPoIFilterIncludeTypes = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static LLFormat LastPoICoordFmtSelection
        {
            get => _currentSettings.LastPoICoordFmtSelection;
            set
            {
                if (_currentSettings.LastPoICoordFmtSelection != value)
                {
                    _currentSettings.LastPoICoordFmtSelection = value;
                    FileManager.WriteSettings(_currentSettings);
                }
            }
        }

        public static int TCPPortCmdTx
        {
            get => _currentSettings.TCPPortCmdTx;
        }

        public static int TCPPortCfgNameTx
        {
            get => _currentSettings.TCPPortCfgNameTx;
        }

        public static int UDPPortTelRx
        {
            get => _currentSettings.UDPPortTelRx;
        }

        public static int UDPPortCapRx
        {
            get => _currentSettings.UDPPortCapRx;
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

        /// <summary>
        /// prepare the settings manager for use in the application by reading the settings from the settings file.
        /// if the version has changed, make a note so we can report the update later. this function is typically
        /// called exactly once prior to any access to the settings.
        /// </summary>
        public static void Preflight()
        {
            Settings.IsVersionUpdated = false;

            _currentSettings = FileManager.ReadSettings();

            if (Settings.VersionJAFDTC != Globals.VersionJAFDTC)
            {
                // TODO: handle any updates to the settings necessitated by the version change

                Settings.IsVersionUpdated = true;
                Settings.IsSkipDCSLuaInstall = false;
                Settings.SkipJAFDTCVersion = "";
                Settings.VersionJAFDTC = Globals.VersionJAFDTC;
            }
        }
	}
}
