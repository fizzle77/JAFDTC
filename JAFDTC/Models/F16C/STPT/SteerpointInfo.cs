// ********************************************************************************************************************
//
// SteerpointInfo.cs -- f-16c steerpoint base information
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

using JAFDTC.Models.Base;
using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F16C.STPT
{
    /// <summary>
    /// viper steerpoint system steerpoint. this class extends the base navigation point (NavpointBase) with support
    /// for viper lat/lon format, tos, and offset points. as in base navpoints, the ui views of the lat/lon
    /// (LatUI/LonUI) are layered on top of the persisted DD format lat/lon (Lat/Lon).
    /// </summary>
    public class SteerpointInfo : NavpointInfoBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties, static

        private static readonly Regex _tosRegex = new(@"^([0-1][0-9]\:[0-5][0-9]\:[0-5][0-9])|(2[0-3]\:[0-5][0-9]\:[0-5][0-9])$");

        // ---- public properties

        public SteerpointRefPoint[] OAP { get; set; }

        // for VIP: [0] vip to target, [1] vip to pup
        // for VRP: [0] target to vrp, [1] target to pup
        //
        public SteerpointRefPoint[] VxP { get; set; }

        // ---- public properties, posts change/validation events

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

        public override string Alt                  // positive integer, on [-1500, 80000]
        {
            get => _alt;
            set
            {
                string error = "Invalid altitude format";
                if (IsIntegerFieldValid(value, -1500, 80000, false))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _alt, value, error);
            }
        }

        private string _tos;                        // "HH:MM:SS", HH = [00,24), MM on [00,59), SS on [00,59)
        public string TOS
        {
            get => _tos;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid TOS format";
                if (IsRegexFieldValid(value, _tosRegex))
                {
                    error = null;
                }
                SetProperty(ref _tos, value, error);
            }
        }

        // ---- public properties, computed

        [JsonIgnore]
        public string RefPtGlyph => (((OAP[0].Type == RefPointTypes.OAP) ||
                                      (OAP[1].Type == RefPointTypes.OAP)) ? "\xE879 " : "") +       // triangle
                                    ((VxP[0].Type == RefPointTypes.VIP) ? "\xF138 " : "") +         // circle
                                    ((VxP[0].Type == RefPointTypes.VRP) ? "\xF16B " : "");          // square

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SteerpointInfo()
        {
            Number = 1;
            TOS = "";
            OAP = new SteerpointRefPoint[2];
            VxP = new SteerpointRefPoint[2];
            for (int i = 0; i < OAP.Length; i++)
            {
                OAP[i] = new SteerpointRefPoint();
                VxP[i] = new SteerpointRefPoint();
            }
        }

        public SteerpointInfo(SteerpointInfo other)
        {
            Number = other.Number;
            Name = new(other.Name);
            Lat = new(other.Lat);
            Lon = new(other.Lon);
            Alt = new(other.Alt);
            TOS = new(other.TOS);
            OAP = new SteerpointRefPoint[2];
            VxP = new SteerpointRefPoint[2];
            for (int i = 0; i < OAP.Length; i++)
            {
                OAP[i] = new(other.OAP[i]);
                VxP[i] = new(other.VxP[i]);
            }
        }

        public virtual object Clone() => new SteerpointInfo(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the steerpoint to default values. the Number field is not changed.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            TOS = "";
            for (int i = 0; i < OAP.Length; i++)
            {
                OAP[i].Reset();
                VxP[i].Reset();
            }

            // set accessors treat "" as illegal. to work around the way SetProperty() handles updating backing store
            // and error state, first set the fields to "" with no error to set backing store. then, use the set
            // accessor with a known bad to set error state (which will not update backing store).
            //
            SetProperty(ref _latUI, "", null, nameof(LatUI));
            SetProperty(ref _lonUI, "", null, nameof(LonUI));

            LatUI = null;
            LonUI = null;
        }
    }
}
