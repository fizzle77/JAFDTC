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
    /// builder to generate the command stream to configure the mfd formats mapped to osb 12-14 along with the
    /// selected format for each master mode through the mfds according to an F16CConfiguration. the stream returns
    /// the master mode to nav. the builder requires the following state:
    /// 
    ///     MFDModeConfig.{mode}: MFDModeConfiguration
    ///         default format and current mfd formats for master mode "mode" (MFDSystem.MasterModes)
    ///
    /// see MFDStateQueryBuilder for further details.
    /// </summary>
    internal class MFDBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private Dictionary<string, object> _state;

        private readonly Dictionary<MFDConfiguration.DisplayFormats, string> _mapFormatToOSB;

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
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure mfd formats via the ded/ufc according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// 
        ///     MFDModeConfig.{mode}, MFDModeConfiguration
        ///         default format and current mfd formats for master mode "mode" (MFDSystem.MasterModes)
        /// 
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.MFD.IsDefault)
                return;

            AddExecFunction("NOP", new() { "MFDBuilder:Build()" });

            _state = state;

            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice hotas = _aircraft.GetDevice("HOTAS");
            AirframeDevice mfdL = _aircraft.GetDevice("LMFD");
            AirframeDevice mfdR = _aircraft.GetDevice("RMFD");

            MFDSystem tgtMFD = (MFDSystem)_cfg.MFD.Clone();
            MFDSystem dflMFD = MFDSystem.ExplicitDefaults;

            AddActions(ufc, new() { "RTN", "RTN", "LIST", "8" }, null, WAIT_BASE);
            AddIfBlock("IsInAAMode", true, null, delegate ()
            {
                AddAction(ufc, "SEQ");
                AddIfBlock("IsInAGMode", true, null, delegate ()
                {
                    for (int mode = 0; mode < (int)MFDSystem.MasterModes.NUM_MODES; mode++)
                        if (state.TryGetValueAs($"MFDModeConfig.{(MFDSystem.MasterModes)mode}",
                                                out MFDModeConfiguration curMFD))
                        {
                            MergeConfigs((MFDSystem.MasterModes) mode, tgtMFD.ModeConfigs[mode].LeftMFD,
                                         curMFD.LeftMFD, dflMFD.ModeConfigs[mode].LeftMFD);
                            MergeConfigs((MFDSystem.MasterModes) mode, tgtMFD.ModeConfigs[mode].RightMFD,
                                         curMFD.RightMFD, dflMFD.ModeConfigs[mode].RightMFD);

                            BuildMFDsForMode((MFDSystem.MasterModes)mode, ufc, hotas, mfdL, mfdR,
                                             tgtMFD.ModeConfigs[mode], curMFD.LeftMFD.SelectedOSB,
                                             curMFD.RightMFD.SelectedOSB);
                        }
                });
                AddActions(ufc, new() { "RTN", "RTN", "LIST", "8", "SEQ" });
            });
            AddAction(ufc, "RTN");
        }

        /// <summary>
        /// returns true if the format should always be configured (even if it is already set up on a particular osb),
        /// false otherwise. currently, had and sms formats are always set as they may link to other configuration.
        /// </summary>
        private static bool IsFormatAlwaysConfigured(MFDSystem.MasterModes mode, string format)
            => ((format == ((int)MFDConfiguration.DisplayFormats.HAD).ToString()) ||
                //
                // TODO: currently only sms setup is for a2g, consider other modes here if a2a sms support added?
                //
                ((mode == MFDSystem.MasterModes.ICP_AG) &&
                 (format == ((int)MFDConfiguration.DisplayFormats.SMS).ToString())));

        /// <summary>
        /// merge the target, current, and default configurations to determine the target configuration we are trying
        /// to load. target configuration is updated with defaults expanded and defaults set where setup agrees with
        /// the current settings (except for IsFormatAlwaysConfigured formats).
        /// </summary>
        private static void MergeConfigs(MFDSystem.MasterModes mode, MFDConfiguration cfgTgt, MFDConfiguration cfgCur,
                                         MFDConfiguration cfgDft)
        {
            string osb12 = (!string.IsNullOrEmpty(cfgTgt.OSB12)) ? cfgTgt.OSB12 : cfgDft.OSB12;
            string osb13 = (!string.IsNullOrEmpty(cfgTgt.OSB13)) ? cfgTgt.OSB13 : cfgDft.OSB13;
            string osb14 = (!string.IsNullOrEmpty(cfgTgt.OSB14)) ? cfgTgt.OSB14 : cfgDft.OSB14;

            cfgTgt.SelectedOSB = (!string.IsNullOrEmpty(cfgTgt.SelectedOSB)) ? cfgTgt.SelectedOSB : cfgDft.SelectedOSB;
            cfgTgt.OSB12 = ((cfgCur.OSB12 != osb12) || IsFormatAlwaysConfigured(mode, osb12)) ? osb12 : null;
            cfgTgt.OSB13 = ((cfgCur.OSB13 != osb13) || IsFormatAlwaysConfigured(mode, osb13)) ? osb13 : null;
            cfgTgt.OSB14 = ((cfgCur.OSB14 != osb14) || IsFormatAlwaysConfigured(mode, osb14)) ? osb14 : null;
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

            BuildMFD(mode, mfdL, "left", $"OSB-{curLeftOSB}", tgtMFD.LeftMFD);
            BuildMFD(mode, mfdR, "right", $"OSB-{curRightOSB}", tgtMFD.RightMFD);

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
        private void BuildMFD(MFDSystem.MasterModes mode, AirframeDevice mfd, string mfdSide, string curSelOSB,
                              MFDConfiguration tgtMFD)
        {
            // update the format assignments to osb12-14. note that this will leave osb14 selected after format
            // assignments are finished.
            //
            curSelOSB = BuildPage(mode, mfd, mfdSide, curSelOSB, "OSB-12", tgtMFD.OSB12);
            curSelOSB = BuildPage(mode, mfd, mfdSide, curSelOSB, "OSB-13", tgtMFD.OSB13);
            curSelOSB = BuildPage(mode, mfd, mfdSide, curSelOSB, "OSB-14", tgtMFD.OSB14);

            if ($"OSB-{tgtMFD.SelectedOSB}" != curSelOSB)
            {
                AddAction(mfd, $"OSB-{tgtMFD.SelectedOSB}");
            }
        }

        /// <summary>
        /// sets up a particular osb to map to a particular format on an mfd in the current master mode. if the hts
        /// format is specified, the enabled threat classes (per the hts system configuration) will also be set up.
        /// returns selected osb post operation: either be the current selection (if we're setting the page tied
        /// to the currently selected osb) or the new selection (otherwise).
        /// </summary>
        private string BuildPage(MFDSystem.MasterModes mode, AirframeDevice mfd, string mfdSide, string osbSel,
                                 string osbTgt, string page)
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
                    ConfigureHADFormatThreats(mfdSide);
                }
                else if ((format == MFDConfiguration.DisplayFormats.SMS) &&
                         ((mode == MFDSystem.MasterModes.ICP_AG) /* TODO: ||(mode == MFDSystem.MasterModes.ICP_AG) */))
                {
                    ConfigureSMSFormat(mfd, mfdSide);
                }
                return osbTgt;
            }
            return osbSel;
        }

        /// <summary>
        /// configure the enabled threats in the had format. assumes the had format is selected on the indicated mfd.
        /// </summary>
        private void ConfigureHADFormatThreats(string mfdSide)
        {
            HTSThreatEnableBuilder builder = new(_cfg, (F16CDeviceManager)_aircraft, null);
            AddBuild(builder, new() { ["mfdSide"] = mfdSide });
        }

        /// <summary>
        /// clear inv mode and configure the munitions in the sms format. assumes the sms format is selected on the
        /// indicated mfd.
        /// </summary>
        private void ConfigureSMSFormat(AirframeDevice mfd, string mfdSide)
        {
            AddIfBlock("IsSMSOnINV", true, new() { mfdSide }, delegate ()
            {
                AddAction(mfd, "OSB-04", WAIT_BASE);
            });
            SMSBuilder builder = new(_cfg, (F16CDeviceManager)_aircraft, null);
            AddBuild(builder, _state);
            // TODO: allow configuration of "to inv, or not to inv? that is the question..."
        }
    }
}