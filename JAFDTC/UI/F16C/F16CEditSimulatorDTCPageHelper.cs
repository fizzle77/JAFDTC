// ********************************************************************************************************************
//
// F16CEditSimulatorDTCPageHelper.cs : viper specialization for EditSimulatorDTCPage
//
// Copyright(C) 2025 ilominar/raven
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

using JAFDTC.Models;
using JAFDTC.Models.Base;
using JAFDTC.Models.F16C;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using System.Collections.Generic;
using System.Diagnostics;

namespace JAFDTC.UI.F16C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class F16CEditSimulatorDTCPageHelper : IEditSimulatorDTCPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(SimDTCSystem.SystemTag, "DCS DTC Tape", "DCS DTC", "\xE77C", typeof(EditSimulatorDTCPage),
                   typeof(F16CEditSimulatorDTCPageHelper));

        public SystemBase GetSystemConfig(IConfiguration config) => ((F16CConfiguration)config).DTE;

        public List<ConfigEditorPageInfo> MergableSystems => new()
        {
            F16CEditRadioPageHelper.PageInfo,
            F16CEditCMDSPage.PageInfo
        };

        /// <summary>
        /// validate the dtc configuration is correct. this checks to ensure the output path is valid and
        /// the template is known. the configuration is updated if necessary.
        /// </summary>
        public void ValidateDTCSystem(IConfiguration config)
        {
            ((F16CConfiguration)config).DTE.ValidateForAirframe(config.Airframe);
        }

        /// <summary>
        /// update the edit state for dtc from the configuration. the update will perform a deep copy of
        /// the data from the configuration.
        /// </summary>
        public void CopyConfigToEdit(IConfiguration config, SimDTCSystem editDTC)
        {
            editDTC.Template = new(((F16CConfiguration)config).DTE.Template);
            editDTC.OutputPath = new(((F16CConfiguration)config).DTE.OutputPath);
            editDTC.MergedSystemTags = new();
            foreach (string tag in ((F16CConfiguration)config).DTE.MergedSystemTags)
                editDTC.MergedSystemTags.Add(tag);
            editDTC.EnableLoad = new(((F16CConfiguration)config).DTE.EnableLoad);
        }

        /// <summary>
        /// update the configuration dtc from the edit state. the update will perform a deep copy of the
        /// data from the configuration.
        /// </summary>
        public void CopyEditToConfig(SimDTCSystem editDTC, IConfiguration config)
        {
            ((F16CConfiguration)config).DTE.Template = new(editDTC.Template);
            ((F16CConfiguration)config).DTE.OutputPath = new(editDTC.OutputPath);
            ((F16CConfiguration)config).DTE.MergedSystemTags = new();
            foreach (string tag in editDTC.MergedSystemTags)
                ((F16CConfiguration)config).DTE.MergedSystemTags.Add(tag);
            ((F16CConfiguration)config).DTE.EnableLoad = new(editDTC.EnableLoad);
        }
    }
}
