// ********************************************************************************************************************
//
// MPDBuilder.cs -- f-15e mpd/mpcd system command builder
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
using JAFDTC.Models.F15E.MPD;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E.Upload
{
    /// <summary>
    /// command builder for the mpd/mpcd systems in the mudhen. translates mpd/mpcd setup in F15EConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class MPDBuilder : F15EBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MPDBuilder(F15EConfiguration cfg, F15EDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure mpd/mpcd system via the push button controls according to the non-default programming settings
        /// (this function is safe to call with a configuration with default settings: defaults are skipped as
        /// necessary).
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            AddExecFunction("NOP", new() { "==== MPDBuilder:Build()" });

            Dictionary<MPDSystem.CockpitDisplays, AirframeDevice> devMap = new()
            {
                [MPDSystem.CockpitDisplays.PILOT_L_MPD] = _aircraft.GetDevice("FLMPD"),
                [MPDSystem.CockpitDisplays.PILOT_MPCD] = _aircraft.GetDevice("FMPCD"),
                [MPDSystem.CockpitDisplays.PILOT_R_MPD] = _aircraft.GetDevice("FRMPD"),

                [MPDSystem.CockpitDisplays.WSO_L_MPCD] = _aircraft.GetDevice("RLMPCD"),
                [MPDSystem.CockpitDisplays.WSO_L_MPD] = _aircraft.GetDevice("RLMPD"),
                [MPDSystem.CockpitDisplays.WSO_R_MPD] = _aircraft.GetDevice("RRMPD"),
                [MPDSystem.CockpitDisplays.WSO_R_MPCD] = _aircraft.GetDevice("RRMPCD")
            };

            if ((_cfg.CrewMember == F15EConfiguration.CrewPositions.PILOT) &&
                !_cfg.MPD.IsCrewMemberDefault(F15EConfiguration.CrewPositions.PILOT))
            {
                AddIfBlock("IsInFrontCockpit", true, null, delegate ()
                {
                    BuildDisplays(devMap, MPDSystem.CockpitDisplays.PILOT_L_MPD, MPDSystem.CockpitDisplays.PILOT_R_MPD);
                });
            }
            else if ((_cfg.CrewMember == F15EConfiguration.CrewPositions.WSO) && 
                     !_cfg.MPD.IsCrewMemberDefault(F15EConfiguration.CrewPositions.WSO))
            {
                AddIfBlock("IsInRearCockpit", true, null, delegate ()
                {
                    BuildDisplays(devMap, MPDSystem.CockpitDisplays.WSO_L_MPCD, MPDSystem.CockpitDisplays.WSO_R_MPCD);
                });
            }
        }

        /// <summary>
        /// walk the displays between min and max (inclusive) and emit the key presses to handle the displays.
        /// </summary>
        void BuildDisplays(Dictionary<MPDSystem.CockpitDisplays, AirframeDevice> devMap,
                           MPDSystem.CockpitDisplays min, MPDSystem.CockpitDisplays max)
        {
            for (MPDSystem.CockpitDisplays disp = min; disp <= max; disp++)
                BuildDisplay(devMap[disp], _cfg.MPD.Displays[(int)disp]);
        }

        /// <summary>
        /// TODO
        /// </summary>
        private void BuildDisplay(AirframeDevice dispDev, MPDConfiguration dispConfig)
        {
            AddIfBlock("IsDisplayInMainMenu", false, new() { $"{dispDev.Name}" }, delegate ()
            {
                AddAction(dispDev, "PB11");
                AddWait(WAIT_BASE);
            });
            AddIfBlock("IsDisplayInMainMenu", false, new() { $"{dispDev.Name}" }, delegate ()
            {
                AddAction(dispDev, "PB11");
                AddWait(WAIT_BASE);
            });
            AddIfBlock("IsProgBoxed", true, new() { $"{dispDev.Name}" }, delegate ()
            {
                AddAction(dispDev, "PB06");
                AddWait(WAIT_BASE);
            });

            AddIfBlock("IsNoDisplaysProgrammed", true, new() { dispDev.Name }, delegate ()
            {
                AddAction(dispDev, "PB06");
                for (int i = 0; i < MPDConfiguration.NUM_SEQUENCES; i++)
                    AddActions(dispDev, ButtonsToSelectFormat(dispConfig.Formats[i]));
                for (int i = 0; i < MPDConfiguration.NUM_SEQUENCES; i++)
                {
                    List<string> modeButtons = ButtonsToEnterMode(dispConfig.Modes[i]);
                    if (modeButtons.Count > 0)
                    {
                        AddActions(dispDev, modeButtons);
                        AddActions(dispDev, ButtonsToSelectFormat(dispConfig.Formats[i]));
                        AddActions(dispDev, ButtonsToExitMode(dispConfig.Modes[i]));
                    }
                }
                AddAction(dispDev, "PB06");

                List<string> fmtButtons = ButtonsToSelectFormat(dispConfig.Formats[0]);
                if (fmtButtons.Count > 0)
                    AddActions(dispDev, fmtButtons);
            });
        }

        /// <summary>
        /// TODO
        /// </summary>
        private static List<string> ButtonsToSelectFormat(string formatStr, bool returnToRoot = true)
        {
            MPDConfiguration.DisplayFormats format = (string.IsNullOrEmpty(formatStr))
                ? MPDConfiguration.DisplayFormats.NONE
                : (MPDConfiguration.DisplayFormats)int.Parse(formatStr);
            return format switch
            {
                MPDConfiguration.DisplayFormats.ADI => new() { "PB01" },
                MPDConfiguration.DisplayFormats.ARMT => new() { "PB02" },
                MPDConfiguration.DisplayFormats.HSI => new() { "PB03" },
                MPDConfiguration.DisplayFormats.TF => new() { "PB04" },
                MPDConfiguration.DisplayFormats.TSD => new() { "PB05" },
                MPDConfiguration.DisplayFormats.TPOD => new() { "PB12" },
                MPDConfiguration.DisplayFormats.TEWS => new() { "PB13" },
                MPDConfiguration.DisplayFormats.AG_RDR => new() { "PB14" },
                MPDConfiguration.DisplayFormats.AA_RDR => new() { "PB15" },
                MPDConfiguration.DisplayFormats.HUD => new() { "PB17" },
                MPDConfiguration.DisplayFormats.ENG => new() { "PB18" },
                MPDConfiguration.DisplayFormats.AG_DLVRY => (returnToRoot) ? new() { "PB11", "PB02", "PB11", "PB11" }
                                                                           : new() { "PB11", "PB02" },
                MPDConfiguration.DisplayFormats.SMRT_WPNS => (returnToRoot) ? new() { "PB11", "PB14", "PB11", "PB11" }
                                                                            : new() { "PB11", "PB14" },
                _ => new() { }
            };
        }

        /// <summary>
        /// TODO
        /// </summary>
        private static List<string> ButtonsToEnterMode(string modeStr)
        {
            MPDConfiguration.MasterModes mode = (string.IsNullOrEmpty(modeStr))
                ? MPDConfiguration.MasterModes.NONE
                : (MPDConfiguration.MasterModes)int.Parse(modeStr);
            return mode switch
            {
                MPDConfiguration.MasterModes.A2A => new() { "PB07" },
                MPDConfiguration.MasterModes.A2G => new() { "PB07", "PB07" },
                MPDConfiguration.MasterModes.NAV => new() { "PB07", "PB07", "PB07" },
                _ => new() { }
            };
        }

        /// <summary>
        /// TODO
        /// </summary>
        private static List<string> ButtonsToExitMode(string modeStr)
        {
            MPDConfiguration.MasterModes mode = (string.IsNullOrEmpty(modeStr))
                ? MPDConfiguration.MasterModes.NONE
                : (MPDConfiguration.MasterModes)int.Parse(modeStr);
            return mode switch
            {
                MPDConfiguration.MasterModes.A2A => new() { "PB07", "PB07", "PB07" },
                MPDConfiguration.MasterModes.A2G => new() { "PB07", "PB07" },
                MPDConfiguration.MasterModes.NAV => new() { "PB07" },
                _ => new() { }
            };
        }
    }
}