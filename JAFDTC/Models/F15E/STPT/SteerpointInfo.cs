// ********************************************************************************************************************
//
// SteerpointInfo.cs -- f-15e steerpoint point
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

using JAFDTC.Models.Base;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F15E.STPT
{
    /// <summary>
    /// mudhen steerpoint system steerpoint. this class extends the base navigation point (NavpointBase) with support
    /// for mudhen lat/lon format, target/initial points, reference points, and tot. steerpoints may be typed as
    /// "target points" or plain steerpoints. as in base navpoints, the ui views of the lat/lon (LatUI/LonUI) are
    /// layered on top of the persisted DD format lat/lon (Lat/Lon).
    /// </summary>
    public class SteerpointInfo : NavpointInfoBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties, static

        // TODO: check TOT format in mudhen
        private static readonly Regex _totRegex = new(@"^([0-1][0-9]\:[0-5][0-9]\:[0-5][0-9])|(2[0-3]\:[0-5][0-9]\:[0-5][0-9])$");

        // ---- public properties

        public List<RefPointInfo> RefPoints { get; set; }

        [JsonIgnore]
        private bool _isInitialUI;                  // ui-utilized property, not persisted
        [JsonIgnore]
        public bool IsInitialUI
        {
            get => _isInitialUI;
            set
            {
                if (value)
                {
                    StptGlyphUI = "\xF16B";
                }
                else
                {
                    StptGlyphUI = (IsTarget) ? "\xE879" : "\xF138";
                }
                _isInitialUI = value;
            }
        }

        // ---- public properties, posts change/validation events

        [JsonIgnore]
        private string _route;                      // string route identifier: [ABC]
        public string Route
        {
            get => _route;
            //
            // NOTE: route should only be set by code, so no need for validation here...
            //
            set => SetProperty(ref _route, value);
        }

        [JsonIgnore]
        private string _latUI;                      // string, DDM "[N|S] 00° 00.000’"
        [JsonIgnore]
        public override string LatUI
        {
            get => Coord.ConvertFromLatDD(Lat, LLFormat.DDM_P3ZF);
            set
            {
                string error = "Invalid latitude DDM format";
                if (IsRegexFieldValid(value, Coord.LatRegexFor(LLFormat.DDM_P3ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = Coord.ConvertToLatDD(value, LLFormat.DDM_P3ZF);
                SetProperty(ref _latUI, value, error);
            }
        }

        [JsonIgnore]
        private string _lonUI;                      // string, DDM "[E|W] 000° 00.000’"
        [JsonIgnore]
        public override string LonUI
        {
            get => Coord.ConvertFromLonDD(Lon, LLFormat.DDM_P3ZF);
            set
            {
                string error = "Invalid longitude DDM format";
                if (IsRegexFieldValid(value, Coord.LonRegexFor(LLFormat.DDM_P3ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = Coord.ConvertToLonDD(value, LLFormat.DDM_P3ZF);
                SetProperty(ref _lonUI, value, error);
            }
        }

        // NOTE: override Alt to enforce mudhen valid range.

        public override string Alt                 // integer, on [1, 59999] or [-59999, -1]
        {
            get => _alt;
            set
            {
                string error = "Invalid altitude format";
                if (IsIntegerFieldValid(value, 1, 59999, false) || IsIntegerFieldValid(value, -59999, -1, false))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alt, value, error);
            }
        }

        // TODO: check TOT format in mudhen
        private string _tot;                        // "HH:MM:SS", HH = [00,24), MM on [00,59), SS on [00,59)
        public string TOT
        {
            get => _tot;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid TOS format";
                if (IsRegexFieldValid(value, _totRegex))
                {
                    error = null;
                }
                SetProperty(ref _tot, value, error);
            }
        }

        private bool _isTarget;
        public bool IsTarget
        {
            get => _isTarget;
            set
            {
                StptGlyphUI = (value) ? "\xE879" : ((IsInitialUI) ? "\xF16B" : "\xF138");
                SetProperty(ref _isTarget, value);
            }
        }

        // ---- public properties, computed

        [JsonIgnore]
        public string RefPtUI                       // ui-utilized property, not persisted
        {
            get
            {
                int count = 0;
                foreach (RefPointInfo info in RefPoints)
                {
                    if (info.IsValid)
                    {
                        count++;
                    }
                }
                return (count > 0) ? count.ToString() : "–";
            }
        }

        [JsonIgnore]
        private string _stptGlyphUI;                // ui-utilized property, not persisted
        [JsonIgnore]
        public string StptGlyphUI
        {
            get => _stptGlyphUI;
            set => SetProperty(ref _stptGlyphUI, value);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SteerpointInfo()
        {
            Route = "A";
            Number = 1;
            TOT = "";
            RefPoints = new();
            IsTarget = false;
        }

        public SteerpointInfo(SteerpointInfo other)
        {
            Route = new(other.Route);
            Number = other.Number;
            Name = new(other.Name);
            Lat = new(other.Lat);
            Lon = new(other.Lon);
            Alt = new(other.Alt);
            TOT = new(other.TOT);
            RefPoints = new(other.RefPoints);
            IsTarget = other.IsTarget;
        }

        public virtual object Clone() => new SteerpointInfo(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the steerpoint to default values. the Number and route fields are not changed.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            TOT = "";
            RefPoints.Clear();
            IsTarget = false;
        }
    }
}
