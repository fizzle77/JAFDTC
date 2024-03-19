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
        public override void Build()
        {
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

            if (!_cfg.MPD.IsCrewMemberDefault(MPDSystem.CrewPositions.PILOT))
            {
                AddIfBlock("IsInFrontCockpit", null, delegate ()
                {
                    AddRunFunction("GoToFrontCockpit");
                    AddWait(2 * WAIT_LONG);
                    BuildDisplays(devMap, MPDSystem.CockpitDisplays.PILOT_L_MPD, MPDSystem.CockpitDisplays.PILOT_R_MPD);
                });
            }
            if (!_cfg.MPD.IsCrewMemberDefault(MPDSystem.CrewPositions.WSO))
            {
                AddIfBlock("IsInRearCockpit", null, delegate ()
                {
                    AddRunFunction("GoToRearCockpit");
                    AddWait(2 * WAIT_LONG);
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
            {
                BuildDisplay(devMap[disp], _cfg.MPD.Displays[(int)disp]);
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        private void BuildDisplay(AirframeDevice dispDev, MPDConfiguration dispConfig)
        {
            AddIfBlock("IsDisplayNotInMainMenu", new() { $"{dispDev.Name}" }, delegate ()
            {
                AddAction(dispDev, "PB11");
                AddWait(WAIT_BASE);
            });
            AddIfBlock("IsDisplayNotInMainMenu", new() { $"{dispDev.Name}" }, delegate ()
            {
                AddAction(dispDev, "PB11");
                AddWait(WAIT_BASE);
            });
            AddIfBlock("IsProgBoxed", new() { $"{dispDev.Name}" }, delegate ()
            {
                AddAction(dispDev, "PB06");
                AddWait(WAIT_BASE);
            });

            AddIfBlock("NoDisplaysProgrammed", new() { dispDev.Name }, delegate ()
            {
                AddAction(dispDev, "PB06");
                for (int i = 0; i < MPDConfiguration.NUM_SEQUENCES; i++)
                {
                    AddActions(dispDev, ButtonsToSelectFormat(dispConfig.Formats[i]));
                }
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
                {
                    AddActions(dispDev, fmtButtons);
                }
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


#if NOPE

using DTC.New.Presets.V2.Aircrafts.F15E.Systems;
using DTC.New.Uploader.Base;

namespace DTC.New.Uploader.Aircrafts.F15E;

public partial class F15EUploader : Base.Uploader
{

    private void ConfigureFrontDisplays()
    {
        if (config.Displays.Pilot.LeftMPD.FirstDisplay != Display.None)
        {
            BuildDisplay(FLMPD, config.Displays.Pilot.LeftMPD);
        }

        if (config.Displays.Pilot.RightMPD.FirstDisplay != Display.None)
        {
            BuildDisplay(FRMPD, config.Displays.Pilot.RightMPD);
        }

        if (config.Displays.Pilot.MPCD.FirstDisplay != Display.None)
        {
            BuildDisplay(FMPCD, config.Displays.Pilot.MPCD);
        }
    }

    private void ConfigureRearDisplays()
    {
        if (config.Displays.WSO.LeftMPCD.FirstDisplay != Display.None)
        {
            BuildDisplay(RLMPCD, config.Displays.WSO.LeftMPCD);
        }

        if (config.Displays.WSO.LeftMPD.FirstDisplay != Display.None)
        {
            BuildDisplay(RLMPD, config.Displays.WSO.LeftMPD);
        }

        if (config.Displays.WSO.RightMPD.FirstDisplay != Display.None)
        {
            BuildDisplay(RRMPD, config.Displays.WSO.RightMPD);
        }

        if (config.Displays.WSO.RightMPCD.FirstDisplay != Display.None)
        {
            BuildDisplay(RRMPCD, config.Displays.WSO.RightMPCD);
        }
    }

    private void BuildDisplay(Device display, DisplayConfig config)
    {
        NavigateToMainMenu(display);

        StartIf(NoDisplaysProgrammed(display));
        {
            Cmd(display.GetCommand("PB06"));

            SelectDisplay(display, config.FirstDisplay);

            if (config.SecondDisplay != Display.None)
            {
                SelectDisplay(display, config.SecondDisplay);
            }

            if (config.ThirdDisplay != Display.None)
            {
                SelectDisplay(display, config.ThirdDisplay);
            }

            if (config.FirstDisplayMode != DisplayMode.None)
            {
                NavigateToDisplayMode(display, config.FirstDisplayMode);
                SelectDisplay(display, config.FirstDisplay);
                ExitDisplayMode(display, config.FirstDisplayMode);
            }

            if (config.SecondDisplayMode != DisplayMode.None)
            {
                NavigateToDisplayMode(display, config.SecondDisplayMode);
                SelectDisplay(display, config.SecondDisplay);
                ExitDisplayMode(display, config.SecondDisplayMode);
            }

            if (config.ThirdDisplayMode != DisplayMode.None)
            {
                NavigateToDisplayMode(display, config.ThirdDisplayMode);
                SelectDisplay(display, config.ThirdDisplay);
                ExitDisplayMode(display, config.ThirdDisplayMode);
            }

            Cmd(display.GetCommand("PB06"));

            SelectDisplay(display, config.FirstDisplay, false);
        }
        EndIf();
    }

    private void NavigateToDisplayMode(Device device, DisplayMode displayMode)
    {
        Cmd(device.GetCommand("PB07"));

        if (displayMode == DisplayMode.AG)
        {
            Cmd(device.GetCommand("PB07"));
        }
        if (displayMode == DisplayMode.NAV)
        {
            Cmd(device.GetCommand("PB07"));
            Cmd(device.GetCommand("PB07"));
        }
    }

    private void ExitDisplayMode(Device device, DisplayMode displayMode)
    {
        Cmd(device.GetCommand("PB07"));

        if (displayMode == DisplayMode.AG)
        {
            Cmd(device.GetCommand("PB07"));
        }
        if (displayMode == DisplayMode.AA)
        {
            Cmd(device.GetCommand("PB07"));
            Cmd(device.GetCommand("PB07"));
        }
    }


    private Condition NoDisplaysProgrammed(Device display)
    {
        return new Condition($"NoDisplaysProgrammed('{display.Name}')");
    }

    private CustomCommand GoToFrontCockpit()
    {
        return new CustomCommand("GoToFrontCockpit()");
    }

    private CustomCommand GoToRearCockpit()
    {
        return new CustomCommand("GoToRearCockpit()");
    }
}

#endif