// ********************************************************************************************************************
//
// SteerpointInfo.cs -- f-16c steerpoint base information
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

using JAFDTC.Models.Base;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F16C.STPT
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class SteerpointInfo : NavpointInfoBase
    {
        private static readonly Regex timeRegex = new(@"^([0-1][0-9]\:[0-5][0-9]\:[0-5][0-9])|(2[0-3]\:[0-5][0-9]\:[0-5][0-9])$");

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events.

        public SteerpointRefPoint[] OAP { get; set; }

        public SteerpointRefPoint[] VxP { get; set; }

        // ---- following properties post change and validation events.

        [JsonIgnore]
        private string _latUI;                      // string, DDM "[N|S] 00° 00.000’"
        [JsonIgnore]
        public override string LatUI
        {
            get => ConvertFromLatDD(Lat, LLFormat.DDM_P3ZF);
            set
            {
                string error = "Invalid latitude DDM format";
                if (IsRegexFieldValid(value, LatRegexFor(LLFormat.DDM_P3ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lat = ConvertToLatDD(value, LLFormat.DDM_P3ZF);
                SetProperty(ref _latUI, value, error);
            }
        }

        [JsonIgnore]
        private string _lonUI;                      // string, DDM "[E|W] 000° 00.000’"
        [JsonIgnore]
        public override string LonUI
        {
            get => ConvertFromLonDD(Lon, LLFormat.DDM_P3ZF);
            set
            {
                string error = "Invalid longitude DDM format";
                if (IsRegexFieldValid(value, LonRegexFor(LLFormat.DDM_P3ZF), false))
                {
                    value = value.ToUpper();
                    error = null;
                }
                Lon = ConvertToLonDD(value, LLFormat.DDM_P3ZF);
                SetProperty(ref _lonUI, value, error);
            }
        }

        private string _tos;                        // "HH:MM:SS", HH = [00,24), MM on [00,59), SS on [00,59)
        public string TOS
        {
            get => _tos;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid TOS format";
                if (IsRegexFieldValid(value, timeRegex))
                {
                    error = null;
                }
                SetProperty(ref _tos, value, error);
            }
        }

        // ---- following properties are synthesized from other properties

#if NOPE
        [JsonIgnore]
        public override string Location => ((string.IsNullOrEmpty(Lat)) ? "Unknown" : LatUI) + ", " +
                                           ((string.IsNullOrEmpty(Lon)) ? "Unknown" : LonUI) + ", " +
                                           ((string.IsNullOrEmpty(Alt)) ? "Unknown" : Alt);
#endif

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

        // reset the steerpoint to default values. the Number field is not changed.
        //
        public override void Reset()
        {
            base.Reset();
            TOS = "";
            for (int i = 0; i < OAP.Length; i++)
            {
                OAP[i].Reset();
                VxP[i].Reset();
            }
        }
    }
}
