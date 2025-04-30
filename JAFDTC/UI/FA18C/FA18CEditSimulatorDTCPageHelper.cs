// ********************************************************************************************************************
//
// FA18CEditSimulatorDTCPageHelper.cs : hornet specialization for EditSimulatorDTCPage
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
using JAFDTC.Models.FA18C;
using JAFDTC.UI.App;
using JAFDTC.UI.Base;
using System;
using System.Collections.Generic;

namespace JAFDTC.UI.FA18C
{
    /// <summary>
    /// TODO: document
    /// </summary>
    internal class FA18CEditSimulatorDTCPageHelper : IEditSimulatorDTCPageHelper
    {
        public static ConfigEditorPageInfo PageInfo
            => new(SimDTCSystem.SystemTag, "DCS DTC Tape", "DCS DTC", "\xE77C", typeof(EditSimulatorDTCPage),
                   typeof(FA18CEditSimulatorDTCPageHelper));

        public SystemBase GetSystemConfig(IConfiguration config) => ((FA18CConfiguration)config).MUMI;

        public List<ConfigEditorPageInfo> MergableSystems => new()
        {
            FA18CEditRadioPageHelper.PageInfo,
            FA18CEditCMSPage.PageInfo
        };

        /// <summary>
        /// validate the dtc configuration is correct. this checks to ensure the output path is valid and
        /// the template is known. the configuration is updated if necessary.
        /// </summary>
        public void ValidateDTCSystem(IConfiguration config)
        {
            ((FA18CConfiguration)config).MUMI.ValidateForAirframe(config.Airframe);
        }

        /// <summary>
        /// update the edit state for dtc from the configuration. the update will perform a deep copy of
        /// the data from the configuration.
        /// </summary>
        public void CopyConfigToEdit(IConfiguration config, SimDTCSystem editDTC)
        {
            editDTC.Template = new(((FA18CConfiguration)config).MUMI.Template);
            editDTC.OutputPath = new(((FA18CConfiguration)config).MUMI.OutputPath);
            editDTC.MergedSystemTags = new();
            foreach (string tag in ((FA18CConfiguration)config).MUMI.MergedSystemTags)
                editDTC.MergedSystemTags.Add(tag);
            editDTC.EnableLoad = new(((FA18CConfiguration)config).MUMI.EnableLoad);
        }

        /// <summary>
        /// update the configuration dtc from the edit state. the update will perform a deep copy of the
        /// data from the configuration.
        /// </summary>
        public void CopyEditToConfig(SimDTCSystem editDTC, IConfiguration config)
        {
            ((FA18CConfiguration)config).MUMI.Template = new(editDTC.Template);
            ((FA18CConfiguration)config).MUMI.OutputPath = new(editDTC.OutputPath);
            ((FA18CConfiguration)config).MUMI.MergedSystemTags = new();
            foreach (string tag in editDTC.MergedSystemTags)
                ((FA18CConfiguration)config).MUMI.MergedSystemTags.Add(tag);
            ((FA18CConfiguration)config).MUMI.EnableLoad = new(editDTC.EnableLoad);
        }
    }
}
