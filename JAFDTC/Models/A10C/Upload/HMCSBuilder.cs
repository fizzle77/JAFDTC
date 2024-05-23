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

using JAFDTC.Models.A10C.DSMS;
using JAFDTC.Models.A10C.HMCS;
using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.A10C.Upload
{
    internal class HMCSBuilder : A10CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HMCSBuilder(A10CConfiguration cfg, A10CDeviceManager dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb)
        {
        }

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

            // Common HMCS settings
            if (!_cfg.HMCS.IsTGPTrackDefault)
                AddActionsForSettingProperty(rmfd, "RMFD_08", "TGPTrack");

            // TODO query for brightness state
            //
            //if (!_cfg.HMCS.IsBrightnessSettingDefault)
            //    Click(rmfd, "RMFD_08", "TGPTrack");

            // Profiles...
            BuildProfile(cdu, rmfd, Profiles.PRO1);
            BuildProfile(cdu, rmfd, Profiles.PRO2);
            BuildProfile(cdu, rmfd, Profiles.PRO3);

            // Active profile
            SetProfileActive(rmfd, (Profiles)_cfg.HMCS.ActiveProfileValue);
        }

        private void BuildProfile(AirframeDevice cdu, AirframeDevice rmfd, Profiles profile)
        {
            HMCSProfileSettings profileCfg = _cfg.HMCS.GetProfileSettings(profile);
            if (profileCfg.IsDefault)
                return;

            SetProfileActive(rmfd, profile);

            // Do each setting, top to bottom.
            AddActionsForSettingProperty(rmfd, "RMFD_18", "Crosshair", profile);
            AddAction(rmfd, "RMFD_19"); // Move down

            AddActionsForSettingProperty(rmfd, "RMFD_18", "OwnSPI", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "SPIIndicator", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "HorizonLine", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "HDC", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "Hookship", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "TGPDiamond", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "TGPFOV", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "FlightMembers", profile);
            AddActionsForRangeProperty(cdu, rmfd, "FlightMembersRange", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "FMSPI", profile);
            AddActionsForRangeProperty(cdu, rmfd, "FMSPIRange", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "DonorAirPPLI", profile);
            AddActionsForRangeProperty(cdu, rmfd, "DonorAirPPLIRange", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "DonorSPI", profile);
            AddActionsForRangeProperty(cdu, rmfd, "DonorSPIRange", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "CurrentMA", profile);
            AddActionsForRangeProperty(cdu, rmfd, "CurrentMARange", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "AirEnvir", profile);
            AddAction(rmfd, "RMFD_19");

            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR VMF FRIEND

            AddActionsForSettingProperty(rmfd, "RMFD_18", "AirPPLINonDonor", profile);
            AddActionsForRangeProperty(cdu, rmfd, "AirPPLINonDonorRange", profile);
            AddAction(rmfd, "RMFD_19");

            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR TRK FRIEND
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR NEUTRAL
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR SUSPECT
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR HOSTILE
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: AIR OTHER

            AddActionsForSettingProperty(rmfd, "RMFD_18", "GndEnvir", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "GndVMFFriend", profile);
            AddActionsForRangeProperty(cdu, rmfd, "GndVMFFriendRange", profile);
            AddAction(rmfd, "RMFD_19");

            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND PPLI
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND TRK FRIEND
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND NEUTRAL
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND SUSPECT
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND HOSTILE
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: GND OTHER
            AddAction(rmfd, "RMFD_19"); // Skipped because no function in DCS: EMER POINT

            AddActionsForSettingProperty(rmfd, "RMFD_18", "Steerpoint", profile);
            AddActionsForRangeProperty(cdu, rmfd, "SteerpointRange", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "MsnMarkpoints", profile);
            AddActionsForRangeProperty(cdu, rmfd, "MsnMarkpointsRange", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "MsnMarkLabels", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "Airspeed", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "RadarAltitude", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "BaroAltitude", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "ACHeading", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "HelmetHeading", profile);
            AddAction(rmfd, "RMFD_19");

            AddActionsForSettingProperty(rmfd, "RMFD_18", "HMDElevLines", profile);
            AddAction(rmfd, "RMFD_19");
        }

        private void SetProfileActive(AirframeDevice rmfd, Profiles profile, bool skipReEnter = false)
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
            if (propName == "SPIIndicator") // SPI Indicator is the only prop with just 2 values. All others are 3.
                clicks = Math.Abs(clicks);
            else
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
            foreach (char c in configuredVal)
                AddAction(cdu, c.ToString()); // type numeric range into scratchpad
            AddAction(rmfd, "RMFD_17"); // range button on HMCS profile page
        }

    }
}
