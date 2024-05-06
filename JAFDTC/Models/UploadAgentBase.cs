// ********************************************************************************************************************
//
// UploadAgentBase.cs -- abstract base class for an upload agent
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

// define this to enable dcs command stream logging to the log file.
//
#define noDEBUG_CMD_LOGGING

using JAFDTC.Models.DCS;
using JAFDTC.Utilities;
using JAFDTC.Utilities.Networking;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Store;
using Windows.System;

namespace JAFDTC.Models
{
    /// <summary>
    /// abstract base class for an upload agent responsible for building a stream of commands for use by dcs to set
    /// up avionics according to a configuration.
    /// 
    /// derived classes must override BuildSystems() and may optionally override SetupBuilder() and TeardownBuilder()
    /// if they have setup/teardown commands to generate before or after the system configuration commands are built.
    /// </summary>
    public abstract class UploadAgentBase : IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // internal classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// core setup builder to perform common setup actions at the start of a command streams such as sending the
        /// "start of upload" marker. derived classes should invoke base.Build from their Build() methods before
        /// returning.
        /// 
        /// instances of this class may be built with a null IAirframeDeviceManager.
        /// </summary>
        internal class CoreSetupBuilder : BuilderBase, IBuilder
        {
            public CoreSetupBuilder(IAirframeDeviceManager adm, StringBuilder sb) : base(adm, sb) { }

            public override void Build()
            {
                AddMarker("<upload_prog>");
            }
        }

        /// <summary>
        /// core setup builder to perform common teardown actions at the end of a command streams such as sending the
        /// "end of upload" marker. derived classes should invoke base.Build from their Build() methods before
        /// returning.
        /// 
        /// instances of this class may be built with a null IAirframeDeviceManager.
        /// </summary>
        internal class CoreTeardownBuilder : BuilderBase, IBuilder
        {
            public CoreTeardownBuilder(IAirframeDeviceManager adm, StringBuilder sb) : base(adm, sb) { }

            public override void Build()
            {
                AddMarker("");
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public UploadAgentBase() { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// send a command string to dcs via CockpitCmdTx. returns true on success, false on failure.
        /// </summary>
        private static bool SendCommandsToDCS(StringBuilder sb)
        {
            if (sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
            }
            string str = sb.ToString();
            if (!string.IsNullOrEmpty(str))
            {
#if DEBUG_CMD_LOGGING
                FileManager.Log($"CMD stream data size is {str.Length}");
                FileManager.Log($"CMD stream:\n****************\n{str}\n****************");
#endif
                return CockpitCmdTx.Send(str);
            }
            return true;
        }

        /// <summary>
        /// TODO: document
        /// </summary>
        public async Task<bool> Load(App curApp)
        {
            string preflight = null;
            if (PreflightBuilder != null)
            {
                curApp.DCSQueryResponseReceived += (object sender, string response) =>
                {
                    preflight = new(response);
                };

                StringBuilder sbPreflight = new();
                PreflightBuilder(sbPreflight).Build();

                if (!SendCommandsToDCS(sbPreflight))
                {
                    return false;
                }
                for (int i = 0; (i < 20) && string.IsNullOrEmpty(preflight); i++)
                {
                    await Task.Delay(50);
                }
                if (preflight == null)
                {
                    FileManager.Log("Load query response timed out, aborting configuration load");
                    return false;
                }
            }

            // TODO: handle preflight data here...
            StringBuilder sbConfig = new();
            SetupBuilder(sbConfig).Build();
            BuildSystems(sbConfig);
            TeardownBuilder(sbConfig).Build();

            return SendCommandsToDCS(sbConfig);
        }

        /// <summary>
        /// derived classes must override this method to build out the system configurations.
        /// </summary>
        public virtual void BuildSystems(StringBuilder sb) { }

        /// <summary>
        /// derived classes may override this method to return a different builder.
        /// </summary>
        public virtual IBuilder PreflightBuilder(StringBuilder sb) => null;

        /// <summary>
        /// derived classes may override this method to return a different builder.
        /// </summary>
        public virtual IBuilder SetupBuilder(StringBuilder sb) => new CoreSetupBuilder(null, sb);

        /// <summary>
        /// TODOderived classes may override this method to return a different builder.
        /// </summary>
        public virtual IBuilder TeardownBuilder(StringBuilder sb) => new CoreTeardownBuilder(null, sb);
    }
}
