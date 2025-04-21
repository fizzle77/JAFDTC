// ********************************************************************************************************************
//
// DTEBuilder.cs -- f-16c dte command builder
//
// Copyright(C) 2025 ilominar/raven
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
using Microsoft.UI.Composition;
using System.Collections.Generic;
using System.Text;
using Windows.AI.MachineLearning.Preview;
using Windows.Media.Capture.Frames;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class DTEBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public DTEBuilder(F16CConfiguration cfg, F16CDeviceManager dm, StringBuilder sb) : base(cfg, dm, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the osb number (12, 13, or 14 as a string) in the mfd configuration that has the given display
        /// format, null if no such format is found.
        /// </summary>
        private string FindMSBWithFormat(MFDConfiguration mfd, MFDConfiguration.DisplayFormats format)
        {
            string strFormat = ((int)format).ToString();
            if (mfd.OSB12 == strFormat)
                return "12";
            else if (mfd.OSB13 == strFormat)
                return "13";
            else if (mfd.OSB14 == strFormat)
                return "14";
            return null;
        }

        /// <summary>
        /// configure dte system via the ded/ufc according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.DTE.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== DTEBuilder:Build()" });

            if (!_cfg.DTE.EnableLoadValue)
                return;

            AirframeDevice ufc = _aircraft.GetDevice("UFC");

            MFDModeConfiguration mcfg = state[$"MFDModeConfig.{MFDSystem.MasterModes.NAV}"] as MFDModeConfiguration;

            string osb = FindMSBWithFormat(mcfg.LeftMFD, MFDConfiguration.DisplayFormats.DTE);
            string osbSel = mcfg.LeftMFD.SelectedOSB;
            string mfdTag = "left";
            AirframeDevice mfd = _aircraft.GetDevice("LMFD");
            if (osb == null)
            {
                osb = FindMSBWithFormat(mcfg.RightMFD, MFDConfiguration.DisplayFormats.DTE);
                osbSel = mcfg.RightMFD.SelectedOSB;
                mfdTag = "right";
                mfd = _aircraft.GetDevice("RMFD");
            }
            if (osb !=null)
            {
                SelectMasterModeNAV(ufc);
                if (osb != osbSel)
                    AddAction(mfd, $"OSB-{osb}", WAIT_BASE);
                AddAction(mfd, "OSB-03", WAIT_LONG);
                AddWhileBlock("IsDTELoadDone", false, new() { mfdTag }, delegate () { AddWait(500); }, 20);
                if (osb != osbSel)
                    AddAction(mfd, $"OSB-{osbSel}", WAIT_BASE);
            }
        }
    }
}
