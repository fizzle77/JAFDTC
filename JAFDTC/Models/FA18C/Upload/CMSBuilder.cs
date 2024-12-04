// ********************************************************************************************************************
//
// CMSBuilder.cs -- fa-18c cms command builder
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
using JAFDTC.Models.FA18C.CMS;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.FA18C.Upload
{
    /// <summary>
    /// command builder for the cms system in the hornet. translates countermeasures setup in FA18CConfiguration
    /// into commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class CMSBuilder : FA18CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public CMSBuilder(FA18CConfiguration cfg, FA18CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure cms system via the lmfd/cmds according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.CMS.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== CMSBuilder:Build()" });

            AirframeDevice lmfd = _aircraft.GetDevice("LMFD");
            AirframeDevice cmds = _aircraft.GetDevice("CMDS");
            CMSSystem defaultSys = CMSSystem.ExplicitDefaults;

            AddWhileBlock("IsLMFDTAC", false, null, delegate ()
            {
                AddAction(lmfd, "OSB-18");                                                  // MENU
            });
            AddActions(lmfd, new() { "OSB-17" });                                           // EW
            AddIfBlock("IsDispenserOff", true, null, delegate ()
            {
                AddAction(cmds, "ON");
                AddWait(WAIT_VERY_LONG);
            });

            AddActions(lmfd, new() { "OSB-08", "OSB-09" }); // ALE-47, ARM

            for (var i = 0; i < _cfg.CMS.Programs.Length; i++)
            {
                CMProgram pgm = _cfg.CMS.Programs[i];
                CMProgram pgmDefault = defaultSys.Programs[i];
                if (!pgm.IsDefault)
                {
                    if (!string.IsNullOrEmpty(pgm.ChaffQ))
                    {
                        AddAction(lmfd, "OSB-05", WAIT_BASE);                               // Chaff
                        AdjustQty(lmfd, int.Parse(pgm.ChaffQ), int.Parse(pgmDefault.ChaffQ));
                        AddAction(lmfd, "OSB-05");
                    }
                    if (!string.IsNullOrEmpty(pgm.FlareQ))
                    {
                        AddAction(lmfd, "OSB-04", WAIT_BASE);                               // Flare
                        AdjustQty(lmfd, int.Parse(pgm.FlareQ), int.Parse(pgmDefault.FlareQ));
                        AddAction(lmfd, "OSB-04");
                    }
                    if (!string.IsNullOrEmpty(pgm.SQ))
                    {
                        AddAction(lmfd, "OSB-14", WAIT_BASE);                               // Rpt
                        AdjustQty(lmfd, int.Parse(pgm.SQ), int.Parse(pgmDefault.SQ));
                        AddAction(lmfd, "OSB-14");
                    }
                    if (!string.IsNullOrEmpty(pgm.SI))
                    {
                        AddAction(lmfd, "OSB-15", WAIT_BASE);                               // Interval
                        AdjustInterval(lmfd, double.Parse(pgm.SI), double.Parse(pgmDefault.SI));
                        AddAction(lmfd, "OSB-15");
                    }
                }
                AddActions(lmfd, new() { "OSB-19", "OSB-20" }, null, WAIT_BASE);            // SAVE, STEP
            }
            AddActions(lmfd, new() { "OSB-09" });                                           // RETURN
            AddWhileBlock("IsLMFDTAC", false, null, delegate ()
            {
                AddAction(lmfd, "OSB-18");                                                  // MENU
            });
            AddActions(lmfd, new() { "OSB-03" });                                           // HUD
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void AdjustQty(AirframeDevice lmfd, int target, int defaultValue)
        {
            if (target > defaultValue)
            {
                for (var s = 0; s < target - defaultValue; s++)
                    AddAction(lmfd, "OSB-12"); // Up
            }
            else if (target < defaultValue)
            {
                for (var s = 0; s < defaultValue - target; s++)
                    AddAction(lmfd, "OSB-13"); // Down
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void AdjustInterval(AirframeDevice lmfd, double target, double defaultValue)
        {
            if (target > defaultValue)
            {
                for (var s = 0; s < (target - defaultValue) / (double)0.25; s++)
                    AddAction(lmfd, "OSB-12"); // Up
            }
            else if(target < defaultValue)
            {
                for (var s = 0; s < (defaultValue - target) / (double)0.25; s++)
                    AddAction(lmfd, "OSB-13"); // Down
            }
        }
    }
}
