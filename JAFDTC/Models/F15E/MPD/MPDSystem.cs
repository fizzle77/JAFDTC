// ********************************************************************************************************************
//
// MPDSystem.cs -- f-15e mpd/mpcd system
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

using JAFDTC.Utilities;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.F15E.MPD
{
    public class MPDSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:F15E:MPD";

        // mpd/mpcd displays. this enum is used to index internal arrays and should be sequential.
        //
        public enum CockpitDisplays
        {
            PILOT_L_MPD = 0,
            PILOT_MPCD = 1,
            PILOT_R_MPD = 2,

            WSO_L_MPCD = 3,
            WSO_L_MPD = 4,
            WSO_R_MPD = 5,
            WSO_R_MPCD = 6,

            NUM_DISPLAYS = 7
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events.

        public MPDConfiguration[] Displays { get; set; }

        // ---- public properties, computed

        /// <summary>
        /// returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                for (int i = 0; i < (int)CockpitDisplays.NUM_DISPLAYS; i++)
                {
                    if (!Displays[i].IsDefault)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// returns true if the setup for the indicated crew member is default, false otherwise.
        /// </summary>
        public bool IsCrewMemberDefault(F15EConfiguration.CrewPositions member)
        {
            int min = (member == F15EConfiguration.CrewPositions.PILOT) ? (int)CockpitDisplays.PILOT_L_MPD
                                                                        : (int)CockpitDisplays.WSO_L_MPCD;
            int max = (member == F15EConfiguration.CrewPositions.PILOT) ? (int)CockpitDisplays.PILOT_R_MPD
                                                                        : (int)CockpitDisplays.WSO_R_MPCD;
            for (int i = min; i <= max; i++)
            {
                if (!Displays[i].IsDefault)
                {
                    return false;
                }
            }
            return true;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MPDSystem()
        {
            Displays = new MPDConfiguration[(int)CockpitDisplays.NUM_DISPLAYS];
            for (int i = 0; i < (int)CockpitDisplays.NUM_DISPLAYS; i++)
            {
                Displays[i] = new MPDConfiguration();
            }
        }

        public MPDSystem(MPDSystem other)
        {
            Displays = new MPDConfiguration[(int)CockpitDisplays.NUM_DISPLAYS];
            for (int i = 0; i < (int)CockpitDisplays.NUM_DISPLAYS; i++)
            {
                Displays[i] = new MPDConfiguration(other.Displays[i]);
            }
        }

        public virtual object Clone() => new MPDSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the instance to defaults (by definition, field value of "" implies default).
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < (int)CockpitDisplays.NUM_DISPLAYS; i++)
            {
                Displays[i].Reset();
            }
        }
    }
}
