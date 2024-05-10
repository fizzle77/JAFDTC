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
        /// core builder to set up a query with the given arguments. instances of this class may be passed to Query()
        /// for query processing. the base implementation adds the query command to the command stream. derived
        /// classes must follow one of these two implementation paths:
        /// 
        /// (1) provide query and argsQuery at construction time and, if they override Build(), invoke base.Build()
        ///     as their last action before returning.
        /// (2) do not provide query and argsQuery at construction time and invoke AddQuery() as the last command
        ///     in the command stream in their Build() implementation.
        /// 
        /// instances of this class may be built with a null IAirframeDeviceManager.
        /// </summary>
        internal class CoreQueryBuilder : BuilderBase, IBuilder
        {
            private readonly string _query;
            private readonly List<string> _argsQuery;

            public CoreQueryBuilder(IAirframeDeviceManager adm, StringBuilder sb, string query = null,
                                    List<string> argsQuery = null)
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
        /// core setup builder to perform common setup actions at the start of a command stream.  SetupBuilder
        /// returns an instance of this class. the base implementation sends a "start of upload" marker. derived
        /// classes should invoke base.Build() from their Build() methods before returning.
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
        /// core teardown builder to perform common teardown actions at the end of a command stream. TeardownBuilder
        /// returns an instance of this class. the base implementation sends an "end of upload" marker. derived
        /// classes should invoke base.Build() from their Build() methods before returning.
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
        /// send a command sequence encoded in a IBuilder to dcs via CockpitCmdTx. returns true on success, false on
        /// failure. note an empty command sequence is always sent successfully.
        /// </summary>
        private static bool SendCommandsToDCS(IBuilder builder)
        {
            string str = builder.ToString();
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
        ///     Task.Run(async () => { response = await SendQueryToDCS("TestQuery", new () { "Testing" }); }).Wait();
        ///     
        /// response provides the response from dcs, null if there were issues.
        /// </summary>
        private static async Task<string> SendQueryToDCS(string query, List<string> argsQuery, IBuilder builder)
        {
            if (builder == null)
            {
                StringBuilder sbPreflight = new();
                builder = new CoreQueryBuilder(null, sbPreflight, query, argsQuery);
                builder.Build();
            }

            string preflight = null;
            App.DCSQueryResponseReceived += (object sender, string response) =>
            {
                preflight = new(response);
                //
                // NOTE: we are automatically unsubscribed from the event upon the response from dcs.
            };

            if (SendCommandsToDCS(builder))
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
        /// post a query with the specified parameters to dcs and wait for a response. the query is added to the end
        /// of the sequence in the specified builder (if null, a new empty builder is used). builder is typically
        /// derived from CoreQueryBuilder. returns the string response from dcs to the query on success, null on
        /// failure.
        /// 
        /// this method is asynchronous with any other command sequences being sent to dcs by the upload agent and
        /// blocks on the response or a time-out.
        /// </summary>
        public static string Query(string query, List<string> argsQuery, IBuilder builder = null)
        {
            string response = null;
            Task.Run(async () => { response = await SendQueryToDCS(query, argsQuery, builder); }).Wait();
            return response;
        }

        /// <summary>
        /// create the stream of commands and state necessary to load a configuration on the jet, then send the
        /// commands to the jet for processing via our network connection to the dcs scripting engine. Load()
        /// uses SetupBuilder(), BuildSystems(), and TeardownBuilder() to create the command streams for the systems
        /// in the airframe. returns true on success, false on failure.
        /// </summary>
        public async Task<bool> Load()
        {
            StringBuilder sb = new();
            IBuilder setupBuilder = SetupBuilder(sb);
            IBuilder teardownBuilder = TeardownBuilder(sb);

            setupBuilder.Build();
            await Task.Run(() => BuildSystems(sb));
            teardownBuilder.Build();

            return SendCommandsToDCS(teardownBuilder);
        }

        /// <summary>
        /// derived classes must override this method to build out the system configurations, see IUploadAgent.
        /// </summary>
        public virtual void BuildSystems(StringBuilder sb)
        {
            // this function will typically create multiple IBuilder instances for various systems and use them to
            // build out the command string.
            //
            // while building systems, the function may use Query() to query dcs state. the reply from dcs can
            // then be used in the builders to provide information to inform the commands added to the builder.
            //
            // for example,
            //
            //     // (optional) build out any command sequences that are needed before the actual query
            //     // command.
            //     //
            //     StringBuilder sbQuery = new();
            //     MyCoreQueryBuilder queryBuilder = new(_cfg, _dcsCmds, sbQuery).Build();
            //
            //     // submit query and block until response, then unpack the string from dcs into whatever
            //     // internal representation is appropriate.
            //     //
            //     string response = Query("GetState", new () { "All" }, queryBuilder);
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
