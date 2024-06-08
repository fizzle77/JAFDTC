// ********************************************************************************************************************
//
// SMSStateQueryBuilder.cs -- f-16c sms state query builder
//
// Copyright(C) 2024 ilominar/raven
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
    /// dcs query builder for a query on current sms state across specified modes. QuerySMSMunitionsForMode() method
    /// generates a stream of queries to switch to the sms page for the given master mode and collect all munitions
    /// accessible through the page.
    /// 
    /// this class produces a state dictionary with
    ///
    ///     SMSMuni.{mode}.mfdSide: string
    ///         identifies mfd that displays the hts page in "mode", legal values are "left" or "right"
    ///     SMSMuni.{mode}.osbSel: string
    ///         mfd button name ("OSB-nn") of the currently selected format on the mfd identified by mfdSide in "mode"
    ///     SMSMuni.{mode}.osbSMS: string
    ///         mfd button name ("OSB-nn") that selects the sms format on the mfd identified by mfdSide in "mode"
    ///     SMSMuni.{mode}: List of string
    ///         sms quantity + name strings for the munitions on the jet in "mode"
    ///
    /// where "mode" is a MFDSystem.MasterModes.
    /// </summary>
    internal class SMSStateQueryBuilder : QueryBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SMSStateQueryBuilder(IAirframeDeviceManager dcsCmds, StringBuilder sb) : base(dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// search the mfd format configuration for left/right displays looking for the side (left or right), sms
        /// osb (osb12-14), and currently-selected osb (osb12-14). return these three parameters.
        /// </summary>
        private void FindSMSFormat(MFDModeConfiguration mfdFmts,
                                   out string mfdSideOut, out string osbSMSOut, out string osbSelOut)
        {
            string fmtSMS = ((int)MFDConfiguration.DisplayFormats.SMS).ToString();
            string osbSMS;
            if (mfdFmts.LeftMFD.OSB12 == fmtSMS)
                osbSMS = "L12";
            else if (mfdFmts.LeftMFD.OSB13 == fmtSMS)
                osbSMS = "L13";
            else if (mfdFmts.LeftMFD.OSB14 == fmtSMS)
                osbSMS = "L14";
            else if (mfdFmts.RightMFD.OSB12 == fmtSMS)
                osbSMS = "R12";
            else if (mfdFmts.RightMFD.OSB13 == fmtSMS)
                osbSMS = "R13";
            else if (mfdFmts.RightMFD.OSB14 == fmtSMS)
                osbSMS = "R14";
            else
            {
                mfdSideOut = null;
                osbSMSOut = null;
                osbSelOut = null;
                return;
            }

            mfdSideOut = (osbSMS[0] == 'L') ? "left" : "right";
            osbSMSOut = $"OSB-{osbSMS[1..]}";
            osbSelOut = $"OSB-{((osbSMS[0] == 'L') ? mfdFmts.LeftMFD.SelectedOSB : mfdFmts.RightMFD.SelectedOSB)}";
        }

        /// <summary>
        /// iterate over the munitions in the indicated page capturing each munition.
        /// </summary>
        private List<string> DoMunitionsQuery(string modeBtn, string mfdSide, string osbSMS, string osbSelect)
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice mfd = (mfdSide == "left") ? _aircraft.GetDevice("LMFD") : _aircraft.GetDevice("RMFD");

            AddAction(ufc, modeBtn, WAIT_BASE);                                 // push A-G
            if (osbSMS != osbSelect)
                AddAction(mfd, osbSMS);                                         // push osb to select SMS format
            AddWhileBlock("IsSMSOnINV", true, new() { mfdSide }, delegate ()
            {
                AddAction(mfd, "OSB-04");                                       // INV osb
            });

            List<string> munitions = new();
            string munition;
            while (true)
            {
                AddQuery("QuerySMSMuniState", new() { mfdSide });
                munition = Query();
                if (!string.IsNullOrEmpty(munition) && ((munitions.Count == 0) || (munition != munitions[0])))
                    munitions.Add(munition);
                else
                    break;

                ClearCommands();
                AddAction(mfd, "OSB-06");                                       // advance to next munition
            }

            ClearCommands();
            if (osbSMS != osbSelect)
                AddAction(mfd, osbSelect);                                      // push osb to return to original
            AddAction(ufc, modeBtn, WAIT_BASE);                                 // push nav
            AddQuery("QueryNOP", null);
            Query();

            return munitions;
        }

        /// <summary>
        /// gather a list of current munitions available via the sms page in the given master mode. note that the
        /// sms page must be on the left or right mfd for the specified mode. returns a state dictionary with,
        ///
        ///     SMSMuni.{mode}.mfdSide: string
        ///         identifies mfd that displays the hts page in "mode", legal values are "left" or "right"
        ///     SMSMuni.{mode}.osbSel: string
        ///         mfd button name ("OSB-nn") of the currently selected format on the mfd identified by mfdSide in "mode"
        ///     SMSMuni.{mode}.osbSMS: string
        ///         mfd button name ("OSB-nn") that selects the sms format on the mfd identified by mfdSide in "mode"
        ///     SMSMuni.{mode}: List of string
        ///         sms quantity + name strings for the munitions on the jet in "mode"
        ///
        /// where "mode" is a MFDSystem.MasterModes.
        /// 
        /// returns avionics to nav master mode with no other changes.
        /// </summary>
        public Dictionary<string, object> QuerySMSMunitionsForMode(MFDSystem.MasterModes mode,
                                                                   Dictionary<string, object> state = null)
        {
            ClearCommands();

            string modeBtn = mode switch
            {
                // TODO: support ICP_AA here as well eventually?
                MFDSystem.MasterModes.ICP_AG => "AG",
                _ => null
            };
            state ??= new();
            if ((modeBtn != null) && state.TryGetValue($"MFDModeConfig.{mode}", out object value))
            {
                MFDModeConfiguration modeFmts = value as MFDModeConfiguration;
                FindSMSFormat(modeFmts, out string mfdSide, out string osbSel, out string osbSMS);
                if (mfdSide != null)
                {
                    state.Add($"SMSMuni.{mode}.mfdSide", mfdSide);
                    state.Add($"SMSMuni.{mode}.osbSel", osbSel);
                    state.Add($"SMSMuni.{mode}.osbSMS", osbSMS);
                    state.Add($"SMSMuni.{mode}", DoMunitionsQuery(modeBtn, mfdSide, osbSel, osbSMS));
                }
            }
            return state;
        }
    }
}
