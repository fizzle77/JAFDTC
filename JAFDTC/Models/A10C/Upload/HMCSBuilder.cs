// ********************************************************************************************************************
//
// DSMSBuilder.cs -- a-10c hmcs system builder
//
// Copyright(C) 2024 fizzle, JAFDTC contributors
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

using JAFDTC.Models.A10C.HMCS;
using JAFDTC.Models.DCS;
using System;
using System.Reflection;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    internal class HMCSBuilder : A10CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HMCSBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override void Build()
        {
            if (_cfg.HMCS.IsDefault)
                return;

            AirframeDevice cdu = _aircraft.GetDevice("CDU");
            AirframeDevice rmfd = _aircraft.GetDevice("RMFD");

            AddIfBlock("IsSTATInDefaultMFDPosition", true, null, delegate () { BuildHMCS(cdu, rmfd); });
            // TODO handle non-default STAT position
        }

        private void BuildHMCS(AirframeDevice cdu, AirframeDevice rmfd)
        {
            AddActions(rmfd, new() { "RMFD_14", "RMFD_03" }); // Go to STAT, HMCS

            //
            // Common HMCS settings
            //

            // TGP Track
            if (!_cfg.HMCS.IsTGPTrackDefault)
                AddActionsForSettingProperty(rmfd, "RMFD_08", "TGPTrack");

            // Brightess
            if (!_cfg.HMCS.IsBrightnessSettingDefault)
            {
                if (_cfg.HMCS.BrightnessSettingValue == (int)BrightnessSettingOptions.NIGHT)
                    AddIfBlock("IsDayBrightnessSelected", true, null, delegate () { SetHMCSBrightness(rmfd, BrightnessSettingOptions.NIGHT); });
                else
                    AddIfBlock("IsDayBrightnessSelected", false, null, delegate () { SetHMCSBrightness(rmfd, BrightnessSettingOptions.DAY); });
            }

            //
            // Profiles
            //

            BuildProfile(cdu, rmfd, Profiles.PRO1);
            BuildProfile(cdu, rmfd, Profiles.PRO2);
            BuildProfile(cdu, rmfd, Profiles.PRO3);

            // Set the active profile. Has to be done after the profile setting upload.
            SetProfileActive(rmfd, (Profiles)_cfg.HMCS.ActiveProfileValue);
        }

        private void SetHMCSBrightness(AirframeDevice rmfd, BrightnessSettingOptions setting)
        {
            if (setting == BrightnessSettingOptions.NIGHT)
                AddAction(rmfd, "RMFD_10");
            else
                AddAction(rmfd, "RMFD_09");
        }

        private void BuildProfile(AirframeDevice cdu, AirframeDevice rmfd, Profiles profile)
        {
            HMCSProfileSettings profileCfg = _cfg.HMCS.GetProfileSettings(profile);
            if (profileCfg.IsDefault)
                return;

            string lastModifiedProp = GetLastModifiedProp(profile);

            SetProfileActive(rmfd, profile);

            // Do each setting, top to bottom.
            // It's important that this in the same order as the jet's list of settings.

            AddActionsForSettingProperty(rmfd, "RMFD_18", "Crosshair", profile);
            if (lastModifiedProp == "Crosshair") return;
            AddAction(rmfd, "RMFD_19"); // Move down

            AddActionsForSettingProperty(rmfd, "RMFD_18", "OwnSPI", profile);
            if (lastModifiedProp == "OwnSPI") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "SPIIndicator", profile);
            if (lastModifiedProp == "SPIIndicator") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "HorizonLine", profile);
            if (lastModifiedProp == "HorizonLine") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "HDC", profile);
            if (lastModifiedProp == "HDC") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "Hookship", profile);
            if (lastModifiedProp == "Hookship") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "TGPDiamond", profile);
            if (lastModifiedProp == "TGPDiamond") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "TGPFOV", profile);
            if (lastModifiedProp == "TGPFOV") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "FlightMembers", profile);
            if (lastModifiedProp == "FlightMembers") return;
            AddActionsForRangeProperty(cdu, rmfd, "FlightMembersRange", profile);
            if (lastModifiedProp == "FlightMembersRange") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "FMSPI", profile);
            if (lastModifiedProp == "FMSPI") return;
            AddActionsForRangeProperty(cdu, rmfd, "FMSPIRange", profile);
            if (lastModifiedProp == "FMSPIRange") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "DonorAirPPLI", profile);
            if (lastModifiedProp == "DonorAirPPLI") return;
            AddActionsForRangeProperty(cdu, rmfd, "DonorAirPPLIRange", profile);
            if (lastModifiedProp == "DonorAirPPLIRange") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "DonorSPI", profile);
            if (lastModifiedProp == "DonorSPI") return;
            AddActionsForRangeProperty(cdu, rmfd, "DonorSPIRange", profile);
            if (lastModifiedProp == "DonorSPIRange") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "CurrentMA", profile);
            if (lastModifiedProp == "CurrentMA") return;
            AddActionsForRangeProperty(cdu, rmfd, "CurrentMARange", profile);
            if (lastModifiedProp == "CurrentMARange") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "AirEnvir", profile);
            if (lastModifiedProp == "AirEnvir") return;
            AddAction(rmfd, "RMFD_19");

            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR VMF FRIEND

            AddActionsForSettingProperty(rmfd, "RMFD_18", "AirPPLINonDonor", profile);
            if (lastModifiedProp == "AirPPLINonDonor") return;
            AddActionsForRangeProperty(cdu, rmfd, "AirPPLINonDonorRange", profile);
            if (lastModifiedProp == "AirPPLINonDonorRange") return;
            AddAction(rmfd, "RMFD_19");

            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR TRK FRIEND
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR NEUTRAL
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR SUSPECT
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR HOSTILE
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR OTHER

            AddActionsForSettingProperty(rmfd, "RMFD_18", "GndEnvir", profile);
            if (lastModifiedProp == "GndEnvir") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "GndVMFFriend", profile);
            if (lastModifiedProp == "GndVMFFriend") return;
            AddActionsForRangeProperty(cdu, rmfd, "GndVMFFriendRange", profile);
            if (lastModifiedProp == "GndVMFFriendRange") return;
            AddAction(rmfd, "RMFD_19");

            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND PPLI
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND TRK FRIEND
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND NEUTRAL
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND SUSPECT
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND HOSTILE
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND OTHER
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: EMER POINT

            AddActionsForSettingProperty(rmfd, "RMFD_18", "Steerpoint", profile);
            if (lastModifiedProp == "Steerpoint") return;
            AddActionsForRangeProperty(cdu, rmfd, "SteerpointRange", profile);
            if (lastModifiedProp == "SteerpointRange") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "MsnMarkpoints", profile);
            if (lastModifiedProp == "MsnMarkpoints") return;
            AddActionsForRangeProperty(cdu, rmfd, "MsnMarkpointsRange", profile);
            if (lastModifiedProp == "MsnMarkpointsRange") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "MsnMarkLabels", profile);
            if (lastModifiedProp == "MsnMarkLabels") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "Airspeed", profile);
            if (lastModifiedProp == "Airspeed") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "RadarAltitude", profile);
            if (lastModifiedProp == "RadarAltitude") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "BaroAltitude", profile);
            if (lastModifiedProp == "BaroAltitude") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "ACHeading", profile);
            if (lastModifiedProp == "ACHeading") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "HelmetHeading", profile);
            if (lastModifiedProp == "HelmetHeading") return;
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "HMDElevLines", profile);
            if (lastModifiedProp == "HMDElevLines") return;
            AddAction(rmfd, "RMFD_19");
        }

        private void SetProfileActive(AirframeDevice rmfd, Profiles profile)
        {
            AddActions(rmfd, new() { "RMFD_14", "RMFD_03" }); // Go to STAT, HMCS: ensure we're at the top of the list

            string button = profile switch
            {
                Profiles.PRO1 => "RMFD_03",
                Profiles.PRO2 => "RMFD_04",
                Profiles.PRO3 => "RMFD_05",
                _ => throw new ApplicationException("Unexpected ActiveProfileValue: " + _cfg.HMCS.ActiveProfileValue)
            };
            AddAction(rmfd, button);
        }

        /// <summary>
        /// Add the correct number of click actions on the supplied button to 
        /// get to the configured value for the supplied property.
        /// </summary>
        private void AddActionsForSettingProperty(AirframeDevice rmfd, string button, string propName, Profiles? profile = null)
        {
            int numClicks = GetNumClicksForProperty(propName, profile);
            for (int i = 0; i < numClicks; i++)
                AddAction(rmfd, button, WAIT_BASE);
        }

        /// <summary>
        /// Lookup the default and configured values for the supplied property
        /// and return the number of clicks to get to the configured value.
        /// 
        /// This accounts for default-ness. If the configured value is the default, we'll get zero clicks.
        /// </summary>
        private int GetNumClicksForProperty(string propName, Profiles? profile = null)
        {
            PropertyInfo property;
            int defaultVal;
            int configuredVal;

            if (profile == null)
            {
                property = typeof(HMCSSystem).GetProperty(propName);
                if (property == null)
                    throw new ApplicationException("property not found:" + propName);

                defaultVal = int.Parse((string)property.GetValue(HMCSSystem.ExplicitDefaults));
                configuredVal = int.Parse((string)property.GetValue(_cfg.HMCS));
            }
            else
            {
                property = typeof(HMCSProfileSettings).GetProperty(propName);
                if (property == null)
                    throw new ApplicationException("property not found: " + propName);

                defaultVal = int.Parse((string)property.GetValue(HMCSProfileSettings.GetExplicitDefaults((Profiles)profile)));
                configuredVal = int.Parse((string)property.GetValue(_cfg.HMCS.GetProfileSettings((Profiles)profile)));
            }

            int clicks = configuredVal - defaultVal;
            if (propName == "SPIIndicator") // SPI Indicator is the only prop with just 2 values: it's always 0 or 1 clicks.
                clicks = Math.Abs(clicks);
            else // All other props have 3 possible values
            {
                if (clicks == -1)
                    clicks = 2;
                else if (clicks == -2)
                    clicks = 1;
            }
            return clicks;
        }

        /// <summary>
        /// Add actions to set the range value of the supplied property.
        /// This accounts for default-ness and will not make a change for a default value.
        /// </summary>
        private void AddActionsForRangeProperty(AirframeDevice cdu, AirframeDevice rmfd, string propName, Profiles profile)
        {
            PropertyInfo property;
            string defaultVal;
            string configuredVal;

            property = typeof(HMCSProfileSettings).GetProperty(propName);
            if (property == null)
                throw new ApplicationException("property not found:" + propName);

            defaultVal = (string)property.GetValue(HMCSProfileSettings.GetExplicitDefaults(profile));
            configuredVal = (string)property.GetValue(_cfg.HMCS.GetProfileSettings(profile));

            if (defaultVal == configuredVal)
                return;

            AddAction(cdu, "CLR");
            AddActions(cdu, ActionsForCleanNum(configuredVal)); // type numeric range into scratchpad
            AddAction(rmfd, "RMFD_17"); // range button on HMCS profile page
        }

        /// <summary>
        /// Returns the last non-default property name in the given profile.
        /// </summary>
        private string GetLastModifiedProp(Profiles profile)
        {
            // This is a lame hack but it works for now. This is to prevent us from
            // always arrowing down to the very bottom of the list for any modified
            // profile, even if only one setting at the top is changed.

            string lastPropName = null;
            HMCSProfileSettings profileCfg = _cfg.HMCS.GetProfileSettings(profile);

            // Check each setting, top to bottom.
            // It's important that this in the same order as the jet's list of settings.

            if (GetNumClicksForProperty("Crosshair", profile) > 0)
                lastPropName = "Crosshair";

            if (GetNumClicksForProperty("OwnSPI", profile) > 0)
                lastPropName = "OwnSPI";

            if (GetNumClicksForProperty("SPIIndicator", profile) > 0)
                lastPropName = "SPIIndicator";

            if (GetNumClicksForProperty("HorizonLine", profile) > 0)
                lastPropName = "HorizonLine";

            if (GetNumClicksForProperty("HDC", profile) > 0)
                lastPropName = "HDC";

            if (GetNumClicksForProperty("Hookship", profile) > 0)
                lastPropName = "Hookship";

            if (GetNumClicksForProperty("TGPDiamond", profile) > 0)
                lastPropName = "TGPDiamond";

            if (GetNumClicksForProperty("TGPFOV", profile) > 0)
                lastPropName = "TGPFOV";

            if (GetNumClicksForProperty("FlightMembers", profile) > 0)
                lastPropName = "FlightMembers";

            if (!profileCfg.IsFlightMembersRangeDefault)
                lastPropName = "FlightMembersRange";

            if (GetNumClicksForProperty("FMSPI", profile) > 0)
                lastPropName = "FMSPI";

            if (!profileCfg.IsFlightMemberSPIRangeDefault)
                lastPropName = "FMSPIRange";

            if (GetNumClicksForProperty("DonorAirPPLI", profile) > 0)
                lastPropName = "DonorAirPPLI";

            if (!profileCfg.IsDonorAirPPLIRangeDefault)
                lastPropName = "DonorAirPPLIRange";

            if (GetNumClicksForProperty("DonorSPI", profile) > 0)
                lastPropName = "DonorSPI";

            if (!profileCfg.IsDonorSPIRangeDefault)
                lastPropName = "DonorSPIRange";

            if (GetNumClicksForProperty("CurrentMA", profile) > 0)
                lastPropName = "CurrentMA";

            if (!profileCfg.IsCurrentMARangeDefault)
                lastPropName = "CurrentMARange";

            if (GetNumClicksForProperty("AirEnvir", profile) > 0)
                lastPropName = "AirEnvir";

            if (GetNumClicksForProperty("AirPPLINonDonor", profile) > 0)
                lastPropName = "AirPPLINonDonor";

            if (!profileCfg.IsAirPPLINonDonorRangeDefault)
                lastPropName = "AirPPLINonDonorRange";

            if (GetNumClicksForProperty("GndEnvir", profile) > 0)
                lastPropName = "GndEnvir";

            if (GetNumClicksForProperty("GndVMFFriend", profile) > 0)
                lastPropName = "GndVMFFriend";

            if (!profileCfg.IsGndVMFFriendRangeDefault)
                lastPropName = "GndVMFFriendRange";

            if (GetNumClicksForProperty("Steerpoint", profile) > 0)
                lastPropName = "Steerpoint";

            if (!profileCfg.IsSteerPointRangeDefault)
                lastPropName = "SteerpointRange";

            if (GetNumClicksForProperty("MsnMarkpoints", profile) > 0)
                lastPropName = "MsnMarkpoints";

            if (!profileCfg.IsMsnMarkpointsRangeDefault)
                lastPropName = "MsnMarkpointsRange";

            if (GetNumClicksForProperty("MsnMarkLabels", profile) > 0)
                lastPropName = "MsnMarkLabels";

            if (GetNumClicksForProperty("Airspeed", profile) > 0)
                lastPropName = "Airspeed";

            if (GetNumClicksForProperty("RadarAltitude", profile) > 0)
                lastPropName = "RadarAltitude";

            if (GetNumClicksForProperty("BaroAltitude", profile) > 0)
                lastPropName = "BaroAltitude";

            if (GetNumClicksForProperty("ACHeading", profile) > 0)
                lastPropName = "ACHeading";

            if (GetNumClicksForProperty("HelmetHeading", profile) > 0)
                lastPropName = "HelmetHeading";

            if (GetNumClicksForProperty("HMDElevLines", profile) > 0)
                lastPropName = "HMDElevLines";

            return lastPropName;
        }
    }
}
