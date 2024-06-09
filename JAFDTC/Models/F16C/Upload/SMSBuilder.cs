// ********************************************************************************************************************
//
// SMSBuilder.cs -- f-16c sms command builder
//
// Copyright(C) 2024 ilominar/raven
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
using JAFDTC.Models.F16C.SMS;
using JAFDTC.Utilities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static JAFDTC.Models.F16C.SMS.SMSSystem;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// builder to generate the command stream to configure the sms munitions via the sms format on the mfd. the
    /// command stream is built assuming the sms format is selected on one of the viper's mfds. the builder
    /// requires the following state:
    ///
    ///     SMSMuni.{mode}.mfdSide: string
    ///         identifies mfd that displays the hts page in "mode", legal values are "left" or "right"
    ///     SMSMuni.{mode}.osbSel: string
    ///         mfd button name ("OSB-nn") of the currently selected format on the mfd identified by mfdSide in "mode"
    ///     SMSMuni.{mode}.osbSMS: string
    ///         mfd button name ("OSB-nn") that selects the sms format on the mfd identified by mfdSide in "mode"
    ///     SMSMuni.{mode}: List of string
    ///         sms quantity + name strings for the munitions on the jet in "mode"
    ///         
    /// where "mode" is a MFDSystem.MasterModes.
    ///
    /// see SMSStateQueryBuilder for further details.
    /// </summary>
    internal class SMSBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly List<F16CMunition> _munitionDb;

        private readonly static Dictionary<string, string> _mapEmplToOSB = new()
        {
            [((int)MunitionSettings.EmploymentModes.CCIP).ToString()] = "OSB-20",
            [((int)MunitionSettings.EmploymentModes.CCRP).ToString()] = "OSB-19",
            [((int)MunitionSettings.EmploymentModes.DTOS).ToString()] = "OSB-18",
            [((int)MunitionSettings.EmploymentModes.LADD).ToString()] = "OSB-17",
            [((int)MunitionSettings.EmploymentModes.MAN).ToString()] = "OSB-16"
        };
        private readonly static Dictionary<string, string> _mapEmplToLabel = new()
        {
            [((int)MunitionSettings.EmploymentModes.VIS).ToString()] = "VIS",
            [((int)MunitionSettings.EmploymentModes.PRE).ToString()] = "PRE",
            [((int)MunitionSettings.EmploymentModes.BORE).ToString()] = "BORE"
        };
        private readonly static Dictionary<string, string> _mapFuzeToLabel = new()
        {
            [((int)MunitionSettings.FuzeModes.NSTL).ToString()] = "NSTL",
            [((int)MunitionSettings.FuzeModes.NOSE).ToString()] = "NOSE",
            [((int)MunitionSettings.FuzeModes.TAIL).ToString()] = "TAIL",
            [((int)MunitionSettings.FuzeModes.NSTL_HI).ToString()] = "NSTL",
            [((int)MunitionSettings.FuzeModes.NOSE_LO).ToString()] = "NOSE",
            [((int)MunitionSettings.FuzeModes.TAIL_HI).ToString()] = "TAIL"
        };
        private readonly static Dictionary<char, string> _mapDigitToOSB = new()
        {
            ['1'] = "OSB-20", ['2'] = "OSB-19", ['3'] = "OSB-18", ['4'] = "OSB-17", ['5'] = "OSB-16",
            ['6'] = "OSB-06", ['7'] = "OSB-07", ['8'] = "OSB-08", ['9'] = "OSB-09", ['0'] = "OSB-10",
        };
        private readonly static Dictionary<string, string> _mapRelToLabel = new()
        {
            [((int)MunitionSettings.ReleaseModes.SGL).ToString()] = "SGL",
            [((int)MunitionSettings.ReleaseModes.PAIR).ToString()] = "PAIR",
            [((int)MunitionSettings.ReleaseModes.TRI_SGL).ToString()] = "SGL",
            [((int)MunitionSettings.ReleaseModes.TRI_PAIR_F2B).ToString()] = "PAIR_F2B",
            [((int)MunitionSettings.ReleaseModes.TRI_PAIR_L2R).ToString()] = "PAIR_L2R",
            [((int)MunitionSettings.ReleaseModes.MAV_SGL).ToString()] = "SGL",
            [((int)MunitionSettings.ReleaseModes.MAV_PAIR).ToString()] = "PAIR",
            [((int)MunitionSettings.ReleaseModes.GBU24_SGL).ToString()] = "SGL",
            [((int)MunitionSettings.ReleaseModes.GBU24_RP1).ToString()] = "PAIR",
            [((int)MunitionSettings.ReleaseModes.GBU24_RP2).ToString()] = "PAIR",
            [((int)MunitionSettings.ReleaseModes.GBU24_RP3).ToString()] = "PAIR",
            [((int)MunitionSettings.ReleaseModes.GBU24_RP4).ToString()] = "PAIR"
        };
        private readonly static Dictionary<string, string> _mapAutoPwrToLabel = new()
        {
            [((int)MunitionSettings.AutoPowerModes.OFF).ToString()] = "OFF",
            [((int)MunitionSettings.AutoPowerModes.NORTH_OF).ToString()] = "ON",
            [((int)MunitionSettings.AutoPowerModes.SOUTH_OF).ToString()] = "ON",
            [((int)MunitionSettings.AutoPowerModes.EAST_OF).ToString()] = "ON",
            [((int)MunitionSettings.AutoPowerModes.WEST_OF).ToString()] = "ON"
        };
        private readonly static Dictionary<string, string> _mapAutoPwrModeToLabel = new()
        {
            [((int)MunitionSettings.AutoPowerModes.OFF).ToString()] = "OFF",
            [((int)MunitionSettings.AutoPowerModes.NORTH_OF).ToString()] = "NORTH OF",
            [((int)MunitionSettings.AutoPowerModes.SOUTH_OF).ToString()] = "SOUTH OF",
            [((int)MunitionSettings.AutoPowerModes.EAST_OF).ToString()] = "EAST OF",
            [((int)MunitionSettings.AutoPowerModes.WEST_OF).ToString()] = "WEST OF"
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SMSBuilder(F16CConfiguration cfg, F16CDeviceManager dm, StringBuilder sb)
            : base(cfg, dm, sb)
        {
            _munitionDb = FileManager.LoadF16CMunitions();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure sms system via the icp/ded according to the non-default programming settings (this function is
        /// safe to call with a configuration with default settings: defaults are skipped as necessary). the builder
        /// assumes the sms page is currently selected and requires the following state:
        /// 
        ///     SMSMuni.{mode}.mfdSide: string
        ///         identifies mfd that displays the hts page in "mode", legal values are "left" or "right"
        ///     SMSMuni.{mode}.osbSel: string
        ///         mfd button name ("OSB-nn") of the currently selected format on the mfd identified by mfdSide in "mode"
        ///     SMSMuni.{mode}.osbSMS: string
        ///         mfd button name ("OSB-nn") that selects the sms format on the mfd identified by mfdSide in "mode"
        ///     SMSMuni.{mode}: List of string
        ///         sms quantity + name strings for the munitions on the jet in "mode"
        ///
        /// where "mode" is a MFDSystem.MasterModes.
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.SMS.IsDefault)
                return;

            // TODO: handle a2a or a2g here by allowing muniQtys state for a2a, a2g weapons? assume only a2g for now.
            MFDSystem.MasterModes mode = MFDSystem.MasterModes.ICP_AG;

            state.TryGetValueAs($"SMSMuni.{mode}.mfdSide", out string mfdSide);
            state.TryGetValueAs($"SMSMuni.{mode}.osbSel", out string osbSel);
            state.TryGetValueAs($"SMSMuni.{mode}.osbSMS", out string osbSMS);
            state.TryGetValueAs($"SMSMuni.{mode}", out List<string> muniQtys);

            AirframeDevice ufc = _aircraft.GetDevice("UFC");
            AirframeDevice mfd = (mfdSide == "left") ? _aircraft.GetDevice("LMFD") : _aircraft.GetDevice("RMFD");

            AddExecFunction("NOP", new() { "==== SMSBuilder:Build() begins" });

            foreach (string muniQty in muniQtys)
            {
                F16CMunition muniInfo = FindMunitionInfoForSMS(muniQty[1..]);
                if (muniInfo != null)
                {
                    List<MunitionSettings> profiles = FindProfilesForMunition(muniInfo.ID);
                    bool isDefault = true;
                    foreach (MunitionSettings profile in profiles)
                    {
                        if (!profile.IsDefault)
                        {
                            isDefault = false;
                            break;
                        }
                    }
                    if (isDefault)
                        continue;

                    AddWhileBlock("IsSMSMuniSelected", false, new() { mfdSide, muniQty }, delegate ()
                    {
                        AddAction(mfd, "OSB-06", WAIT_BASE);
                    }, 8);
                    AddIfBlock("IsSMSMuniSelected", true, new() { mfdSide, muniQty }, delegate ()
                    {
                        string selProfile = "0";
                        foreach (MunitionSettings settings in profiles)
                        {
                            BuildMunition(mfd, mfdSide, muniInfo, settings);
                            if (settings.IsProfileSelected == "True")
                                selProfile = settings.Profile;
                        }
                        SetProfile(mfd, mfdSide, muniInfo.ID, selProfile);
                    });
                }

                AddExecFunction("NOP", new() { "==== SMSBuilder:Build() ends" });
            }
        }

        /// <summary>
        /// build the command stream for the munition setup. for munitions that have profiles, the method will
        /// select the correct profile. BuildMunitionConfigCore() build the munition-specific command stream.
        /// </summary>
        private void BuildMunition(AirframeDevice mfd, string mfdSide, F16CMunition muniInfo, MunitionSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.Profile))
            {
                string profNum = $"{int.Parse(settings.Profile) + 1}";
                string profLabel = $"PROF{profNum}";
                switch (muniInfo.MunitionInfo.ID)
                {
                    case Munitions.CBU_87:
                    case Munitions.CBU_97:
                    case Munitions.GBU_10:
                    case Munitions.GBU_12:
                    case Munitions.MK_82_LD:
                    case Munitions.MK_82_HDSE:
                    case Munitions.MK_82_HDAB:
                    case Munitions.MK_84_LD:
                    case Munitions.MK_84_HD:
                        SetProfile(mfd, mfdSide, muniInfo.MunitionInfo.ID, settings.Profile);
                        AddIfBlock("IsSMSProfile", true, new() { mfdSide, profLabel }, delegate ()
                        {
                            BuildMunitionConfigCore(mfd, mfdSide, muniInfo, settings);
                        });
                        break;
                    case Munitions.GBU_24:
                        SetProfile(mfd, mfdSide, muniInfo.MunitionInfo.ID, settings.Profile);
                        AddIfBlock("IsSMSProfileGBU24", true, new() { mfdSide, profNum }, delegate ()
                        {
                            BuildMunitionConfigCore(mfd, mfdSide, muniInfo, settings);
                        });
                        break;
                    default:
                        BuildMunitionConfigCore(mfd, mfdSide, muniInfo, settings);
                        break;
                }
            }
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        private void BuildMunitionConfigCore(AirframeDevice mfd, string mfdSide, F16CMunition muniInfo,
                                             MunitionSettings settings)
        {
            // perform settings updates to base sms page for munition.
            //
            switch (muniInfo.MunitionInfo.ID)
            {
                case Munitions.CBU_87:
                case Munitions.CBU_97:
                    SetMenuEmployment(mfd, settings.EmplMode);
                    SetRelease(mfd, mfdSide, "IsSMSReleaseType", settings);
                    AdvanceToLabel(mfd, mfdSide, "IsSMSFuze", "OSB-18", settings.FuzeMode, _mapFuzeToLabel);
                    break;
                case Munitions.CBU_103:
                case Munitions.CBU_105:
                    AdvanceToLabel(mfd, mfdSide, "IsSMSEmploymentWCMD", "OSB-02", settings.EmplMode, _mapEmplToLabel);
                    AdvanceToLabel(mfd, mfdSide, "IsSMSReleaseTypeWCMD", "OSB-19", settings.ReleaseMode, _mapRelToLabel);
                    EnterNumericParams(mfd, mfdSide, "OSB-18", new() { settings.RippleSpacing });
                    break;
                case Munitions.GBU_10:
                case Munitions.GBU_12:
                    SetMenuEmployment(mfd, settings.EmplMode);
                    SetRelease(mfd, mfdSide, "IsSMSReleaseType", settings);
                    AdvanceToLabel(mfd, mfdSide, "IsSMSFuze", "OSB-18", settings.FuzeMode, _mapFuzeToLabel);
                    break;
                case Munitions.GBU_24:
                    AdvanceToLabel(mfd, mfdSide, "IsSMSEmploymentGBU24", "OSB-02", settings.EmplMode, _mapEmplToLabel);
                    SetReleaseGBU24(mfd, mfdSide, settings);
                    AdvanceToLabel(mfd, mfdSide, "IsSMSFuzeGBU24", "OSB-18", settings.FuzeMode, _mapFuzeToLabel);
                    AdvanceToLabel(mfd, mfdSide, "IsSMSArmDelayGBU24", "OSB-17", $"AD {settings.ArmDelayMode}SEC");
                    break;
                case Munitions.GBU_31:
                case Munitions.GBU_31P:
                case Munitions.GBU_38:
                    AdvanceToLabel(mfd, mfdSide, "IsSMSEmploymentJDAM", "OSB-02", settings.EmplMode, _mapEmplToLabel);
                    break;
                case Munitions.MK_82_LD:
                case Munitions.MK_84_LD:
                    SetMenuEmployment(mfd, settings.EmplMode);
                    SetRelease(mfd, mfdSide, "IsSMSReleaseType", settings);
                    AdvanceToLabel(mfd, mfdSide, "IsSMSFuze", "OSB-18", settings.FuzeMode, _mapFuzeToLabel);
                    break;
                case Munitions.MK_82_HDSE:
                case Munitions.MK_82_HDAB:
                case Munitions.MK_84_HD:
                    SetMenuEmployment(mfd, settings.EmplMode);
                    SetRelease(mfd, mfdSide, "IsSMSReleaseType", settings);
                    AdvanceToLabel(mfd, mfdSide, "IsSMSFuzeHD", "OSB-18", settings.FuzeMode, _mapFuzeToLabel);
                    break;
                case Munitions.AGM_65D:
                case Munitions.AGM_65G:
                case Munitions.AGM_65H:
                case Munitions.AGM_65K:
                    AdvanceToLabel(mfd, mfdSide, "IsSMSEmploymentMAV", "OSB-02", settings.EmplMode, _mapEmplToLabel);
                    SetReleaseMAV(mfd, mfdSide, "OSB-08", settings.ReleaseMode);
                    break;
                default:
                    break;
            }

            // perform settings updates on cntl page for munition.
            //
            AddAction(mfd, "OSB-05", WAIT_BASE);
            switch (muniInfo.MunitionInfo.ID)
            {
                case Munitions.CBU_87:
                case Munitions.CBU_97:
                    EnterNumericParams(mfd, mfdSide, "OSB-10", new() { settings.ReleaseAng });
                    EnterNumericParams(mfd, mfdSide, "OSB-19", new() { settings.ArmDelay, settings.BurstAlt });
                    break;
                case Munitions.CBU_103:
                    EnterNumericParams(mfd, mfdSide, "OSB-18", new() { settings.BurstAlt });
                    if (!string.IsNullOrEmpty(settings.Spin))
                        AdvanceToLabel(mfd, mfdSide, "IsSMSSpinWCMD", "OSB-17", $"{settings.Spin}RPM");
                    break;
                case Munitions.CBU_105:
                    EnterNumericParams(mfd, mfdSide, "OSB-18", new() { settings.BurstAlt });
                    break;
                case Munitions.GBU_10:
                case Munitions.GBU_12:
                case Munitions.MK_82_LD:
                case Munitions.MK_84_LD:
                case Munitions.MK_82_HDSE:
                case Munitions.MK_82_HDAB:
                case Munitions.MK_84_HD:
                    EnterNumericParams(mfd, mfdSide, "OSB-10", new() { settings.ReleaseAng });
                    EnterNumericParams(mfd, mfdSide, "OSB-20", new() { settings.ArmDelay, settings.ArmDelay2 });
                    break;
                case Munitions.GBU_24:
                    EnterNumericParams(mfd, mfdSide, "OSB-09", new() { settings.CueRange });
                    EnterNumericParams(mfd, mfdSide, "OSB-10", new() { settings.ReleaseAng }, true);
                    break;
                case Munitions.GBU_31:
                case Munitions.GBU_31P:
                case Munitions.GBU_38:
                    EnterNumericParams(mfd, mfdSide, "OSB-06", new() { settings.ImpactAng });
                    EnterNumericParams(mfd, mfdSide, "OSB-07", new() { settings.ImpactAzi });
                    EnterNumericParams(mfd, mfdSide, "OSB-08", new() { settings.ImpactVel });
                    AdvanceToLabel(mfd, mfdSide, "IsSMSArmDelayJDAM", "OSB-19", $"AD {settings.ArmDelayMode}SEC");
                    break;
                case Munitions.AGM_65D:
                case Munitions.AGM_65G:
                case Munitions.AGM_65H:
                case Munitions.AGM_65K:
                    AdvanceToLabel(mfd, mfdSide, "IsSMSAutoPwrMAV", "OSB-07", settings.AutoPwrMode, _mapAutoPwrToLabel);
                    EnterNumericParams(mfd, mfdSide, "OSB-19", new() { settings.AutoPwrSP });
                    AdvanceToLabel(mfd, mfdSide, "IsSMSAutoPwrModeMAV", "OSB-20", settings.AutoPwrMode,
                                   _mapAutoPwrModeToLabel);
                    break;
                default:
                    break;
            }
            AddAction(mfd, "OSB-05", WAIT_BASE);

        }

        /// <summary>
        /// press an osb on the indicated mfd until the button label matches the target. mapToPit specifies an
        /// optional mapping from the target value to the avionics label. the test function takes the mfd side along
        /// with the target label and returns true when the avionics match. method does nothing if target is empty.
        /// </summary>
        private void AdvanceToLabel(AirframeDevice mfd, string mfdSide, string fnTest, string osbStep, string target,
                                    Dictionary<string, string> mapToPit = null)
        {
            if (!string.IsNullOrEmpty(target))
            {
                target = (mapToPit != null) ? mapToPit[target] : target;
                AddWhileBlock(fnTest, false, new() { mfdSide, target }, delegate ()
                {
                    AddAction(mfd, osbStep, WAIT_BASE);
                }, 16);
            }
        }

        /// <summary>
        /// enter a numeric parameter on the keypad page with numbers along the vertical edges of the mfds. the keypad
        /// is entered by pressing the start osb, followed by the values in the value list (with non-digit characters
        /// removed). each value ends with a press of ENTR (osb 2).
        /// </summary>
        private void EnterNumericParams(AirframeDevice mfd, string mfdSide, string osbStart, List<string> values,
                                        bool isNegOK = false)
        {
            bool hasNonNull = false;
            foreach (string value in values)
                if (!string.IsNullOrEmpty(value))
                {
                    hasNonNull = true;
                    break;
                }
            if (!hasNonNull)
                return;

            AddAction(mfd, osbStart, WAIT_BASE);

            foreach (string value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    List<string> buttons = new();
                    bool isPositive = true;
                    foreach (char c in AdjustNoSeparators(value).ToCharArray())
                    {
                        if (c == '-')
                            isPositive = false;
                        else
                            buttons.Add(_mapDigitToOSB[c]);
                    }
                    if (isNegOK)
                        AddWhileBlock("IsSMSCntlNumericPadNeg", isPositive, new() { mfdSide }, delegate ()
                        {
                            AddAction(mfd, "OSB-05");
                        });
                    AddActions(mfd, buttons);
                }
                AddAction(mfd, "OSB-02", WAIT_BASE);
            }
        }

        /// <summary>
        /// generate the command stream to set the profile to the given profile (as specified in the avionics).
        /// </summary>
        private void SetProfile(AirframeDevice mfd, string mfdSide, Munitions muni, string profile)
        {
            if (!string.IsNullOrEmpty(profile))
            switch (muni)
                {
                    case Munitions.CBU_87:
                    case Munitions.CBU_97:
                    case Munitions.GBU_10:
                    case Munitions.GBU_12:
                    case Munitions.MK_82_LD:
                    case Munitions.MK_82_HDSE:
                    case Munitions.MK_82_HDAB:
                    case Munitions.MK_84_LD:
                    case Munitions.MK_84_HD:
                        AdvanceToLabel(mfd, mfdSide, "IsSMSProfile", "OSB-07", $"PROF{int.Parse(profile) + 1}");
                        break;
                    case Munitions.GBU_24:
                        AdvanceToLabel(mfd, mfdSide, "IsSMSProfileGBU24", "OSB-07", $"{int.Parse(profile) + 1}");
                        break;
                    default:
                        break;
                }
        }

        /// <summary>
        /// generate the command stream to select the employment mode when osb-2 jumps to a menu page that selects the
        /// employment. function does nothing if the mode is null/emtpy. this applies to ccip, ccrp, dtoss, ladd,
        /// and man employments.
        /// </summary>
        private void SetMenuEmployment(AirframeDevice mfd, string emplMode)
        {
            if (!string.IsNullOrEmpty(emplMode))
                AddActions(mfd, new() { "OSB-02", _mapEmplToOSB[emplMode] }, null, WAIT_BASE);
        }

        /// <summary>
        /// generate the command stream to set up the base release mode and associated settings. this includes
        /// single/pair (osb 8), ripple pulse count (osb 10), and munition spacing (osb 9).
        /// </summary>
        private void SetRelease(AirframeDevice mfd, string mfdSide, string fnModeCheck, MunitionSettings settings)
        {
            bool isEmplMAN = (!string.IsNullOrEmpty(settings.EmplMode) &&
                              (int.Parse(settings.EmplMode) != (int)MunitionSettings.EmploymentModes.MAN));

            if (!string.IsNullOrEmpty(settings.ReleaseMode))
                AdvanceToLabel(mfd, mfdSide, fnModeCheck, "OSB-08", _mapRelToLabel[settings.ReleaseMode]);
            if (!string.IsNullOrEmpty(settings.RippleSpacing) && isEmplMAN)
                EnterNumericParams(mfd, mfdSide, "OSB-09", new() { settings.RippleSpacing });
            if (!string.IsNullOrEmpty(settings.RipplePulse))
                EnterNumericParams(mfd, mfdSide, "OSB-10", new() { settings.RipplePulse });
        }

        /// <summary>
        /// generate the command stream to set up the release mode for the gbu-24. this includes single/pair (osb 8),
        /// ripple pulse (osb 10), and ripple delay (osb 9).
        /// </summary>
        private void SetReleaseGBU24(AirframeDevice mfd, string mfdSide, MunitionSettings settings)
        {
            // only care about correctly identifying GBU24_RPx modes. none of these are default, so they should be
            // explicitly set in settings.ReleaseMode (i.e., settings.Release mode can't be nil for these modes).
            //
            MunitionSettings.ReleaseModes relMode = MunitionSettings.ReleaseModes.Unknown;
            if (!string.IsNullOrEmpty(settings.ReleaseMode))
                relMode = (MunitionSettings.ReleaseModes)int.Parse(settings.ReleaseMode);

            if (!string.IsNullOrEmpty(settings.ReleaseMode))
                AdvanceToLabel(mfd, mfdSide, "IsSMSReleaseTypeGBU24", "OSB-08", _mapRelToLabel[settings.ReleaseMode]);
            if (!string.IsNullOrEmpty(settings.RippleDelayMode))
            {
                string target = relMode switch
                {
                    MunitionSettings.ReleaseModes.GBU24_RP1 => "1",
                    MunitionSettings.ReleaseModes.GBU24_RP2 => "2",
                    MunitionSettings.ReleaseModes.GBU24_RP3 => "3",
                    MunitionSettings.ReleaseModes.GBU24_RP4 => "4",
                    _ => null
                };
                if (target != null)
                    AdvanceToLabel(mfd, mfdSide, "IsSMSRipplePulseGBU24", "OSB-10", target);
            }
            if (!string.IsNullOrEmpty(settings.RippleDelayMode) && (relMode != MunitionSettings.ReleaseModes.GBU24_RP1))
                AdvanceToLabel(mfd, mfdSide, "IsSMSRippleDelayGBU24", "OSB-09", $"{settings.RippleDelayMode}MSEC");
        }

        /// <summary>
        /// generate the command stream to set up the release mode for mavericks. this includes the ripple pulse
        /// (osb 8).
        /// </summary>
        private void SetReleaseMAV(AirframeDevice mfd, string mfdSide, string osb, string release)
        {
            if (!string.IsNullOrEmpty(release))
            {
                string relQty = (release == ((int)MunitionSettings.ReleaseModes.MAV_PAIR).ToString()) ? "2" : "1";
                EnterNumericParams(mfd, mfdSide, "OSB-08", new() { relQty });
            }
        }

        /// <summary>
        /// return the munition information from the munition database that matches the given sms label sms (label
        /// does not include quantity).
        /// </summary>
        private F16CMunition FindMunitionInfoForSMS(string labelSMS)
        {
            foreach (F16CMunition muni in _munitionDb)
                foreach (string muniDbLabelSMS in muni.LabelSMS)
                    if (muniDbLabelSMS == labelSMS)
                        return muni;
            return null;
        }

        /// <summary>
        /// return a list of the non-default profiles associated with the given munition from the configuration.
        /// </summary>
        private List<MunitionSettings> FindProfilesForMunition(Munitions muni)
        {
            List<MunitionSettings> profiles = new();
            foreach (KeyValuePair<string, MunitionSettings> kvp in _cfg.SMS.GetProfilesForMunition(muni))
                if (!kvp.Value.IsDefault)
                    profiles.Add(kvp.Value);
            return profiles;
        }
    }
}