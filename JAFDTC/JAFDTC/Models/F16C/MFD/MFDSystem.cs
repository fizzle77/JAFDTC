// ********************************************************************************************************************
//
// MFDSystem.cs -- f-16c mfd system
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
    // avionics master modes. this enum is used to index internal arrays and should be sequential.
    //
    public enum Modes
    {
        NAV = 0,
        ICP_AG = 1,
        ICP_AA = 2,
        DGFT_MSL = 3,
        DGFT_DGFT = 4,
        NUM_MODES = 5
    }

    // avionics mfd formats. this enum is used to index ui menus. we do not include FLIR, RCCE, or TFR formats as
    // these are not implemented.
    //
    public enum Formats
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

    /// <summary>
    /// class to capture the settings of the MFD setup. most MFD fields are encoded as strings. when persisted, a
    /// field value of "" implies that the field is set to the default value in the avionics.
    /// </summary>
    public class MFDSystem : BindableObject, ISystem
    {
        public const string SystemTag = "JAFDTC:F16C:MFD";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change and validation events.

        // mode configurations are indexed by Modes enum.
        //
        public MFDModeConfiguration[] ModeConfigs { get; set; }

        // ---- following properties are synthesized.

        // returns a MFDSystem with the fields populated with the actual default values (note that usually the value
        // "" implies default).
        //
        // defaults are as of DCS v2.9.0.47168.
        //
        [JsonIgnore]
        public readonly static MFDSystem ExplicitDefaults = new()
        {
            ModeConfigs = new MFDModeConfiguration[(int)Modes.NUM_MODES]
            {
                    new MFDModeConfiguration(                                                       // Modes.NAV
                        new MFDConfiguration(Formats.FCR, Formats.TEST, Formats.DTE, 14),
                        new MFDConfiguration(Formats.SMS, Formats.HSD, Formats.BLANK, 13)),
                    new MFDModeConfiguration(                                                       // Modex.ICP_AG
                        new MFDConfiguration(Formats.FCR, Formats.FLCS, Formats.TEST, 14),
                        new MFDConfiguration(Formats.SMS, Formats.HSD, Formats.BLANK, 14)),
                    new MFDModeConfiguration(                                                       // Modes.ICP_AA
                        new MFDConfiguration(Formats.FCR, Formats.FLCS, Formats.TEST, 14),
                        new MFDConfiguration(Formats.SMS, Formats.HSD, Formats.BLANK, 14)),
                    new MFDModeConfiguration(                                                       // Modes.DGFT_MSL
                        new MFDConfiguration(Formats.FCR, Formats.BLANK, Formats.BLANK, 14),
                        new MFDConfiguration(Formats.SMS, Formats.BLANK, Formats.BLANK, 14)),
                    new MFDModeConfiguration(                                                       // Modes.DGFT_DGFT
                        new MFDConfiguration(Formats.FCR, Formats.BLANK, Formats.BLANK, 14),
                        new MFDConfiguration(Formats.SMS, Formats.BLANK, Formats.BLANK, 14))
            }
        };

        // returns true if the instance indicates a default setup (all fields are "") or the object is in explicit
        // form, false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault
        {
            get
            {
                for (var i = Modes.NAV; i <= Modes.DGFT_DGFT; i++)
                {
                    if (!ModeConfigs[(int)i].IsDefault)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MFDSystem()
		{
            ModeConfigs = new MFDModeConfiguration[(int)Modes.NUM_MODES]
            {
                new MFDModeConfiguration(),
                new MFDModeConfiguration(),
                new MFDModeConfiguration(),
                new MFDModeConfiguration(),
                new MFDModeConfiguration()
            };
		}

        public MFDSystem(MFDSystem other)
        {
            ModeConfigs = new MFDModeConfiguration[(int)Modes.NUM_MODES]
            {
                new MFDModeConfiguration(other.ModeConfigs[(int)Modes.NAV]),
                new MFDModeConfiguration(other.ModeConfigs[(int)Modes.ICP_AA]),
                new MFDModeConfiguration(other.ModeConfigs[(int)Modes.ICP_AG]),
                new MFDModeConfiguration(other.ModeConfigs[(int)Modes.DGFT_MSL]),
                new MFDModeConfiguration(other.ModeConfigs[(int)Modes.DGFT_DGFT])
            };
        }

        public virtual object Clone()
        {
            return new MFDSystem(this);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // Methods
        //
        // ------------------------------------------------------------------------------------------------------------

        // reset the instance to defaults (by definition, field value of "" implies default).
        //
        public void Reset()
        {
            foreach (MFDModeConfiguration config in ModeConfigs)
            {
                config.Reset();
            }
        }
    }
}
