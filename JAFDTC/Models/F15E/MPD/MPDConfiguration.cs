// ********************************************************************************************************************
//
// MPDConfiguration.cs -- f-15e mpd/mpcd configuration
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

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F15E.MPD
{
    /// <summary>
    /// TODO
    /// </summary>
    public class MPDConfiguration
    {
        // master modes. this enum is used to index internal arrays and should be sequential.
        //
        public enum MasterModes
        {
            NONE = 0,
            A2G = 1,
            A2A = 2,
            NAV = 3,
        }

        // display formats. this enum is used to index internal arrays and should be sequential.
        //
        public enum DisplayFormats
        {
            NONE = 0,
            AA_RDR = 1,
            AG_RDR = 2,
            AG_DLVRY = 3,
            ADI = 4,
            ARMT = 5,
            ENG = 6,
            HSI = 7,
            HUD = 8,
            SMRT_WPNS = 9,
            TEWS = 10,
            TF = 11,
            TPOD = 12,
            TSD = 13,
        }

        public const int NUM_SEQUENCES = 3;                     // number of sequences.

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events.

        public string[] Formats { get; set; }                   // per sequence, values MPDConfiguration.DisplayFormats
        public string[] Modes { get; set; }                     // per sequence, values MPDConfiguration.MasterModes

        // ---- following properties are synthesized.

        // returns true if the instance indicates a default setup (all fields are "" or "0"), false otherwise.
        // note that we don't check modes here as mode[i] is only valid if format[i] is non-default.
        //
        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                for (int i = 0; i < NUM_SEQUENCES; i++)
                {
                    if (!string.IsNullOrEmpty(Formats[i]) && (Formats[i] != ((int)DisplayFormats.NONE).ToString()))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MPDConfiguration()
        {
            Formats = new string[NUM_SEQUENCES];
            Modes = new string[NUM_SEQUENCES];
            Reset();
        }

        public MPDConfiguration(MPDConfiguration other)
        {
            Formats = new string[3]
            {
                new(other.Formats[0]),
                new(other.Formats[1]),
                new(other.Formats[2])
            };
            Modes = new string[3]
            {
                new(other.Modes[0]),
                new(other.Modes[1]),
                new(other.Modes[2])
            };
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of null or "" implies default).
        //
        public void Reset()
        {
            for (int i = 0; i < NUM_SEQUENCES; i++)
            {
                Formats[i] = "";
                Modes[i] = "";
            }
        }
    }
}
