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
using System.Collections.Generic;
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

        public override void Build(Dictionary<string, object> state = null)
        {
            if (_cfg.HMCS.IsDefault)
                return;

            AddExecFunction("NOP", new() { "==== HMCSBuilder:Build()" });

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

            SetProfileActive(rmfd, profile);

            // Do each setting, top to bottom.
            // This must be in the same order as the jet's list of settings.
            const string BUTTON = "RMFD_18_SHORT";

            AddActionsForSettingProperty(rmfd, BUTTON, "Crosshair", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "OwnSPI", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "SPIIndicator", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "HorizonLine", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "HDC", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "Hookship", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "TGPDiamond", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "TGPFOV", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "FlightMembers", profile);
            AddActionsForRangeProperty(cdu, rmfd, "FlightMembersRange", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "FMSPI", profile);
            AddActionsForRangeProperty(cdu, rmfd, "FMSPIRange", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "DonorAirPPLI", profile);
            AddActionsForRangeProperty(cdu, rmfd, "DonorAirPPLIRange", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "DonorSPI", profile);
            AddActionsForRangeProperty(cdu, rmfd, "DonorSPIRange", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "CurrentMA", profile);
            AddActionsForRangeProperty(cdu, rmfd, "CurrentMARange", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "AirEnvir", profile);
            QueueContingentArrowDownAction();

            QueueContingentArrowDownAction(); // Skipped because no function in DCS: AIR VMF FRIEND

            AddActionsForSettingProperty(rmfd, BUTTON, "AirPPLINonDonor", profile);
            AddActionsForRangeProperty(cdu, rmfd, "AirPPLINonDonorRange", profile);
            QueueContingentArrowDownAction();

            QueueContingentArrowDownAction(); // Skipped because no function in DCS: AIR TRK FRIEND
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: AIR NEUTRAL
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: AIR SUSPECT
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: AIR HOSTILE
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: AIR OTHER

            AddActionsForSettingProperty(rmfd, BUTTON, "GndEnvir", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "GndVMFFriend", profile);
            AddActionsForRangeProperty(cdu, rmfd, "GndVMFFriendRange", profile);
            QueueContingentArrowDownAction();

            QueueContingentArrowDownAction(); // Skipped because no function in DCS: GND PPLI
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: GND TRK FRIEND
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: GND NEUTRAL
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: GND SUSPECT
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: GND HOSTILE
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: GND OTHER
            QueueContingentArrowDownAction(); // Skipped because no function in DCS: EMER POINT

            AddActionsForSettingProperty(rmfd, BUTTON, "Steerpoint", profile);
            AddActionsForRangeProperty(cdu, rmfd, "SteerpointRange", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "MsnMarkpoints", profile);
            AddActionsForRangeProperty(cdu, rmfd, "MsnMarkpointsRange", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "MsnMarkLabels", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "Airspeed", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "RadarAltitude", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "BaroAltitude", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "ACHeading", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "HelmetHeading", profile);
            QueueContingentArrowDownAction();

            AddActionsForSettingProperty(rmfd, BUTTON, "HMDElevLines", profile);
            QueueContingentArrowDownAction();
        }

        private void SetProfileActive(AirframeDevice rmfd, Profiles profile)
        {
            AddActions(rmfd, new() { "RMFD_14", "RMFD_03" }); // Go to STAT, HMCS: ensure we're at the top of the list

            // Moved to the top of a new profile. Ensure no queued arrow-downs.
            ClearAllContingentArrowDownActions();

            string button = profile switch
            {
                Profiles.PRO1 => "RMFD_03",
                Profiles.PRO2 => "RMFD_04",
                Profiles.PRO3 => "RMFD_05",
                _ => throw new ApplicationException("Unexpected ActiveProfileValue: " + _cfg.HMCS.ActiveProfileValue)
            };
            AddAction(rmfd, button);
        }

        /// 
        /// Contingent arrow down actions
        ///

        // We traverse the whole list of settings to determine which ones need to be changed, but more often than not
        // we don't need to actually go all the way to the bottom of the list in the cockpit. So we add "contingent"
        // arrow-down actions, simply a count. When we discover a setting that actually needs changing, we add an 
        // actual action for each queued arrow-down, moving down the list to change the setting in question. If we
        // don't discover a setting down the list that needs changing, those pointless clicks never get added.

        private void QueueContingentArrowDownAction()
        {
            _numContingentArrowDownActions++;
        }
        private void ClearAllContingentArrowDownActions()
        {
            _numContingentArrowDownActions = 0;
        }
        private void AddAllContingentArrowDownActions(AirframeDevice rmfd)
        {
            for (int i = 0; i < _numContingentArrowDownActions; i++)
                AddAction(rmfd, "RMFD_19_SHORT");
            ClearAllContingentArrowDownActions();
        }
        int _numContingentArrowDownActions = 0;

        /// <summary>
        /// Add the correct number of click actions on the supplied button to 
        /// get to the configured value for the supplied property.
        /// </summary>
        private void AddActionsForSettingProperty(AirframeDevice rmfd, string button, string propName, Profiles? profile = null)
        {
            int numClicks = GetNumClicksForProperty(propName, profile);
            
            // This setting is non-default. We need all the preceding arrow-down actions to get here and change it.
            if (numClicks > 0)
                AddAllContingentArrowDownActions(rmfd);

            // Add the actual setting changes.
            for (int i = 0; i < numClicks; i++)
                AddAction(rmfd, button);
        }

        /// <summary>
        /// Lookup the default and configured values for the supplied property
        /// and return the number of clicks to get to the configured value.
        /// 
        /// This accounts for default-ness: returns 0 if the configured value is the default.
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

            // SPI Indicator is the only prop with just 2 possible values. All other props have 3.
            // Hacky but for one special case, I prefer to just handle it one-off like this.
            int maxVal = propName == "SPIIndicator" ? 2 : 3;
            return GetNumClicksForWraparoundSetting(defaultVal, configuredVal, maxVal);
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

            // This setting is non-default. We need all the preceding arrow-down actions to get here and change it.
            AddAllContingentArrowDownActions(rmfd);

            // Add the actual setting changes.
            AddAction(cdu, "CLR");
            AddActions(cdu, ActionsForCleanNum(configuredVal)); // type numeric range into scratchpad
            AddAction(rmfd, "RMFD_17"); // range button on HMCS profile page
        }
    }
}
