// ********************************************************************************************************************
//
// DLNKSystem.cs -- f-16c datalink system configuration
//
// Copyright(C) 2023-2025 ilominar/raven
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
    /// class to capture the settings of the DLNK system. most DLNK fields are encoded as strings. a field value of
    /// "" implies that the field is set to the default value in the avionics.
    /// </summary>
    public class DLNKSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:F16C:DLNK";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- private properties, static

        private static readonly Regex _callRegex = new(@"^[a-zA-Z][a-zA-Z]$");
        private static readonly Regex _numRegex = new(@"^[1-9][1-9]$");
        private static readonly Regex _tndlRegex = new(@"^[0-7]{5}$");

        // ---- public properties

        public string Ownship { get; set; }                     // string, number 1-8

        public TeamMember[] TeamMembers { get; set; }

        // ---- public properties, posts change/validation events

        private bool _isOwnshipLead;
        public bool IsOwnshipLead
        {
            get => _isOwnshipLead;
            set => SetProperty(ref _isOwnshipLead, value);
        }

        private string _ownshipCallsign;                        // string, two letter [A-Z][A-Z]
        public string OwnshipCallsign
        {
            get => _ownshipCallsign;
            set
            {
                string error = "Invalid callsign format";
                if (IsRegexFieldValid(value, _callRegex))
                {
                    value = value.ToUpper();
                    error = null;
                }
                SetProperty(ref _ownshipCallsign, value, error);

            }
        }

        private string _ownshipFENumber;                        // string, two digit [1-9][1-9]
        public string OwnshipFENumber
        {
            get => _ownshipFENumber;
            set
            {
                string error = "Invalid callsign flight number format";
                if (IsRegexFieldValid(value, _numRegex))
                    error = null;
                SetProperty(ref _ownshipFENumber, value, error);
            }
        }

        private bool _isFillEmptyTNDL;
        public bool IsFillEmptyTNDL
        {
            get => _isFillEmptyTNDL;
            set => SetProperty(ref _isFillEmptyTNDL, value);
        }

        private string _fillEmptyTNDL;
        public string FillEmptyTNDL
        {
            get => _fillEmptyTNDL;
            set
            {
                string error = ((IsRegexFieldValid(value, _tndlRegex))) ? null : "Invalid TNDL value";
                SetProperty(ref _fillEmptyTNDL, value, error);
            }
        }

        // ---- public properties, computed

        [JsonIgnore]
        public override bool IsDefault
        {
            get
            {
                for (int i = 0; i < TeamMembers.Length; i++)
                    if (!TeamMembers[i].IsDefault)
                        return false;

                // NOTE: we don't include IsOwnshipLead here as default will depend on which slot the pilot is
                // NOTE: sitting in.
                //
                return !IsFillEmptyTNDL && string.IsNullOrEmpty(OwnshipCallsign) && string.IsNullOrEmpty(OwnshipFENumber);
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
            OwnshipCallsign = "";
            OwnshipFENumber = "";
            IsFillEmptyTNDL = false;
            FillEmptyTNDL = "";
            TeamMembers = new TeamMember[8];
            for (int i = 0; i < TeamMembers.Length; i++)
                TeamMembers[i] = new TeamMember();
        }

        public DLNKSystem(DLNKSystem other)
        {
            Ownship = new(other.Ownship);
            IsOwnshipLead = other.IsOwnshipLead;
            OwnshipCallsign = other.OwnshipCallsign;
            OwnshipFENumber = other.OwnshipFENumber;
            IsFillEmptyTNDL = other.IsFillEmptyTNDL;
            FillEmptyTNDL = other.FillEmptyTNDL;
            TeamMembers = new TeamMember[8];
            for (int i = 0; i < TeamMembers.Length; i++)
                TeamMembers[i] = new TeamMember(other.TeamMembers[i]);
        }

        public virtual object Clone() => new DLNKSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public override void Reset()
        {
            Ownship = "";
            IsOwnshipLead = false;
            OwnshipCallsign = "";
            OwnshipFENumber = "";
            IsFillEmptyTNDL = false;
            FillEmptyTNDL = "";
            for (int i = 0; i < TeamMembers.Length; i++)
                TeamMembers[i].Reset();
        }
    }
}
