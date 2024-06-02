// ********************************************************************************************************************
//
// MFDBuilder.cs -- f-16c mfd command builder
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
using JAFDTC.Models.F16C.MFD;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the mfd format setup in the viper. translates cmds setup in F16CConfiguration into commands
    /// that drive the dcs clickable cockpit.
    /// </summary>
    internal class MFDBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly Dictionary<MFDConfiguration.DisplayFormats, string> _mapFormatToOSB;
        private readonly string[] _htsThreatToOSB;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MFDBuilder(F16CConfiguration cfg, F16CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb)
        {
            _mapFormatToOSB = new()
            {
                [MFDConfiguration.DisplayFormats.BLANK] = "OSB-01",
                [MFDConfiguration.DisplayFormats.DTE] = "OSB-08",
                [MFDConfiguration.DisplayFormats.FCR] = "OSB-20",
                [MFDConfiguration.DisplayFormats.FLCS] = "OSB-10",
                [MFDConfiguration.DisplayFormats.HAD] = "OSB-02",
                [MFDConfiguration.DisplayFormats.HSD] = "OSB-07",
                [MFDConfiguration.DisplayFormats.SMS] = "OSB-06",
                [MFDConfiguration.DisplayFormats.TEST] = "OSB-09",
                [MFDConfiguration.DisplayFormats.TGP] = "OSB-19",
                [MFDConfiguration.DisplayFormats.WPN] = "OSB-18"
                //
                // following are not supported by dcs.
                //
                // case Formats.FLIR:      // OSB 16
                // case Formats.RCCE:      // OSB 4
                // case Formats.TFR:       // OSB 17
            };

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
        /// configure mfd formats via the icp/ded according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice hotas = _aircraft.GetDevice("HOTAS");
            AirframeDevice mfdL = _aircraft.GetDevice("LMFD");
            AirframeDevice mfdR = _aircraft.GetDevice("RMFD");

            if (!_cfg.MFD.IsDefault)
            {
                MFDSystem tgtMFD = (MFDSystem)_cfg.MFD.Clone();
                MFDSystem dflMFD = MFDSystem.ExplicitDefaults;
                MFDSystem curMFD = new();

                MFDStateQueryBuilder query = new(_aircraft, new StringBuilder());
                query.QueryCurrentMFDStateForAllModes(curMFD.ModeConfigs);

                AddActions(ufc, new() { "RTN", "RTN", "LIST", "8" }, null, WAIT_BASE);
                AddIfBlock("IsInAAMode", true, null, delegate ()
                {
                    AddAction(ufc, "SEQ");
                    AddIfBlock("IsInAGMode", true, null, delegate ()
                    {
                        for (int mode = 0; mode < (int)MFDSystem.MasterModes.NUM_MODES; mode++)
                        {
                            MergeConfigs(tgtMFD.ModeConfigs[mode].LeftMFD,
                                         curMFD.ModeConfigs[mode].LeftMFD, dflMFD.ModeConfigs[mode].LeftMFD);
                            MergeConfigs(tgtMFD.ModeConfigs[mode].RightMFD,
                                         curMFD.ModeConfigs[mode].RightMFD, dflMFD.ModeConfigs[mode].RightMFD);

                            BuildMFDsForMode((MFDSystem.MasterModes)mode, ufc, hotas, mfdL, mfdR,
                                             tgtMFD.ModeConfigs[mode],
                                             curMFD.ModeConfigs[mode].LeftMFD.SelectedOSB,
                                             curMFD.ModeConfigs[mode].RightMFD.SelectedOSB);
                        }
                    });
                    AddActions(ufc, new() { "RTN", "RTN", "LIST", "8", "SEQ" });
                });
                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// merge the target, current, and default configurations to determine the target configuration we are trying
        /// to load. target configuration is updated with defaults expanded (ie, not "").
        /// </summary>
        private static void MergeConfigs(MFDConfiguration cfgTgt, MFDConfiguration cfgCur, MFDConfiguration cfgDft)
        {
            string osb12 = (!string.IsNullOrEmpty(cfgTgt.OSB12)) ? cfgTgt.OSB12 : cfgDft.OSB12;
            string osb13 = (!string.IsNullOrEmpty(cfgTgt.OSB13)) ? cfgTgt.OSB13 : cfgDft.OSB13;
            string osb14 = (!string.IsNullOrEmpty(cfgTgt.OSB14)) ? cfgTgt.OSB14 : cfgDft.OSB14;

            cfgTgt.SelectedOSB = (!string.IsNullOrEmpty(cfgTgt.SelectedOSB)) ? cfgTgt.SelectedOSB : cfgDft.SelectedOSB;
            cfgTgt.OSB12 = (cfgCur.OSB12 != osb12) ? osb12 : null;
            cfgTgt.OSB13 = (cfgCur.OSB13 != osb13) ? osb13 : null;
            cfgTgt.OSB14 = (cfgCur.OSB14 != osb14) ? osb14 : null;
        }

        /// <summary>
        /// builds the format set ups for the left/right mfds in a master mode based on configuration. command stream
        /// starts by setting the master mode, then programming the formats, selecting the current format, and
        /// returning to nav master mode. only non-default setups are processed.
        /// </summary>
        private void BuildMFDsForMode(MFDSystem.MasterModes mode, AirframeDevice ufc, AirframeDevice hotas,
                                      AirframeDevice mfdL, AirframeDevice mfdR, MFDModeConfiguration tgtMFD,
                                      string curLeftOSB, string curRightOSB)
        {
            string masterMode = (mode == MFDSystem.MasterModes.ICP_AA) ? "AA" : "AG";
            if (mode == MFDSystem.MasterModes.DGFT_DGFT)
            {
                AddAction(hotas, "DGFT");
            }
            else if (mode == MFDSystem.MasterModes.DGFT_MSL)
            {
                AddAction(hotas, "MSL");
            }
            else if(mode != MFDSystem.MasterModes.NAV)
            {
                AddAction(ufc, masterMode);
            }

            BuildMFD(mfdL, "left", $"OSB-{curLeftOSB}", tgtMFD.LeftMFD);
            BuildMFD(mfdR, "right", $"OSB-{curRightOSB}", tgtMFD.RightMFD);

            if ((mode == MFDSystem.MasterModes.DGFT_DGFT) || (mode == MFDSystem.MasterModes.DGFT_MSL))
            {
                AddAction(hotas, "CENTER");
            }
            else if (mode != MFDSystem.MasterModes.NAV)
            {
                AddAction(ufc, masterMode);
            }
        }

        /// <summary>
        /// builds the mfd format set ups for a single mfd in the current master mode and sets the initial selected
        /// format based on configuration.
        /// </summary>
        private void BuildMFD(AirframeDevice mfd, string mfdSide, string curSelOSB, MFDConfiguration tgtMFD)
        {
            // update the format assignments to osb12-14. note that this will leave osb14 selected after format
            // assignments are finished.
            //
            curSelOSB = BuildPage(mfd, mfdSide, curSelOSB, "OSB-12", tgtMFD.OSB12);
            curSelOSB = BuildPage(mfd, mfdSide, curSelOSB, "OSB-13", tgtMFD.OSB13);
            curSelOSB = BuildPage(mfd, mfdSide, curSelOSB, "OSB-14", tgtMFD.OSB14);

            if ($"OSB-{tgtMFD.SelectedOSB}" != curSelOSB)
            {
                AddAction(mfd, $"OSB-{tgtMFD.SelectedOSB}");
            }
        }

        /// <summary>
        /// sets up a particular osb to map to a particular format on an mfd in the current master mode. if the hts
        /// format is specified, the enabled threat classes (per the hts system configuration) will also be set up.
        /// returns selected osb post operation (will either be the current selection or the new selection.
        /// </summary>
        private string BuildPage(AirframeDevice mfd, string mfdSide, string osbSel, string osbTgt, string page)
        {
            if (!string.IsNullOrEmpty(page) && (osbSel != osbTgt))
            {
                AddAction(mfd, osbTgt);
            }
            if (!string.IsNullOrEmpty(page))
            {
                MFDConfiguration.DisplayFormats format = (MFDConfiguration.DisplayFormats)int.Parse(page);
                AddActions(mfd, new() { osbTgt, _mapFormatToOSB[format] });
                if (format == MFDConfiguration.DisplayFormats.HAD)
                {
                    AddIfBlock("IsHTSOnMFD", true, new() { mfdSide }, delegate ()
                    {
                        BuildHTSOnMFDIfOn(mfd, mfdSide);
                    });
                }
                else if (format == MFDConfiguration.DisplayFormats.SMS)
                {
                    AddAction(mfd, "OSB-04");
                }
                return osbTgt;
            }
            return osbSel;
        }

        /// <summary>
        /// sets up the hts threat classes on the hts mfd format. had format should be displayed at the time
        /// these commands execute.
        /// </summary>
        private void BuildHTSOnMFDIfOn(AirframeDevice mfd, string mfdSide)
        {
            // ASSUMES: HTS enables TC1-TC11 and disables MAN threats by default. OSB-5 (ALL) used to flip that
            // ASSUMES: around so we start from TC1-11 disabled and MAN enabled.
            //
            AddAction(mfd, "OSB-04");

            AddIfBlock("HTSAllNotSelected", true, new() { mfdSide }, delegate ()
            {
                AddAction(mfd, "OSB-05");
            });
            AddAction(mfd, "OSB-05");

            // TODO: would be nice to do the button presses conditionally based on current state. this will
            // TODO: require some post-setup command sequencing though.
            //
            if (!_cfg.HTS.EnabledThreats[0])
            {
                AddAction(mfd, _htsThreatToOSB[0]);
            }
            for (int i = 1; i < _htsThreatToOSB.Length; i++)
            {
                if (_cfg.HTS.EnabledThreats[i])
                {
                    AddAction(mfd, _htsThreatToOSB[i]);
                }
            }

            AddAction(mfd, "OSB-04");
        }
    }
}