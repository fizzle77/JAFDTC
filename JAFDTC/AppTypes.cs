// ********************************************************************************************************************
//
// AppTypes.cs : helpful jafdtc types
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

using System.Collections.Generic;
using System.Text.Json;

namespace JAFDTC
{
    public enum AirframeTypes
    {
        None = 0,
        A10C = 1,
        AH64D = 2,
        AV8B = 3,
        F15E = 4,
        F16C = 5,
        FA18C = 6,
        M2000C = 7,
        F14AB = 8
    }

    public static class Globals
    {
        public static readonly Dictionary<AirframeTypes, string> AirframeNames = new()
        {
            [AirframeTypes.None] = "",
            [AirframeTypes.A10C] = "A-10C Warthog",
            [AirframeTypes.AH64D] = "AH-64D Apache",
            [AirframeTypes.AV8B] = "AV-8B Harrier",
            [AirframeTypes.F15E] = "F-15E Strike Eagle",
            [AirframeTypes.F16C] = "F-16C Viper",
            [AirframeTypes.FA18C] = "F/A-18C Hornet",
            [AirframeTypes.M2000C] = "Mirage 2000C",
            [AirframeTypes.F14AB] = "F-14A/B Tomcat"
        };

        public static readonly Dictionary<AirframeTypes, string> AirframeShortNames = new()
        {
            [AirframeTypes.None] = "",
            [AirframeTypes.A10C] = "A-10C",
            [AirframeTypes.AH64D] = "AH-64D",
            [AirframeTypes.AV8B] = "AV-8B",
            [AirframeTypes.F15E] = "F-15E",
            [AirframeTypes.F16C] = "F-16C",
            [AirframeTypes.FA18C] = "F/A-18C",
            [AirframeTypes.M2000C] = "M2000C",
            [AirframeTypes.F14AB] = "F-14A/B"
        };

        public static readonly JsonSerializerOptions JSONOptions = new() { WriteIndented = true };

        public const string VersionJAFDTC = "v1.0.0-B.14";              // current version

        public const string BuildJAFDTC = "version 1.0.0-B.14 of 8-DEC-23 (build 31415926)";
    }
}
