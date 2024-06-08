// ********************************************************************************************************************
//
// HTSThreatBuilder.cs -- f-16c hts threat table command builder
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

using JAFDTC.Models.DCS;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// builder to generate the command stream to configure hts enabled threat tables via the hts format. the command
    /// stream is built assuming the hts format is selected on one of the viper's mfds. the builder requires the
    /// following state:
    /// 
    ///     mfdSide: string
    ///         identifies the mfd that displays the hts page, legal values are "left" or "right"
    ///
    /// builder will restore the hts format to the base hts page.
    /// </summary>
    internal class HTSThreatEnableBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly string[] _htsThreatToOSB;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HTSThreatEnableBuilder(F16CConfiguration cfg, F16CDeviceManager dcsCmds, StringBuilder sb)
            : base(cfg, dcsCmds, sb)
        {
            _htsThreatToOSB = new string[]
            {
                "OSB-02", "OSB-20", "OSB-19", "OSB-18", "OSB-17", "OSB-16",     // MAN, TC1-TC5
                "OSB-06", "OSB-07", "OSB-08", "OSB-09", "OSB-10", "OSB-01"      // TC6-TC11
            };
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure hts enabled threat tables via the hts mfd format according to the non-default programming
        /// settings (this function is safe to call with a configuration with default settings: defaults are skipped
        /// as necessary). the builder requires the following state:
        /// 
        ///     mfdSide: string
        ///         identifies the mfd that displays the hts page, legal values are "left" or "right"
        /// 
        /// the command stream assumes the hts page is selected on the mfd given by mfdSide state at the point the
        /// command stream this builder generates is added. 
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.HTS.IsDefault)
                return;

            if (state.TryGetValueAs("mfdSide", out string mfdSide))
            {
                AirframeDevice mfd = (mfdSide == "left") ? _aircraft.GetDevice("LMFD") : _aircraft.GetDevice("RMFD");

                AddAction(mfd, "OSB-04");                       // flip to threats

                // ASSUMES: HTS enables TC1-TC11 and disables MAN threats by default. OSB-5 (ALL) used to flip that
                // ASSUMES: around so we start from TC1-11 disabled and MAN enabled.
                //
                AddIfBlock("HTSAllNotSelected", true, new() { mfdSide }, delegate ()
                {
                    AddAction(mfd, "OSB-05");
                });
                AddAction(mfd, "OSB-05");

                // TODO: would be nice to do the button presses conditionally based on current state. this will
                // TODO: require some post-setup command sequencing though.
                //
                if (!_cfg.HTS.EnabledThreats[0])
                    AddAction(mfd, _htsThreatToOSB[0]);
                for (int i = 1; i < _htsThreatToOSB.Length; i++)
                    if (_cfg.HTS.EnabledThreats[i])
                        AddAction(mfd, _htsThreatToOSB[i]);

                AddAction(mfd, "OSB-04");                       // flip to main hts page
            }
        }
    }
}
