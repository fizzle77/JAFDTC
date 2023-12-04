// ********************************************************************************************************************
//
// TeamMember.cs -- f-16c datalink system team member information
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
using System.Text.RegularExpressions;

namespace JAFDTC.Models.F16C.DLNK
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class TeamMember : BindableObject
    {
        private static readonly Regex tndlRegex = new(@"^[0-7]{5}$");

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private bool _tdoa;
        public bool TDOA
        {
            get => _tdoa;
            set => SetProperty(ref _tdoa, value);
        }

        private string _tndl;
        public string TNDL
        {
            get => _tndl;
            set
            {
                string error = ((IsRegexFieldValid(value, tndlRegex))) ? null : "Invalid TNDL value";
                SetProperty(ref _tndl, value, error);
            }
        }

        private string _driverUID;
        public string DriverUID
        {
            get => _driverUID;
            set => SetProperty(ref _driverUID, value);
        }

        // ---- TODO

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault => (!TDOA && string.IsNullOrEmpty(TNDL) && string.IsNullOrEmpty(DriverUID));

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public TeamMember() => (TDOA, TNDL, DriverUID) = (false, "", "");

        public TeamMember(TeamMember other)
            => (TDOA, TNDL, DriverUID) = (other.TDOA, new(other.TNDL), new(other.DriverUID));

        public virtual object Clone() => new TeamMember(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public void Reset() => (TDOA, TNDL, DriverUID) = (false, "", "");
    }
}
