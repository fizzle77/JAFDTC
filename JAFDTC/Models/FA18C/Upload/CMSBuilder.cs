// ********************************************************************************************************************
//
// CMSBuilder.cs -- fa-18c cms command builder
//
// Copyright(C) 2021-2023 the-paid-actor & others
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
        /// configure cms system via the lddi/cmds according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.CMS.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== CMSBuilder:Build()" });

            if (_cfg.IsMerged(CMSSystem.SystemTag))
                return;

            AirframeDevice lddi = _aircraft.GetDevice("LDDI");
            AirframeDevice cmds = _aircraft.GetDevice("CMDS");
            CMSSystem defaultSys = CMSSystem.ExplicitDefaults;

            AddWhileBlock("IsLDDITAC", false, null, delegate ()
            {
                AddAction(lddi, "OSB-18", WAIT_BASE);                                       // MENU
            }, 4);

            AddAction(lddi, "OSB-17", WAIT_BASE);                                           // EW
            AddIfBlock("IsDispenserOff", true, null, delegate ()
            {
                AddAction(cmds, "ON");
                AddWhileBlock("IsALE47Mode", false, new() { "STBY" }, delegate ()
                {
                    AddWait(1000);
                });
            });
            AddAction(lddi, "OSB-08", WAIT_BASE);                                           // ALE-47
            AddWhileBlock("IsALE47Mode", false, new() { "MAN 1" }, delegate ()
            {
                AddAction(lddi, "OSB-19", WAIT_BASE);                                       // MODE
            });
            AddAction(lddi, "OSB-09", WAIT_BASE);                                           // ARM

            for (var i = 0; i < _cfg.CMS.Programs.Length; i++)
            {
                CMProgram pgm = _cfg.CMS.Programs[i];
                CMProgram pgmDefault = defaultSys.Programs[i];
                if (!pgm.IsDefault)
                {
                    if (!string.IsNullOrEmpty(pgm.ChaffQ))
                    {
                        AddAction(lddi, "OSB-05", WAIT_BASE);                               // Chaff
                        AdjustQty(lddi, int.Parse(pgm.ChaffQ), int.Parse(pgmDefault.ChaffQ));
                        AddAction(lddi, "OSB-05", WAIT_BASE);
                    }
                    if (!string.IsNullOrEmpty(pgm.FlareQ))
                    {
                        AddAction(lddi, "OSB-04", WAIT_BASE);                               // Flare
                        AdjustQty(lddi, int.Parse(pgm.FlareQ), int.Parse(pgmDefault.FlareQ));
                        AddAction(lddi, "OSB-04", WAIT_BASE);
                    }
                    if (!string.IsNullOrEmpty(pgm.SQ))
                    {
                        AddAction(lddi, "OSB-14", WAIT_BASE);                               // Rpt
                        AdjustQty(lddi, int.Parse(pgm.SQ), int.Parse(pgmDefault.SQ));
                        AddAction(lddi, "OSB-14", WAIT_BASE);
                    }
                    if (!string.IsNullOrEmpty(pgm.SI))
                    {
                        AddAction(lddi, "OSB-15", WAIT_BASE);                               // Interval
                        AdjustInterval(lddi, double.Parse(pgm.SI), double.Parse(pgmDefault.SI));
                        AddAction(lddi, "OSB-15", WAIT_BASE);
                    }
                }
                AddAction(lddi, "OSB-19", WAIT_BASE);                                       // SAVE
                AddAction(lddi, "OSB-20", WAIT_BASE);                                       // STEP
            }
            AddActions(lddi, new() { "OSB-09" });                                           // RTN
            AddWhileBlock("IsLDDITAC", false, null, delegate ()
            {
                AddAction(lddi, "OSB-18");                                                  // MENU
            });
            AddActions(lddi, new() { "OSB-03" });                                           // HUD
        }

        /// <summary>
        /// adjust the quantity of a chaff or flare program.
        /// </summary>
        private void AdjustQty(AirframeDevice lddi, int target, int defaultValue)
        {
            // TODO: worth looking at querying this state to minimize the number of button presses.

            if (target > defaultValue)
                for (var s = 0; s < target - defaultValue; s++)
                    AddAction(lddi, "OSB-12", WAIT_BASE);                                   // Up
            else if (target < defaultValue)
                for (var s = 0; s < defaultValue - target; s++)
                    AddAction(lddi, "OSB-13", WAIT_BASE);                                   // Down
        }

        /// <summary>
        /// adjust the interval of a chaff or flare program.
        /// </summary>
        private void AdjustInterval(AirframeDevice lddi, double target, double defaultValue)
        {
            // TODO: worth looking at querying this state to minimize the number of button presses.

            if (target > defaultValue)
                for (var s = 0; s < (target - defaultValue) / (double)0.25; s++)
                    AddAction(lddi, "OSB-12", WAIT_BASE);                                   // Up
            else if (target < defaultValue)
                for (var s = 0; s < (defaultValue - target) / (double)0.25; s++)
                    AddAction(lddi, "OSB-13", WAIT_BASE);                                   // Down
        }
    }
}
