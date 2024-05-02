// ********************************************************************************************************************
//
// A10CMunition.cs -- munition class
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

namespace JAFDTC.Models.A10C
{
    public sealed class A10CMunition
    {
        public string Key { get; set; } // name as it appears in on DCS display scrape. must be unique.

        public string Name { get; set; } // display name for the weapon

        public string Image { get; set; } // name of the weapon's image file

        // values to enable/disable settings UI
        public bool CCIP { get; set; }
        public bool CCRP { get; set; }
        public bool EscMnvr { get; set; }
        public bool Laser { get; set; }
        public bool AutoLase { get; set; }
        public bool Pairs { get; set; }
        public bool Ripple { get; set; }
        public bool RipFt { get; set; }
        public bool HOF { get; set; }
        public bool Fuze { get; set; }

        // synthesized properties
        public string ImageFullPath => "/Images/" + Image;
        public bool SingleReleaseOnly => !Pairs && !Ripple;
    }
}
