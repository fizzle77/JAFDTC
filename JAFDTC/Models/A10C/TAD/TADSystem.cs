// ********************************************************************************************************************
//
// TADSystem.cs -- a-10c tad system
//
// Copyright(C) 2024 fizzle, JAFDTC contributors
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
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.TAD
{
    public enum CoordDisplayOptions
    {
        LL = 0,
        MGRS = 1,
        OFF = 2
    }

    public enum HookModeOptions
    {
        HookOwn = 0,
        OwnHook,
        HookBull,
        BullHook,
        HookCurs,
        CursHook,
        BullCurs,
        CursBull
    }

    // visibility options for everything but hook info
    public enum ProfileVisibilityOptions
    {
        On = 0,
        Off
    }

    // visibility options for hook info
    public enum HookInfoVisibilityOptions
    {
        On = 0,
        Active,
        Off
    }

    public enum RangeOptions
    {
        Range5 = 0,
        Range10,
        Range20,
        Range40,
        Range80,
        Range160
    }

    public enum MapOptions
    {
        AUTO = 0,
        OFF,
        MAN
    }

    public enum CenterDepressed
    {
        Center = 0,
        Depressed
    }

    public class TADSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:A10C:TAD";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- properties that post change and validation events

        private string _coordDisplay;
        public string CoordDisplay
        {
            get => _coordDisplay;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _coordDisplay, value, error);
            }
        }

        private string _grpID;
        public string GrpID
        {
            get => _grpID;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 99)) ? null : "Invalid format";
                SetProperty(ref _grpID, value, error);
            }
        }

        private string _ownID;
        public string OwnID
        {
            get => _ownID;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 1, 99)) ? null : "Invalid format";
                SetProperty(ref _ownID, value, error);
            }
        }

        private string _callsign;
        public string Callsign  // Exactly 4 characters, any characters the CDU allows
        {
            get => _callsign;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _callsigneRegex))
                    error = null;
                SetProperty(ref _callsign, value, error);
            }
        }
        private static readonly Regex _callsigneRegex = new(@"^[\s0-9a-zA-Z\.\/]{4}$");

        private string _hookOption;
        public string HookOption
        {
            get => _hookOption;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 7)) ? null : "Invalid format";
                SetProperty(ref _hookOption, value, error);
            }
        }

        private string _hookOwnship;
        public string HookOwnship
        {
            get => _hookOwnship;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsBooleanFieldValid(value)) ? null : "Invalid format";
                SetProperty(ref _hookOwnship, value, error);
            }
        }

        private string _mapRange;
        public string MapRange
        {
            get => _mapRange;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 5)) ? null : "Invalid format";
                SetProperty(ref _mapRange, value, error);
            }
        }

        private string _centerDepressed;
        public string CenterDepressed
        {
            get => _centerDepressed;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _centerDepressed, value, error);
            }
        }

        private string _mapOption;
        public string MapOption
        {
            get => _mapOption;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _mapOption, value, error);
            }
        }

        private string _profileBullseye;
        public string ProfileBullseye
        {
            get => _profileBullseye;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _profileBullseye, value, error);
            }
        }

        private string _profileRangeRings;
        public string ProfileRangeRings
        {
            get => _profileRangeRings;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _profileRangeRings, value, error);
            }
        }

        private string _profileHookInfo;
        public string ProfileHookInfo
        {
            get => _profileHookInfo;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _profileHookInfo, value, error);
            }
        }

        private string _profileWaypointLines;
        public string ProfileWaypointLines
        {
            get => _profileWaypointLines;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _profileWaypointLines, value, error);
            }
        }

        private string _profileWaypointLabel;
        public string ProfileWaypointLabel
        {
            get => _profileWaypointLabel;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _profileWaypointLabel, value, error);
            }
        }

        private string _profileWaypoints;
        public string ProfileWaypoints
        {
            get => _profileWaypoints;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _profileWaypoints, value, error);
            }
        }

        private string _profileSPIDisplay;
        public string ProfileSPIDisplay
        {
            get => _profileSPIDisplay;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _profileSPIDisplay, value, error);
            }
        }

        // ---- synthesized properties

        [JsonIgnore]
        public bool IsDefault => IsCoordDisplayDefault && IsGrpIDDefault && IsOwnIDDefault && IsCallsignDefault && IsHookOptionDefault
            && IsHookOwnshipDefault && IsMapRangeDefault && IsCenterDepressedDefault && IsMapOptionDefault && IsProfileBullseyeDefault
            && IsProfileRangeRingsDefault && IsProfileHookInfoDefault && IsProfileWaypointLinesDefault && IsProfileWaypointLabelDefault
            && IsProfileWaypointsDefault && IsProfileSPIDisplayDefault;

        [JsonIgnore]
        public bool IsCoordDisplayDefault => string.IsNullOrEmpty(CoordDisplay) || CoordDisplay == ExplicitDefaults.CoordDisplay;
        [JsonIgnore]
        public int CoordDisplayValue => string.IsNullOrEmpty(CoordDisplay) ? int.Parse(ExplicitDefaults.CoordDisplay) : int.Parse(CoordDisplay);

        [JsonIgnore]
        public bool IsGrpIDDefault => string.IsNullOrEmpty(GrpID) || GrpID == ExplicitDefaults.GrpID;

        [JsonIgnore]
        public bool IsOwnIDDefault => string.IsNullOrEmpty(OwnID) || OwnID == ExplicitDefaults.OwnID;

        [JsonIgnore]
        public bool IsCallsignDefault => string.IsNullOrEmpty(Callsign) || Callsign == ExplicitDefaults.Callsign;

        [JsonIgnore]
        public bool IsHookOptionDefault => string.IsNullOrEmpty(HookOption) || HookOption == ExplicitDefaults.HookOption;
        [JsonIgnore]
        public int HookOptionValue => string.IsNullOrEmpty(HookOption) ? int.Parse(ExplicitDefaults.HookOption) : int.Parse(HookOption);

        [JsonIgnore]
        public bool IsHookOwnshipDefault => string.IsNullOrEmpty(HookOwnship) || HookOwnship == ExplicitDefaults.HookOwnship;
        [JsonIgnore]
        public bool HookOwnshipValue => string.IsNullOrEmpty(HookOwnship) ? bool.Parse(ExplicitDefaults.HookOwnship) : bool.Parse(HookOwnship);

        [JsonIgnore]
        public bool IsMapRangeDefault => string.IsNullOrEmpty(MapRange) || MapRange == ExplicitDefaults.MapRange;
        [JsonIgnore]
        public int MapRangeValue => string.IsNullOrEmpty(MapRange) ? int.Parse(ExplicitDefaults.MapRange) : int.Parse(MapRange);

        [JsonIgnore]
        public bool IsCenterDepressedDefault => string.IsNullOrEmpty(CenterDepressed) || CenterDepressed == ExplicitDefaults.CenterDepressed;
        [JsonIgnore]
        public int CenterDepressedValue => string.IsNullOrEmpty(CenterDepressed) ? int.Parse(ExplicitDefaults.CenterDepressed) : int.Parse(CenterDepressed);

        [JsonIgnore]
        public bool IsMapOptionDefault => string.IsNullOrEmpty(MapOption) || MapOption == ExplicitDefaults.MapOption;
        [JsonIgnore]
        public int MapOptionValue => string.IsNullOrEmpty(MapOption) ? int.Parse(ExplicitDefaults.MapOption) : int.Parse(MapOption);

        [JsonIgnore]
        public bool IsProfileBullseyeDefault => string.IsNullOrEmpty(ProfileBullseye) || ProfileBullseye == ExplicitDefaults.ProfileBullseye;
        [JsonIgnore]
        public int ProfileBullseyeValue => string.IsNullOrEmpty(ProfileBullseye) ? int.Parse(ExplicitDefaults.ProfileBullseye) : int.Parse(ProfileBullseye);

        [JsonIgnore]
        public bool IsProfileRangeRingsDefault => string.IsNullOrEmpty(ProfileRangeRings) || ProfileRangeRings == ExplicitDefaults.ProfileRangeRings;
        [JsonIgnore]
        public int ProfileRangeRingsValue => string.IsNullOrEmpty(ProfileRangeRings) ? int.Parse(ExplicitDefaults.ProfileRangeRings) : int.Parse(ProfileRangeRings);

        [JsonIgnore]
        public bool IsProfileHookInfoDefault => string.IsNullOrEmpty(ProfileHookInfo) || ProfileHookInfo == ExplicitDefaults.ProfileHookInfo;
        [JsonIgnore]
        public int ProfileHookInfoValue => string.IsNullOrEmpty(ProfileHookInfo) ? int.Parse(ExplicitDefaults.ProfileHookInfo) : int.Parse(ProfileHookInfo);

        [JsonIgnore]
        public bool IsProfileWaypointLinesDefault => string.IsNullOrEmpty(ProfileWaypointLines) || ProfileWaypointLines == ExplicitDefaults.ProfileWaypointLines;
        [JsonIgnore]
        public int ProfileWaypointLinesValue => string.IsNullOrEmpty(ProfileWaypointLines) ? int.Parse(ExplicitDefaults.ProfileWaypointLines) : int.Parse(ProfileWaypointLines);

        [JsonIgnore]
        public bool IsProfileWaypointLabelDefault => string.IsNullOrEmpty(ProfileWaypointLabel) || ProfileWaypointLabel == ExplicitDefaults.ProfileWaypointLabel;
        [JsonIgnore]
        public int ProfileWaypointLabelValue => string.IsNullOrEmpty(ProfileWaypointLabel) ? int.Parse(ExplicitDefaults.ProfileWaypointLabel) : int.Parse(ProfileWaypointLabel);

        [JsonIgnore]
        public bool IsProfileWaypointsDefault => string.IsNullOrEmpty(ProfileWaypoints) || ProfileWaypoints == ExplicitDefaults.ProfileWaypoints;
        [JsonIgnore]
        public int ProfileWaypointsValue => string.IsNullOrEmpty(ProfileWaypoints) ? int.Parse(ExplicitDefaults.ProfileWaypoints) : int.Parse(ProfileWaypoints);

        [JsonIgnore]
        public bool IsProfileSPIDisplayDefault => string.IsNullOrEmpty(ProfileSPIDisplay) || ProfileSPIDisplay == ExplicitDefaults.ProfileSPIDisplay;
        [JsonIgnore]
        public int ProfileSPIDisplayValue => string.IsNullOrEmpty(ProfileSPIDisplay) ? int.Parse(ExplicitDefaults.ProfileSPIDisplay) : int.Parse(ProfileSPIDisplay);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public TADSystem()
        {
            Reset();
        }

        public TADSystem(TADSystem other)
        {
            CoordDisplay = other.CoordDisplay;
            GrpID = other.GrpID;
            OwnID = other.OwnID;
            Callsign = other.Callsign;
            HookOption = other.HookOption;
            HookOwnship = other.HookOwnship;
            MapRange = other.MapRange;
            CenterDepressed = other.CenterDepressed;
            MapOption = other.MapOption;
            ProfileBullseye = other.ProfileBullseye;
            ProfileRangeRings = other.ProfileRangeRings;
            ProfileHookInfo = other.ProfileHookInfo;
            ProfileWaypointLines = other.ProfileWaypointLines;
            ProfileWaypointLabel = other.ProfileWaypointLabel;
            ProfileWaypoints = other.ProfileWaypoints;
            ProfileSPIDisplay = other.ProfileSPIDisplay;
        }

        public virtual object Clone() => new TADSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // member methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public void Reset()
        {
            CoordDisplay = "0";
            GrpID = "";
            OwnID = "";
            Callsign = "";
            HookOption = "0";
            HookOwnship = "false";
            MapRange = "0";
            CenterDepressed = "0";
            MapOption = "0";
            ProfileBullseye = "0";
            ProfileRangeRings = "0";
            ProfileHookInfo = "0";
            ProfileWaypointLines = "0";
            ProfileWaypointLabel = "0";
            ProfileWaypoints = "0";
            ProfileSPIDisplay = "0";
        }


        // ------------------------------------------------------------------------------------------------------------
        //
        // static members
        //
        // ------------------------------------------------------------------------------------------------------------

        public readonly static TADSystem ExplicitDefaults = new()
        {
            CoordDisplay = "0",
            GrpID = "",
            OwnID = "",
            Callsign = "",
            HookOption = "0",
            HookOwnship = "false",
            MapRange = "0",
            CenterDepressed = "0",
            MapOption = "0",
            ProfileBullseye = "0",
            ProfileRangeRings = "0",
            ProfileHookInfo = "0",
            ProfileWaypointLines = "0",
            ProfileWaypointLabel = "0",
            ProfileWaypoints = "0",
            ProfileSPIDisplay = "0",
        };
    }
}
