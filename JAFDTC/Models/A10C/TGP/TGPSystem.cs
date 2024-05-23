// ********************************************************************************************************************
//
// HMCSSystem.cs -- a-10c tgp system
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
// Copyright(C) 2024 fizzle
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

namespace JAFDTC.Models.A10C.TGP
{
    // defines the coordinate display options
    //
    public enum CoordDisplayOptions
    {
        LL = 0,
        MGRS = 1,
        OFF = 2
    }

    // defines the video mode options
    //
    public enum VideoModeOptions
    {
        CCD = 0,
        WHOT = 1,
        BHOT = 2
    }

    // defines the laser latch options
    //
    public enum LatchOptions
    {
        ON = 0, // counterintuitive to have ON be 0, but ON is the default
        OFF = 1
    }

    public class TGPSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:A10C:TGP";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- properties that post change and validation events

        private string _coordDisplay;                              // integer [0, 2]
        public string CoordDisplay
        {
            get => _coordDisplay;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _coordDisplay, value, error);
            }
        }

        private string _videoMode;                              // integer [0, 2]
        public string VideoMode
        {
            get => _videoMode;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 2)) ? null : "Invalid format";
                SetProperty(ref _videoMode, value, error);
            }
        }

        private string _laserCode;                           // laser code, [1][1-7][1-8][1-8]
        public string LaserCode
        {
            get => _laserCode;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _laserCodeRegex))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _laserCode, value, error);
            }
        }
        private static readonly Regex _laserCodeRegex = new(@"^1[1-7][1-8][1-8]$");

        private string _LSS;                           // laser code, [1][1-7][1-8][1-8]
        public string LSS
        {
            get => _LSS;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsRegexFieldValid(value, _laserCodeRegex))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _LSS, value, error);
            }
        }

        private string _latch;                              // integer [0, 1]
        public string Latch
        {
            get => _latch;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _latch, value, error);
            }
        }

        // ---- synthesized properties

        [JsonIgnore]
        public bool IsDefault => throw new System.NotImplementedException();

        [JsonIgnore]
        public bool CoordDisplayIsDefault => string.IsNullOrEmpty(CoordDisplay) || CoordDisplay == ExplicitDefaults.CoordDisplay;

        [JsonIgnore]
        public bool VideoModeIsDefault => string.IsNullOrEmpty(VideoMode) || VideoMode == ExplicitDefaults.VideoMode;

        [JsonIgnore]
        public bool LaserCodeIsDefault => string.IsNullOrEmpty(LaserCode) || LaserCode == ExplicitDefaults.LaserCode;

        [JsonIgnore]
        public bool LSSIsDefault => string.IsNullOrEmpty(LSS) || LSS == ExplicitDefaults.LSS;

        [JsonIgnore]
        public bool LatchIsDefault => string.IsNullOrEmpty(Latch) || LSS == ExplicitDefaults.Latch;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public TGPSystem()
        {
            Reset();
        }

        public TGPSystem(TGPSystem other)
        {
            CoordDisplay = other.CoordDisplay;
            VideoMode = other.VideoMode;
            LaserCode = other.LaserCode;
            LSS = other.LSS;
            Latch = other.Latch;
        }

        public virtual object Clone() => new TGPSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // member methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public void Reset()
        {
            CoordDisplay = "0";  // LL
            VideoMode = "0";    // CCD
            LaserCode = "1688";
            LSS = "1688";
            Latch = "0";        // ON
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // static members
        //
        // ------------------------------------------------------------------------------------------------------------

        public readonly static TGPSystem ExplicitDefaults = new()
        {
            CoordDisplay = "0",  // LL
            VideoMode = "0",    // CCD
            LaserCode = "1688",
            LSS = "1688",
            Latch = "0",        // ON
        };
    }
}
