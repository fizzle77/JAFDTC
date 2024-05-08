// ********************************************************************************************************************
//
// CMDSBuilder.cs -- f-16c mfd command builder
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

        private readonly MFDSystem _dfltMFD;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public MFDBuilder(F16CConfiguration cfg, F16CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb)
        {
            _dfltMFD = MFDSystem.ExplicitDefaults;
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
            AirframeDevice leftMFD = _aircraft.GetDevice("LMFD");
            AirframeDevice rightMFD = _aircraft.GetDevice("RMFD");

            if (!_cfg.MFD.IsDefault)
            {
                AddActions(ufc, new() { "RTN", "RTN", "LIST", "8" });
                AddWait(WAIT_BASE);

                AddIfBlock("NotInAAMode", true, null, delegate ()
                {
                    AddAction(ufc, "SEQ");
                    AddIfBlock("NotInAGMode", true, null, delegate () { BuildMFDs(ufc, hotas, leftMFD, rightMFD); });
                    AddActions(ufc, new() { "RTN", "RTN", "LIST", "8", "SEQ" });
                });

                AddAction(ufc, "RTN");
            }
        }

        /// <summary>
        /// builds the format set ups for the left/right mfds in each master mode based on configuration. only non-
        /// default setups are processed.
        /// </summary>
        private void BuildMFDs(AirframeDevice ufc, AirframeDevice hotas, AirframeDevice leftMFD, AirframeDevice rightMFD)
        {
            for (int mode = 0; mode < _cfg.MFD.ModeConfigs.Length; mode++)
            {
                MFDModeConfiguration config = _cfg.MFD.ModeConfigs[mode];
                if (config.IsDefault) 
                    continue;

                if ((MFDSystem.MasterModes)mode == MFDSystem.MasterModes.ICP_AA)
                {
                    AddAction(ufc, "AA");
                }
                else if ((MFDSystem.MasterModes)mode == MFDSystem.MasterModes.ICP_AG)
                {
                    AddAction(ufc, "AG");
                }
                else if ((MFDSystem.MasterModes)mode == MFDSystem.MasterModes.DGFT_DGFT)
                {
                    AddAction(hotas, "DGFT");
                }
                else if ((MFDSystem.MasterModes)mode == MFDSystem.MasterModes.DGFT_MSL)
                {
                    AddAction(hotas, "MSL");
                }

                BuildMFD(mode, true, leftMFD, config.LeftMFD);
                BuildMFD(mode, false, rightMFD, config.RightMFD);

                if ((MFDSystem.MasterModes)mode == MFDSystem.MasterModes.ICP_AA)
                {
                    AddAction(ufc, "AA");
                }
                else if ((MFDSystem.MasterModes)mode == MFDSystem.MasterModes.ICP_AG)
                {
                    AddAction(ufc, "AG");
                }
                else if (((MFDSystem.MasterModes)mode == MFDSystem.MasterModes.DGFT_DGFT) ||
                         ((MFDSystem.MasterModes)mode == MFDSystem.MasterModes.DGFT_MSL))
                {
                    AddAction(hotas, "CENTER");
                }
            }
        }

        /// <summary>
        /// builds the mfd format set ups for a single mfd in the current master mode and sets the initial selected
        /// format based on configuration.
        /// </summary>
        private void BuildMFD(int mode, bool isLeftMFD, AirframeDevice mfd, MFDConfiguration mfdConfig)
        {
            BuildPage(mfd, isLeftMFD, "OSB-12-PG3", mfdConfig.OSB12);
            BuildPage(mfd, isLeftMFD, "OSB-13-PG2", mfdConfig.OSB13);
            BuildPage(mfd, isLeftMFD, "OSB-14-PG1", mfdConfig.OSB14);

            string selOSB = mfdConfig.SelectedOSB;
            if (string.IsNullOrEmpty(selOSB) )
            {
                selOSB = (isLeftMFD) ? _dfltMFD.ModeConfigs[mode].LeftMFD.SelectedOSB
                                     : _dfltMFD.ModeConfigs[mode].RightMFD.SelectedOSB;
            }
            if (selOSB == "12")
            {
                AddAction(mfd, "OSB-12-PG3");
            }
            else if (selOSB == "13")
            {
                AddAction(mfd, "OSB-13-PG2");
            }
        }

        /// <summary>
        /// sets up a particular osb to map to a particular format on an mfd in the current master mode. if the hts
        /// format is specified, the enabled threat classes (per the hts system configuration) will also be set up.
        /// </summary>
        private void BuildPage(AirframeDevice mfd, bool isLeftMFD, string osb, string page)
        {
            string mfdSide = (isLeftMFD) ? "left" : "right";

            AddAction(mfd, osb);

            if (!string.IsNullOrEmpty(page))
            {
                AddAction(mfd, osb);

                switch ((MFDConfiguration.DisplayFormats)int.Parse(page))
                {
                    case MFDConfiguration.DisplayFormats.BLANK:
                        AddAction(mfd, "OSB-01-BLANK");
                        break;
                    case MFDConfiguration.DisplayFormats.DTE:
                        AddAction(mfd, "OSB-08-DTE");
                        break;
                    case MFDConfiguration.DisplayFormats.FCR:
                        AddAction(mfd, "OSB-20-FCR");
                        break;
                    case MFDConfiguration.DisplayFormats.FLCS:
                        AddAction(mfd, "OSB-10-FLCS");
                        break;
                    case MFDConfiguration.DisplayFormats.HAD:
                        AddAction(mfd, "OSB-02-HAD");
                        AddIfBlock("HTSOnMFD", true, new() { mfdSide }, delegate ()
                        {
                            BuildHTSOnMFDIfOn(mfd, isLeftMFD);
                        });
                        break;
                    case MFDConfiguration.DisplayFormats.HSD:
                        AddAction(mfd, "OSB-07-HSD");
                        break;
                    case MFDConfiguration.DisplayFormats.SMS:
                        AddAction(mfd, "OSB-06-SMS");
                        AddAction(mfd, "OSB-04-RCCE");     // disable INV
                        break;
                    case MFDConfiguration.DisplayFormats.TEST:
                        AddAction(mfd, "OSB-09-TEST");
                        break;
                    case MFDConfiguration.DisplayFormats.TGP:
                        AddAction(mfd, "OSB-19-TGP");
                        break;
                    case MFDConfiguration.DisplayFormats.WPN:
                        AddAction(mfd, "OSB-18-WPN");
                        break;
                        //
                        // following are not supported by dcs.
                        //
                        // case Formats.FLIR:      // OSB 16
                        // case Formats.RCCE:      // OSB 4
                        // case Formats.TFR:       // OSB 17
                }
            }
        }

        /// <summary>
        /// sets up the hts threat classes on the hts mfd format.
        /// </summary>
        private void BuildHTSOnMFDIfOn(AirframeDevice mfd, bool isLeftMFD)
        {
            AddAction(mfd, "OSB-04-RCCE");

            string mfdSide = (isLeftMFD) ? "left" : "right";
            AddIfBlock("HTSAllNotSelected", true, new() { mfdSide }, delegate () { AddAction(mfd, "OSB-05"); });

            AddAction(mfd, "OSB-05");

            // ASSUMES: HTS enables TC1-TC11 and disables MAN threats by default. OSB-5 (ALL)
            // ASSUMES: used to flip that around.
            //
            // TODO: would be nice to do the button presses conditionally based on current state.
            //
            if (!_cfg.HTS.EnabledThreats[0])                            // MAN
                AddAction(mfd, "OSB-02-HAD");
            if (_cfg.HTS.EnabledThreats[1])                             // TC 1
                AddAction(mfd, "OSB-20-FCR");
            if (_cfg.HTS.EnabledThreats[2])
                AddAction(mfd, "OSB-19-TGP");
            if (_cfg.HTS.EnabledThreats[3])
                AddAction(mfd, "OSB-18-WPN");
            if (_cfg.HTS.EnabledThreats[4])
                AddAction(mfd, "OSB-17-TFR");
            if (_cfg.HTS.EnabledThreats[5])
                AddAction(mfd, "OSB-16-FLIR");
            if (_cfg.HTS.EnabledThreats[6])
                AddAction(mfd, "OSB-06-SMS");
            if (_cfg.HTS.EnabledThreats[7])
                AddAction(mfd, "OSB-07-HSD");
            if (_cfg.HTS.EnabledThreats[8])
                AddAction(mfd, "OSB-08-DTE");
            if (_cfg.HTS.EnabledThreats[9])
                AddAction(mfd, "OSB-09-TEST");
            if (_cfg.HTS.EnabledThreats[10])
                AddAction(mfd, "OSB-10-FLCS");
            if (_cfg.HTS.EnabledThreats[11])                            // TC 11
                AddAction(mfd, "OSB-01-BLANK");

            AddAction(mfd, "OSB-04-RCCE");
        }
    }
}