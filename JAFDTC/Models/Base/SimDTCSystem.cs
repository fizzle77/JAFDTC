// ********************************************************************************************************************
//
// SimDTCSystem.cs : simulator dtc system
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

using JAFDTC.Utilities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// class to capture the settings of the DTC "system". this serves as a helper to integrate JAFDTC system settings
    /// with settings from the built-in DTC in DCS. this system is airframe-agnostic
    /// </summary>
    public partial class SimDTCSystem : SystemBase
    {
        public const string SystemTag = "JAFDTC:Generic:DTC";

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties do not post change or validation events.

        private string _template;
        public string Template
        {
            get => _template;
            set => SetProperty(ref _template, value, null);
        }

        private string _outputPath;
        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value, null);
        }

        public ObservableCollection<string> MergedSystemTags { get; set; }

        private string _enableRebuild;
        public string EnableRebuild
        {
            get => _enableRebuild;
            set => ValidateAndSetBoolProp(value, ref _enableRebuild);
        }

        private string _enableLoad;
        public string EnableLoad
        {
            get => _enableLoad;
            set => ValidateAndSetBoolProp(value, ref _enableLoad);
        }

        /// <summary>
        /// returns true if the instance indicates a default setup, false otherwise.
        /// </summary>
        [JsonIgnore]
        public override bool IsDefault => ((Template.Length == 0) &&
                                           (OutputPath.Length == 0) &&
                                           (MergedSystemTags.Count == 0) &&
                                           (EnableRebuildValue == false) &&
                                           (EnableLoadValue == false));

        // ---- following accessors get the current value (default or non-default) for various properties

        [JsonIgnore]
        public bool EnableRebuildValue => !string.IsNullOrEmpty(EnableRebuild) && bool.Parse(EnableRebuild);

        [JsonIgnore]
        public bool EnableLoadValue => !string.IsNullOrEmpty(EnableLoad) && bool.Parse(EnableLoad);

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public SimDTCSystem()
        {
            Template = "";
            OutputPath = "";
            EnableRebuild = false.ToString();
            EnableLoad = false.ToString();
            MergedSystemTags = [ ];
        }

        public SimDTCSystem(SimDTCSystem other)
        {
            Template = new(other.Template);
            OutputPath = new(other.OutputPath);
            EnableRebuild = new(other.EnableRebuild);
            EnableLoad = new(other.EnableLoad);
            MergedSystemTags = [ ];
        }

        public virtual object Clone() => new SimDTCSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// validates the Template and OutputPath fields are valid. if not, they are reset to default values.
        /// </summary>
        public void ValidateForAirframe(AirframeTypes airframe)
        {
            if (!FileManager.IsValidDTCTemplate(airframe, Template))
                Template = "";
            if (!string.IsNullOrEmpty(OutputPath))
            {
                string path = Path.GetDirectoryName(OutputPath);
                if (!Directory.Exists(path))
                    OutputPath = "";
            }
        }

        /// <summary>
        /// reset the instance to defaults.
        /// </summary>
        public override void Reset()
        {
            Template = "";
            OutputPath = "";
            EnableRebuild = false.ToString();
            EnableLoad = false.ToString();
            MergedSystemTags.Clear();
        }
    }
}
