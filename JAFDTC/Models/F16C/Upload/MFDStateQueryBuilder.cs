// ********************************************************************************************************************
//
// MFDQueryStateBuilder.cs -- f-16c mfd command builder
//
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
using JAFDTC.Models.F16C.MFD;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// dcs query builder for a query on current mfd state across specified modes. the QueryCurrentMFDState() method
    /// generates a stream of queries to gather current mfd format congfiguration across all master modes.
    /// </summary>
    internal class MFDStateQueryBuilder : QueryBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly Dictionary<string, string> _mapDCSFmtToDispFmt;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MFDStateQueryBuilder(IAirframeDeviceManager dcsCmds, StringBuilder sb) : base(dcsCmds, sb)
        {
            _mapDCSFmtToDispFmt = new()
            {
                [""] = ((int)MFDConfiguration.DisplayFormats.BLANK).ToString(),
                ["DTE"] = ((int)MFDConfiguration.DisplayFormats.DTE).ToString(),
                ["FCR"] = ((int)MFDConfiguration.DisplayFormats.FCR).ToString(),
                ["FLCS"] = ((int)MFDConfiguration.DisplayFormats.FLCS).ToString(),
                ["HAD"] = ((int)MFDConfiguration.DisplayFormats.HAD).ToString(),
                ["HSD"] = ((int)MFDConfiguration.DisplayFormats.HSD).ToString(),
                ["SMS"] = ((int)MFDConfiguration.DisplayFormats.SMS).ToString(),
                ["TEST"] = ((int)MFDConfiguration.DisplayFormats.TEST).ToString(),
                ["TGP"] = ((int)MFDConfiguration.DisplayFormats.TGP).ToString(),
                ["WPN"] = ((int)MFDConfiguration.DisplayFormats.WPN).ToString(),
            };
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// set up a MFDConfiguration in accordance with the state string the QueryMFDFormatState dcs query returns.
        /// </summary>
        private void PackCurrentState(string mfdState, MFDConfiguration mfdConfig)
        {
            List<string> elems = new(mfdState.Split(','));
            mfdConfig.SelectedOSB = (elems.Count > 0) ? elems[0] : "12";
            mfdConfig.OSB12 = (elems.Count > 1) ? _mapDCSFmtToDispFmt[elems[1]] : "";
            mfdConfig.OSB13 = (elems.Count > 2) ? _mapDCSFmtToDispFmt[elems[2]] : "";
            mfdConfig.OSB14 = (elems.Count > 3) ? _mapDCSFmtToDispFmt[elems[3]] : "";
        }

        /// <summary>
        /// query the current mfd setups including formats programmed on osb12-14 and the currently selected format
        /// for a given master mode. assumes jet is in nav on entry and returns with jet in nav. configuartion
        /// returned via MFDModeConfiguration.
        /// </summary>
        private void QueryCurrentMFDState(AirframeDevice ufc, AirframeDevice hotas,
                                          MFDSystem.MasterModes mode, MFDModeConfiguration modeFmtss)
        {
            // build a command stream that sets the target master mode (starting from nav) then queries the left
            // mfd state. run a query using that stream then clear the stream to prepare for the next query.
            //
            string masterMode = (mode == MFDSystem.MasterModes.ICP_AA) ? "AA" : "AG";
            if (mode == MFDSystem.MasterModes.DGFT_DGFT)
                AddAction(hotas, "DGFT", WAIT_BASE);
            else if (mode == MFDSystem.MasterModes.DGFT_MSL)
                AddAction(hotas, "MSL", WAIT_BASE);
            else if (mode != MFDSystem.MasterModes.NAV)
                AddAction(ufc, masterMode, WAIT_BASE);
            AddQuery("QueryMFDFormatState", new() { "left" });

            string mfdStateLeft = Query();
            PackCurrentState(mfdStateLeft, modeFmtss.LeftMFD);
            ClearCommands();

            // build a command stream that queries the right mfd state (should be in the target master mode), then
            // returns to nav master mode. run a query using that stream then clear the stream to prepare for the
            // next query.
            //
            AddQuery("QueryMFDFormatState", new() { "right" });
            if ((mode == MFDSystem.MasterModes.DGFT_DGFT) || (mode == MFDSystem.MasterModes.DGFT_MSL))
                AddAction(hotas, "CENTER");
            else if (mode != MFDSystem.MasterModes.NAV)
                AddAction(ufc, masterMode);

            string mfdStateRight = Query();
            PackCurrentState(mfdStateRight, modeFmtss.RightMFD);
            ClearCommands();
        }

        /// <summary>
        /// gather the current mfd setups including formats programmed on osb12-14 and the currently selected format
        /// for all master modes. results are returned through the modFmtss[] indexed by mode with jet in nav master
        /// mode.
        /// </summary>
        public void QueryCurrentMFDStateForAllModes(MFDModeConfiguration[] modeFmtss)
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice hotas = _aircraft.GetDevice("HOTAS");

            AddWhileBlock("IsInNAVMode", false, null, delegate ()
            {
                AddAction(ufc, "AA", WAIT_BASE);
                AddAction(hotas, "CENTER", WAIT_BASE);
            });

            for (int mode = 0; mode < (int)MFDSystem.MasterModes.NUM_MODES; mode++)
                QueryCurrentMFDState(ufc, hotas, (MFDSystem.MasterModes)mode, modeFmtss[mode]);
        }

        /// <summary>
        /// gather the current mfd setups including formats programmed on osb12-14 and the currently selected format
        /// for a specific master mode. results are returned through the modFmtss with jet in nav master mode.
        /// </summary>
        public void QueryCurrentMFDStateForMode(MFDSystem.MasterModes mode, MFDModeConfiguration modeFmtss)
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice hotas = _aircraft.GetDevice("HOTAS");

            AddWhileBlock("IsInNAVMode", false, null, delegate ()
            {
                AddAction(ufc, "AA", WAIT_BASE);
                AddAction(hotas, "CENTER", WAIT_BASE);
            });

            QueryCurrentMFDState(ufc, hotas, mode, modeFmtss);
        }
    }
}
