// ********************************************************************************************************************
//
// MFDConfiguration.cs -- f-16c mfd configuration
//
// Copyright(C) 2021-2023 the-paid-actor & others
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

namespace JAFDTC.Models.F16C.MFD
{
	/// <summary>
	/// TODO: document
	/// </summary>
	public class MFDConfiguration : BindableObject
	{
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
			get => ((SelectedOSB.Length == 0) && (OSB14.Length == 0) && (OSB13.Length == 0) && (OSB12.Length == 0));
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

        public MFDConfiguration(Formats osb14, Formats osb13, Formats osb12, int selectedPage)
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

        // TODO: document
		public void CleanUp()
		{
		}
	}
}
