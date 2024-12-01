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
using JAFDTC.Utilities.Networking;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.Models
{
    /// <summary>
    /// abstract base class for an upload agent responsible for building a stream of commands for use by dcs to set
    /// up avionics according to a configuration and sending those commands to dcs via the network.
    /// 
    /// derived classes specialize the agent for a specific airframe and configuration. derived classes must override
    /// BuildSystems() and may optionally override SetupBuilder() and TeardownBuilder() if they have setup/teardown
    /// commands streams to generate before or after the system configuration command stream proper. these methods
    /// create appropriate IBuilder instances to build command streams for various parts of the configuration.
    /// </summary>
    public abstract class UploadAgentBase : IUploadAgent
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // internal classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// core setup builder to perform common setup actions at the start of a command stream.  SetupBuilder()
        /// returns an instance of this class. the base implementation sends a "start of upload" marker. derived
        /// classes should invoke base.Build() from their Build() methods before returning.
        /// 
        /// instances of this class may be built with a null IAirframeDeviceManager.
        /// </summary>
        internal class CoreSetupBuilder : BuilderBase, IBuilder
        {
            public CoreSetupBuilder(IAirframeDeviceManager adm, StringBuilder sb) : base(adm, sb) { }

            public override void Build(Dictionary<string, object> state = null)
            {
                AddExecFunction("NOP", new() { "==== CoreSetupBuilder:Build()" });
                AddMarker("<upload_prog>");
            }
        }

        /// <summary>
        /// core teardown builder to perform common teardown actions at the end of a command stream. TeardownBuilder()
        /// returns an instance of this class. the base implementation sends an "end of upload" marker. derived
        /// classes should invoke base.Build() from their Build() methods before returning.
        /// 
        /// instances of this class may be built with a null IAirframeDeviceManager.
        /// </summary>
        internal class CoreTeardownBuilder : BuilderBase, IBuilder
        {
            public CoreTeardownBuilder(IAirframeDeviceManager adm, StringBuilder sb) : base(adm, sb) { }

            public override void Build(Dictionary<string, object> state = null)
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
        /// load a configuration onto the jet, see IUploadAgent.
        /// </summary>
        public async Task<bool> Load()
        {
            StringBuilder sb = new();
            IBuilder setupBuilder = SetupBuilder(sb);
            IBuilder teardownBuilder = TeardownBuilder(sb);

            setupBuilder.Build();
            await Task.Run(() => BuildSystems(sb));
            teardownBuilder.Build();

            string str = teardownBuilder.ToString();
            if (!string.IsNullOrEmpty(str))
            {
#if DEBUG_CMD_LOGGING
                FileManager.Log($"UploadAgentBase stream data size is {str.Length}");
                FileManager.Log($"UploadAgentBase stream:\n****************\n{str}\n****************");
#endif
                return CockpitCmdTx.Send(str);
            }
            return true;
        }

        /// <summary>
        /// derived classes must override this method to build out the system configurations, see IUploadAgent.
        /// </summary>
        public virtual void BuildSystems(StringBuilder sb) { }

        /// <summary>
        /// derived classes may override this method to return a different builder, see IUploadAgent.
        /// </summary>
        public virtual IBuilder SetupBuilder(StringBuilder sb) => new CoreSetupBuilder(null, sb);

        /// <summary>
        /// TODOderived classes may override this method to return a different builder, see IUploadAgent.
        /// </summary>
        public virtual IBuilder TeardownBuilder(StringBuilder sb) => new CoreTeardownBuilder(null, sb);
    }
}
