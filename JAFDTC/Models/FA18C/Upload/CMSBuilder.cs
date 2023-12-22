// ********************************************************************************************************************
//
// CMSBuilder.cs -- fa-18c cms command builder
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.FA18C.CMS;
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

        public CMSBuilder(FA18CConfiguration cfg, FA18CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure cms system via the lmfd/cmds according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            Device lmfd = _aircraft.GetDevice("LMFD");
            Device cmds = _aircraft.GetDevice("CMDS");
            CMSSystem defaultSys = CMSSystem.ExplicitDefaults;

            if (!_cfg.CMS.IsDefault)
            {
                AppendCommand(lmfd.GetCommand("OSB-18")); // Menu
                AppendCommand(lmfd.GetCommand("OSB-17")); // EW

                AppendCommand(StartCondition("DispenserOff"));
                AppendCommand(cmds.GetCommand("ON"));
                AppendCommand(WaitVeryLong());
                AppendCommand(EndCondition("DispenserOff"));

                AppendCommand(lmfd.GetCommand("OSB-08")); // ALE-47
                AppendCommand(lmfd.GetCommand("OSB-09")); // ARM

                for (var i = 0; i < _cfg.CMS.Programs.Length; i++)
                {
                    CMProgram pgm = _cfg.CMS.Programs[i];
                    CMProgram pgmDefault = defaultSys.Programs[i];
                    if (!pgm.IsDefault)
                    {
                        if (!string.IsNullOrEmpty(pgm.ChaffQ))
                        {
                            AppendCommand(lmfd.GetCommand("OSB-05")); // Chaff
                            AppendCommand(Wait());
                            AdjustQty(lmfd, int.Parse(pgm.ChaffQ), int.Parse(pgmDefault.ChaffQ));
                            AppendCommand(lmfd.GetCommand("OSB-05"));
                        }

                        if (!string.IsNullOrEmpty(pgm.FlareQ))
                        {
                            AppendCommand(lmfd.GetCommand("OSB-04")); // Flare
                            AppendCommand(Wait());
                            AdjustQty(lmfd, int.Parse(pgm.FlareQ), int.Parse(pgmDefault.FlareQ));
                            AppendCommand(lmfd.GetCommand("OSB-04"));
                        }

                        if (!string.IsNullOrEmpty(pgm.SQ))
                        {
                            AppendCommand(lmfd.GetCommand("OSB-14")); // Rpt
                            AppendCommand(Wait());
                            AdjustQty(lmfd, int.Parse(pgm.SQ), int.Parse(pgmDefault.SQ));
                            AppendCommand(lmfd.GetCommand("OSB-14"));
                        }

                        if (!string.IsNullOrEmpty(pgm.SI))
                        {
                            AppendCommand(lmfd.GetCommand("OSB-15")); // Interval
                            AppendCommand(Wait());
                            AdjustInterval(lmfd, double.Parse(pgm.SI), double.Parse(pgmDefault.SI));
                            AppendCommand(lmfd.GetCommand("OSB-15"));
                        }
                    }
                    AppendCommand(lmfd.GetCommand("OSB-19")); // Save
                    AppendCommand(lmfd.GetCommand("OSB-20")); // Step
                }
                AppendCommand(lmfd.GetCommand("OSB-09")); // RTN
                AppendCommand(lmfd.GetCommand("OSB-18")); // MENU
                AppendCommand(lmfd.GetCommand("OSB-03")); // HUD
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void AdjustQty(Device lmfd, int target, int defaultValue)
        {
            if (target > defaultValue)
            {
                for (var s = 0; s < target - defaultValue; s++)
                {
                    AppendCommand(lmfd.GetCommand("OSB-12")); // Up
                }
            }
            else if (target < defaultValue)
            {
                for (var s = 0; s < defaultValue - target; s++)
                {
                    AppendCommand(lmfd.GetCommand("OSB-13")); // Down
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void AdjustInterval(Device lmfd, double target, double defaultValue)
        {
            if (target > defaultValue)
            {
                for (var s = 0; s < (target - defaultValue) / (double)0.25; s++)
                {
                    AppendCommand(lmfd.GetCommand("OSB-12")); // Up
                }
            }
            else if(target < defaultValue)
            {
                for (var s = 0; s < (defaultValue - target) / (double)0.25; s++)
                {
                    AppendCommand(lmfd.GetCommand("OSB-13")); // Down
                }
            }
        }
    }
}
