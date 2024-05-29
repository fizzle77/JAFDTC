// ********************************************************************************************************************
//
// F16CMunition.cs -- Properties of F-16C weapons, hydrated from FileManager.LoadF16CMunitions().
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2024 ilominar/raven, fizzle
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

using JAFDTC.Models.A10C.DSMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JAFDTC.Models.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public sealed class F16CMunition
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties are deserialized from DB JSON

        public int ID { get; set; }                             // unique ID used in configuration files

        public string LabelUI { get; set; }                     // munition label for ui

        public string DescrUI { get; set; }                     // munition long (ish) description for ui

        public string LabelSMS { get; set; }                    // munition label for sms page

        public string Image { get; set; }                       // munition image for ui, relative to Images/

        public string Class { get; set; }                       // CNTL class to set parameters like arming delay

        // TODO

        public MunitionSettings MunitionInfo { get; set; }      // munition information

        // ---- following properties are synthesized.

        [JsonIgnore]
        public string ImageFullPath => "/Images/" + Image;
    }
}
