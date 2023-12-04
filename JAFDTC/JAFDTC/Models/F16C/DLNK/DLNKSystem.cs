// ********************************************************************************************************************
//
// DLNKSystem.cs -- f-16c datalink system configuration
//
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

namespace JAFDTC.Models.F16C.DLNK
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class DLNKSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:F16C:DLNK";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        public string Ownship { get; set; }

        private bool _isOwnshipLead;
        public bool IsOwnshipLead
        {
            get => _isOwnshipLead;
            set => SetProperty(ref _isOwnshipLead, value);
        }

        public TeamMember[] TeamMembers { get; set; }

        // ---- following properties are synthesized

        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                for (int i = 0; i < TeamMembers.Length; i++)
                {
                    if (!TeamMembers[i].IsDefault)
                    {
                        return false;
                    }
                }
                return !IsOwnshipLead;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public DLNKSystem()
        {
            Ownship = "";
            IsOwnshipLead = false;
            TeamMembers = new TeamMember[8];
            for (int i = 0; i < TeamMembers.Length; i++)
            {
                TeamMembers[i] = new TeamMember();
            }
        }

        public DLNKSystem(DLNKSystem other)
        {
            Ownship = new(other.Ownship);
            IsOwnshipLead = other.IsOwnshipLead;
            TeamMembers = new TeamMember[8];
            for (int i = 0; i < TeamMembers.Length; i++)
            {
                TeamMembers[i] = new TeamMember(other.TeamMembers[i]);
            }
        }

        public virtual object Clone() => new DLNKSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public void Reset()
        {
            Ownship = "";
            IsOwnshipLead = false;
            for (int i = 0; i < TeamMembers.Length; i++)
            {
                TeamMembers[i].Reset();
            }
        }

        public void CleanUp()
        {
        }
    }
}
