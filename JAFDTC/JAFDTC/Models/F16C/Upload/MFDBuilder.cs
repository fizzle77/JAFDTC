// ********************************************************************************************************************
//
// CMDSBuilder.cs -- f-16c mfd command builder
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
using JAFDTC.Models.F16C.MFD;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the mfd format setup in the viper. translates cmds setup in F16CConfiguration into commands
    /// that drive the dcs clickable cockpit.
    /// </summary>
    internal class MFDBuilder : F16CBuilderBase
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

        public MFDBuilder(F16CConfiguration cfg, F16CCommands f16c, StringBuilder sb) : base(cfg, f16c, sb)
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
            Device ufc = _aircraft.GetDevice("UFC");
            Device hotas = _aircraft.GetDevice("HOTAS");
            Device leftMFD = _aircraft.GetDevice("LMFD");
            Device rightMFD = _aircraft.GetDevice("RMFD");

            if (!_cfg.MFD.IsDefault)
            {
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("8"));

                AppendCommand(Wait());

                AppendCommand(StartCondition("NotInAAMode"));

                AppendCommand(ufc.GetCommand("SEQ"));
                AppendCommand(StartCondition("NotInAGMode"));

                BuildMFDs(ufc, hotas, leftMFD, rightMFD);

                AppendCommand(EndCondition("NotInAGMode"));

                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("8"));
                AppendCommand(ufc.GetCommand("SEQ"));

                AppendCommand(EndCondition("NotInAAMode"));

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// builds the format set ups for the left/right mfds in each master mode based on configuration. only non-
        /// default setups are processed.
        /// </summary>
        private void BuildMFDs(Device ufc, Device hotas, Device leftMFD, Device rightMFD)
        {
            for (int mode = 0; mode < _cfg.MFD.ModeConfigs.Length; mode++)
            {
                MFDModeConfiguration config = _cfg.MFD.ModeConfigs[mode];
                if (config.IsDefault) 
                    continue;

                if ((Modes)mode == Modes.ICP_AA)
                {
                    AppendCommand(ufc.GetCommand("AA"));
                }
                else if ((Modes)mode == Modes.ICP_AG)
                {
                    AppendCommand(ufc.GetCommand("AG"));
                }
                else if ((Modes)mode == Modes.DGFT_DGFT)
                {
                    AppendCommand(hotas.GetCommand("DGFT"));
                }
                else if ((Modes)mode == Modes.DGFT_MSL)
                {
                    AppendCommand(hotas.GetCommand("MSL"));
                }

                BuildMFD(mode, true, leftMFD, config.LeftMFD);
                BuildMFD(mode, false, rightMFD, config.RightMFD);

                if ((Modes)mode == Modes.ICP_AA)
                {
                    AppendCommand(ufc.GetCommand("AA"));
                }
                else if ((Modes)mode == Modes.ICP_AG)
                {
                    AppendCommand(ufc.GetCommand("AG"));
                }
                else if (((Modes)mode == Modes.DGFT_DGFT) || ((Modes)mode == Modes.DGFT_MSL))
                {
                    AppendCommand(hotas.GetCommand("CENTER"));
                }
            }
        }

        /// <summary>
        /// builds the mfd format set ups for a single mfd in the current master mode and sets the initial selected
        /// format based on configuration.
        /// </summary>
        private void BuildMFD(int mode, bool isLeftMFD, Device mfd, MFDConfiguration mfdConfig)
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
                AppendCommand(mfd.GetCommand("OSB-12-PG3"));
            }
            else if (selOSB == "13")
            {
                AppendCommand(mfd.GetCommand("OSB-13-PG2"));
            }
        }

        /// <summary>
        /// sets up a particular osb to map to a particular format on an mfd in the current master mode. if the hts
        /// format is specified, the enabled threat classes (per the hts system configuration) will also be set up.
        /// </summary>
        private void BuildPage(Device mfd, bool isLeftMFD, string osb, string page)
        {
            AppendCommand(mfd.GetCommand(osb));

            if (!string.IsNullOrEmpty(page))
            {
                AppendCommand(mfd.GetCommand(osb));

                switch ((Formats)int.Parse(page))
                {
                    case Formats.BLANK:
                        AppendCommand(mfd.GetCommand("OSB-01-BLANK"));
                        break;
                    case Formats.DTE:
                        AppendCommand(mfd.GetCommand("OSB-08-DTE"));
                        break;
                    case Formats.FCR:
                        AppendCommand(mfd.GetCommand("OSB-20-FCR"));
                        break;
                    case Formats.FLCS:
                        AppendCommand(mfd.GetCommand("OSB-10-FLCS"));
                        break;
                    case Formats.HAD:
                        AppendCommand(mfd.GetCommand("OSB-02-HAD"));
                        AppendCommand(StartCondition("HTSOnMFD", isLeftMFD ? "left" : "right"));
                        BuildHTSOnMFDIfOn(mfd, isLeftMFD);
                        AppendCommand(EndCondition("HTSOnMFD"));
                        break;
                    case Formats.HSD:
                        AppendCommand(mfd.GetCommand("OSB-07-HSD"));
                        break;
                    case Formats.SMS:
                        AppendCommand(mfd.GetCommand("OSB-06-SMS"));
                        AppendCommand(mfd.GetCommand("OSB-04-RCCE"));     // disable INV
                        break;
                    case Formats.TEST:
                        AppendCommand(mfd.GetCommand("OSB-09-TEST"));
                        break;
                    case Formats.TGP:
                        AppendCommand(mfd.GetCommand("OSB-19-TGP"));
                        break;
                    case Formats.WPN:
                        AppendCommand(mfd.GetCommand("OSB-18-WPN"));
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
        private void BuildHTSOnMFDIfOn(Device mfd, bool isLeftMFD)
        {
            AppendCommand(mfd.GetCommand("OSB-04-RCCE"));

            AppendCommand(StartCondition("HTSAllNotSelected", isLeftMFD ? "left" : "right"));
            AppendCommand(mfd.GetCommand("OSB-05"));
            AppendCommand(EndCondition("HTSAllNotSelected"));

            AppendCommand(mfd.GetCommand("OSB-05"));

            // ASSUMES: HTS enables TC1-TC11 and disables MAN threats by default. OSB-5 (ALL)
            // ASSUMES: used to flip that around.
            //
            // TODO: would be nice to do the button presses conditionally based on current state.
            //
            if (!_cfg.HTS.EnabledThreats[0])                            // MAN
                AppendCommand(mfd.GetCommand("OSB-02-HAD"));
            if (_cfg.HTS.EnabledThreats[1])                             // TC 1
                AppendCommand(mfd.GetCommand("OSB-20-FCR"));
            if (_cfg.HTS.EnabledThreats[2])
                AppendCommand(mfd.GetCommand("OSB-19-TGP"));
            if (_cfg.HTS.EnabledThreats[3])
                AppendCommand(mfd.GetCommand("OSB-18-WPN"));
            if (_cfg.HTS.EnabledThreats[4])
                AppendCommand(mfd.GetCommand("OSB-17-TFR"));
            if (_cfg.HTS.EnabledThreats[5])
                AppendCommand(mfd.GetCommand("OSB-16-FLIR"));
            if (_cfg.HTS.EnabledThreats[6])
                AppendCommand(mfd.GetCommand("OSB-06-SMS"));
            if (_cfg.HTS.EnabledThreats[7])
                AppendCommand(mfd.GetCommand("OSB-07-HSD"));
            if (_cfg.HTS.EnabledThreats[8])
                AppendCommand(mfd.GetCommand("OSB-08-DTE"));
            if (_cfg.HTS.EnabledThreats[9])
                AppendCommand(mfd.GetCommand("OSB-09-TEST"));
            if (_cfg.HTS.EnabledThreats[10])
                AppendCommand(mfd.GetCommand("OSB-10-FLCS"));
            if (_cfg.HTS.EnabledThreats[11])                            // TC 11
                AppendCommand(mfd.GetCommand("OSB-01-BLANK"));

            AppendCommand(mfd.GetCommand("OSB-04-RCCE"));
        }
    }
}