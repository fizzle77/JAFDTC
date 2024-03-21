// ********************************************************************************************************************
//
// MFDConfiguration.cs -- f-16c mfd configuration
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

namespace JAFDTC.Models.F16C.MFD
{
	/// <summary>
	/// TODO: document
	/// </summary>
	public class MFDConfiguration : BindableObject
	{
        // avionics mfd formats. this enum is used to index ui menus. we do not include FLIR, RCCE, or TFR formats as
        // these are not implemented.
        //
        public enum DisplayFormats
        {
            BLANK = 0,
            DTE = 1,
            FCR = 2,
            FLCS = 3,
            HAD = 4,
            HSD = 5,
            SMS = 6,
            TEST = 7,
            TGP = 8,
            WPN = 9,
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events.

        public string OSB14 { get; set; }               // 14, 13, or 12 to initial osb
		public string OSB13 { get; set; }
		public string OSB12 { get; set; }
		public string SelectedOSB { get; set; }

        // ---- following properties are synthesized.

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault
        {
			get => (string.IsNullOrEmpty(SelectedOSB) &&
                    string.IsNullOrEmpty(OSB14) && string.IsNullOrEmpty(OSB13) && string.IsNullOrEmpty(OSB12));
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MFDConfiguration()
		{
			Reset();
		}

		public MFDConfiguration(MFDConfiguration other)
		{
            OSB14 = new(other.OSB14);
            OSB13 = new(other.OSB13);
            OSB12 = new(other.OSB12);
            SelectedOSB = new(other.SelectedOSB);
        }

        public MFDConfiguration(DisplayFormats osb14, DisplayFormats osb13, DisplayFormats osb12, int selectedPage)
		{
			OSB14 = ((int)osb14).ToString();
			OSB13 = ((int)osb13).ToString();
			OSB12 = ((int)osb12).ToString();
			SelectedOSB = ((int)selectedPage).ToString();
		}

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public void Reset()
		{
			SelectedOSB = "";
			OSB14 = "";
			OSB13 = "";
			OSB12 = "";
		}
	}
}
