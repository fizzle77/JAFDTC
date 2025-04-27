// ********************************************************************************************************************
//
// MFDQueryStateBuilder.cs -- f-16c mfd command builder
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.F16C.MFD;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// dcs query builder for a query on current mfd state across specified modes. QueryCurrentMFDStateForAllModes()
    /// method generates a stream of queries to gather current mfd format congfiguration (formats on osb 12-14 along
    /// with selected format) across all master modes. this class produces a state dictionary with,
    /// 
    ///     MFDModeConfig.{mode}: MFDModeConfiguration
    ///         default format and current mfd formats for master mode "mode" (MFDSystem.MasterModes)
    /// 
    /// entries for each master mode. if unable to establish the setup on the jet for a given mode, the default setup
    /// for the master mode is returned.
    /// 
    /// NOTE: the dictionary values represent the state at the time the query is run. depending on mfd configuration,
    /// NOTE: these values may no longer be valid once changes are made to mfd configuration.
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

        public MFDStateQueryBuilder(IAirframeDeviceManager dm, StringBuilder sb) : base(dm, sb)
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
            if (!string.IsNullOrEmpty(mfdState))
            {
                List<string> elems = new(mfdState.Split(','));
                mfdConfig.SelectedOSB = (elems.Count > 0) ? elems[0] : "12";
                mfdConfig.OSB12 = (elems.Count > 1) ? _mapDCSFmtToDispFmt[elems[1]] : "";
                mfdConfig.OSB13 = (elems.Count > 2) ? _mapDCSFmtToDispFmt[elems[2]] : "";
                mfdConfig.OSB14 = (elems.Count > 3) ? _mapDCSFmtToDispFmt[elems[3]] : "";
            }
        }

        /// <summary>
        /// set up a MFDConfiguration in accordance with the defaults for the mode.
        /// </summary>
        private static void PackDefaultState(MFDSystem.MasterModes mode, bool isLeftMFD, MFDConfiguration mfdConfig)
        {
            MFDConfiguration dflt = (isLeftMFD) ? MFDSystem.ExplicitDefaults.ModeConfigs[(int)mode].LeftMFD
                                                : MFDSystem.ExplicitDefaults.ModeConfigs[(int)mode].RightMFD;
            mfdConfig.SelectedOSB = dflt.SelectedOSB;
            mfdConfig.OSB12 = dflt.OSB12;
            mfdConfig.OSB13 = dflt.OSB13;
            mfdConfig.OSB14 = dflt.OSB14;
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
            if (!string.IsNullOrEmpty(mfdStateLeft))
                PackCurrentState(mfdStateLeft, modeFmtss.LeftMFD);
            else
                PackDefaultState(mode, true, modeFmtss.LeftMFD);
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
            if (!string.IsNullOrEmpty(mfdStateLeft))
                PackCurrentState(mfdStateRight, modeFmtss.RightMFD);
            else
                PackDefaultState(mode, false, modeFmtss.RightMFD);
            ClearCommands();
        }

        /// <summary>
        /// gather the current mfd setups including formats programmed on osb12-14 and the currently selected format
        /// for all master modes along with munition loadouts from AA and AG master modes. returns a state dictionary
        /// with [ "MFDModeConfig.{MFDSystem.MasterModes}", MFDModeConfiguration ] tuples added for each master mode.
        /// 
        /// returns avionics to nav master mode with no other changes.
        /// </summary>
        public Dictionary<string, object> QueryCurrentMFDStateForAllModes(Dictionary<string, object> state = null)
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice hotas = _aircraft.GetDevice("HOTAS");

            ClearCommands();

            AddWhileBlock("IsInNAVMode", false, null, delegate ()
            {
                AddAction(ufc, "AA", WAIT_BASE);
                AddAction(hotas, "CENTER", WAIT_BASE);
            });

            state ??= new();
            for (int mode = 0; mode < (int)MFDSystem.MasterModes.NUM_MODES; mode++)
            {
                MFDModeConfiguration modeFmtss = new();
                QueryCurrentMFDState(ufc, hotas, (MFDSystem.MasterModes)mode, modeFmtss);
                state.Add($"MFDModeConfig.{(MFDSystem.MasterModes)mode}", modeFmtss);
            }

            return state;
        }
    }
}
