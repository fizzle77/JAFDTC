// ********************************************************************************************************************
//
// SteerpointRefPoint.cs -- f-16c steerpoint r/b/e offset for oap, vip, vrp
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
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F16C.STPT
{
    public enum RefPointTypes
    {
        NONE = 0,
        OAP = 1,
        VIP = 2,
        VRP = 3,
    }

    /// <summary>
    /// reference point that specifies an offset (range, bearing, elevation) from a steerpoint for use in an oap,
    /// vip, or vrp. legal values for each of the fields depend on the reference point type.
    /// </summary>
    public class SteerpointRefPoint : BindableObject
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events.

        public RefPointTypes Type { get; set; }

        // ---- following properties post change and validation events.

        // oap: integer, 00000 on [0,99999) ft
        // vip: integer, 000000 on [0,486090] ft
        // vrp: decimal, 00.0 on [0.0, 80.0] nm

        private string _range;
        public string Range
        {
            get => _range;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (((Type == RefPointTypes.OAP) && IsIntegerFieldValid(value, 0, 99999)) ||
                    ((Type == RefPointTypes.VIP) && IsIntegerFieldValid(value, 0, 486090)))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                else if ((Type == RefPointTypes.VRP) && IsDecimalFieldValid(value, 0.0, 80.0))
                {
                    value = FixupDecimalField(value, "F1");
                    error = null;
                }
                SetProperty(ref _range, value, error);
            }
        }

        // decimal: 000.0 on [0.0, 359.9]

        private string _brng;
        public string Brng
        {
            get => _brng;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (IsDecimalFieldValid(value, 0.0, 359.0))
                {
                    value = FixupDecimalField(value, "F1");
                    error = null;
                }
                SetProperty(ref _brng, value, error);
            }
        }

        // oap: integer, on [-1500, 80000]
        // vip: integer, on [-99999, 99999]
        // vrp: integer, on [-99999, 99999]

        private string _elev;
        public string Elev
        {
            get => _elev;
            set
            {
                string error = (string.IsNullOrEmpty(value)) ? null : "Invalid format";
                if (((Type == RefPointTypes.OAP) && IsIntegerFieldValid(value, -1500, 80000)) ||
                    ((Type == RefPointTypes.VRP) && IsIntegerFieldValid(value, -99999, 99999)) ||
                    ((Type == RefPointTypes.VIP) && IsIntegerFieldValid(value, -99999, 99999)))
                {
                    value = FixupIntegerField(value);
                    error = null;
                }
                SetProperty(ref _elev, value, error);
            }
        }

        // ---- following properties are synthesized from other properties

        [JsonIgnore]
        public bool IsZeroOffset => (string.IsNullOrEmpty(Range) && string.IsNullOrEmpty(Brng) && string.IsNullOrEmpty(Elev));

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SteerpointRefPoint()
            => (Type, Range, Brng, Elev) = (RefPointTypes.NONE, "", "", "");

        public SteerpointRefPoint(SteerpointRefPoint other)
            => (Type, Range, Brng, Elev) = (other.Type, new(other.Range), new(other.Brng), new(other.Elev));

        public virtual object Clone() => new SteerpointRefPoint(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // TODO: document
        public void Reset() => (Type, Range, Brng, Elev) = (RefPointTypes.NONE, "", "", "");
    }
}
