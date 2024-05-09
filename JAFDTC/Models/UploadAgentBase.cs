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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

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
        // private classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// core builder to set up a query with the given arguments.
        /// </summary>
        internal sealed class CoreQueryBuilder : BuilderBase, IBuilder
        {
            private readonly string _query;
            private readonly List<string> _argsQuery;

            public CoreQueryBuilder(IAirframeDeviceManager adm, StringBuilder sb, string query, List<string> argsQuery)
                : base(adm, sb) => (_query, _argsQuery) = (query, argsQuery);

            public override void Build()
            {
                AddQuery(_query, _argsQuery);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // internal classes
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// core setup builder to perform common setup actions at the start of a command stream. the base
        /// implementation sends a "start of upload" marker. derived classes should invoke base.Build() from their
        /// Build() methods before returning.
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
        /// core teardown builder to perform common teardown actions at the end of a command stream. the base
        /// implementation sends an "end of upload" marker. derived classes should invoke base.Build() from their
        /// Build() methods before returning.
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
        /// send a command string encoded in a StringBuilder to dcs via CockpitCmdTx. returns true on success, false
        /// on failure. note an empty command sequence is always sent successfully.
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
        /// post a query with the specified parameters to dcs and wait for a response. returns the string response
        /// from dcs on success, null on failure. this method is asynchronous with any other command sequences
        /// being sent to dcs by the upload agent.
        /// 
        /// typically, this function would be called using code similar to this:
        /// 
        ///     string response = null;
        ///     Task.Run(async () => {
        ///         response = await Query("TestQuery", new () { "Testing" });
        ///     }).Wait();
        ///     
        /// response provides the response from dcs, null if there were issues.
        /// </summary>
        private async Task<string> SendQueryToDCS(string query, List<string> argsQuery)
        {
            string preflight = null;
            App.DCSQueryResponseReceived += (object sender, string response) =>
            {
                // NOTE: we will be automatically unsubscribed from the event upon the response from dcs.

                preflight = new(response);
            };

            StringBuilder sbPreflight = new();
            new CoreQueryBuilder(null, sbPreflight, query, argsQuery).Build();

            if (SendCommandsToDCS(sbPreflight))
            {
                for (int i = 0; (i < 20) && (preflight == null); i++)
                {
                    await Task.Delay(50);
                }
            }
            if (preflight == null)
            {
                FileManager.Log("Query failed to send or response timed out, aborting..");
            }
            return preflight;
        }

        /// <summary>
        /// post a query with the specified parameters to dcs and wait for a response. returns the string response
        /// from dcs on success, null on failure. this method is asynchronous with any other command sequences
        /// being sent to dcs by the upload agent and will wait for the response or a time-out.
        /// </summary>
        public string Query(string query, List<string> argsQuery)
        {
            string response = null;
            Task.Run(async () => { response = await SendQueryToDCS(query, argsQuery); }).Wait();
            return response;
        }

        /// <summary>
        /// create the set of commands and state necessary to load a configuration on the jet, then send the
        /// commands to the jet for processing via the network connection to the dcs scripting engine. Load()
        /// uses SetupBuilder(), BuildSystems(), and TeardownBuilder() to create the command streams for the systems
        /// in the airframe. returns true on success, false on failure.
        /// </summary>
        public async Task<bool> Load()
        {
            StringBuilder sb = new();
            SetupBuilder(sb).Build();
            await Task.Run(() => BuildSystems(sb));
            if (sb.Length > 0)
            {
                TeardownBuilder(sb).Build();
            }
            return SendCommandsToDCS(sb);
        }

        /// <summary>
        /// derived classes must override this method to build out the system configurations, see IUploadAgent.
        /// </summary>
        public virtual void BuildSystems(StringBuilder sb)
        {
            // this function will typically create multiple IBuilder instances for various systems and use them to
            // build out the command string.
            //
            // prior to building systems, the function may use Query() to query dcs state. the reply from dcs can
            // then be used in the builders to provide information to inform the commands added to the builder.
            //
            // for example,
            //
            //     // submit query and block until response, then unpack the string from dcs into whatever
            //     // internal representation is appropriate.
            //     //
            //     string response = null;
            //     Task.Run(async () => {
            //         response = await Query("GetStateQuery", new () { "All" });
            //     }).Wait();
            //     object dcsState = UnpackResponse(response);
            //
            //     // create builder objects and add appropriate commands based on the state we received.
            //     //
            //     new MySystemBuilder(_cfg, _dcsCmds, sb, dcsState).Build();
            //
            // specifics will vary from jet to jet.
        }

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
