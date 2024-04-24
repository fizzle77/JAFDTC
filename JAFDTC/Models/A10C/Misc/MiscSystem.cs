// ********************************************************************************************************************
//
// MiscSystem.cs -- a-10c miscellaneous system
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

namespace JAFDTC.Models.A10C.Misc
{
    // defines the coordinate system options
    //
    public enum CoordSystems
    {
        LL = 0,
        MGRS = 1
    }

    // defines the flight plan auto options
    //
    public enum FlightPlanManualOptions
    {
        Auto = 0,
        Manual = 1
    }

    /// <summary>
    /// TODO: document
    /// </summary>
    public class MiscSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:A10C:MISC";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties post change and validation events

        private string _coordSystem;                              // integer [0, 1]
        public string CoordSystem
        {
            get => _coordSystem;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _coordSystem, value, error);
            }
        }

        private string _flightPlan1Manual;                              // integer [0, 1]
        public string FlightPlan1Manual
        {
            get => _flightPlan1Manual;
            set
            {
                string error = (string.IsNullOrEmpty(value) || IsIntegerFieldValid(value, 0, 1)) ? null : "Invalid format";
                SetProperty(ref _flightPlan1Manual, value, error);
            }
        }

        // ---- following properties are synthesized

        // returns a MiscSystem with the fields populated with the actual default values (note that usually the value
        // "" implies default).
        //
        // defaults are as of DCS v2.9.0.47168.
        //
        public readonly static MiscSystem ExplicitDefaults = new()
        {
            CoordSystem = "0", // Lat/Long
            FlightPlan1Manual = "0" // Auto
        };

        // returns true if the instance indicates a default setup (all fields are "") or the object is in explicit
        // form, false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault
        {
            get => IsCoordSystemDefault && IsFlightPlan1ManualDefault;
        }

        [JsonIgnore]
        public bool IsCoordSystemDefault
        {
            get => string.IsNullOrEmpty(CoordSystem) || CoordSystem == ExplicitDefaults.CoordSystem;
        }

        [JsonIgnore]
        public bool IsFlightPlan1ManualDefault
        {
            get => string.IsNullOrEmpty(FlightPlan1Manual) || FlightPlan1Manual == ExplicitDefaults.FlightPlan1Manual;
        }


        // ---- following accessors get the current value (default or non-default) for various properties

        [JsonIgnore]
        public CoordSystems CoordSystemValue
        {
            get => (CoordSystems)int.Parse((string.IsNullOrEmpty(CoordSystem)) ? ExplicitDefaults.CoordSystem : CoordSystem);
        }

        [JsonIgnore]
        public FlightPlanManualOptions FlightPlan1ManualValue
        {
            get => (FlightPlanManualOptions)int.Parse((string.IsNullOrEmpty(FlightPlan1Manual)) ? ExplicitDefaults.FlightPlan1Manual : FlightPlan1Manual);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MiscSystem()
        {
            Reset();
        }

        public MiscSystem(MiscSystem other)
        {
            CoordSystem = new(other.CoordSystem);
            FlightPlan1Manual = new(other.FlightPlan1Manual);
         }

        public virtual object Clone() => new MiscSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public void Reset()
        {
            CoordSystem = "";
            FlightPlan1Manual = "";
         }
    }
}
