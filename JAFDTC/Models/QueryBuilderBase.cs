// ********************************************************************************************************************
//
// QueryBuilderBase.cs -- base class for a query command stream builder
//
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace JAFDTC.Models
{
    /// <summary>
    /// base class for a command stream builder that sets up a dcs query. like BuilderBase, this class manages the
    /// assembly of a command sequence. in addition, it provides the Query() method to submit the query and await
    /// a response from dcs.
    /// 
    /// command streams built by this class consist of optional commands to configure avionics followed by a "query"
    /// command from AddQuery() that requests information from dcs.
    /// 
    /// in the base implementation, the command stream consists of a single query command with the function name
    /// and arguments specified at construction time. derived classes may change this behavior by overriding the
    /// Build() method.
    /// 
    /// instances of this class may be built with a null IAirframeDeviceManager.
    /// </summary>
    public class QueryBuilderBase : BuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private readonly string _fnQuery;
        private readonly List<string> _argsQuery;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public QueryBuilderBase(IAirframeDeviceManager adm, StringBuilder sb, string fnQuery = null,
                                List<string> argsQuery = null)
            : base(adm, sb) => (_fnQuery, _argsQuery) = (fnQuery, argsQuery);

        // ------------------------------------------------------------------------------------------------------------
        //
        // IBuilder methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// build the command stream for the query. default implementation adds a query with the name and arguments
        /// set up at construction time. derived classes may override this method to generate different sequences.
        /// sequences should always end with a query command.
        /// </summary>
        public override void Build()
        {
            AddQuery(_fnQuery, _argsQuery);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// task function to support query submission.
        /// </summary>
        private async Task<string> SendAsQuery()
        {
            string preflight = null;
            EventHandler<string> responseHandler = new(delegate (object sender, string response)
            {
                preflight = new(response);
                //
                // NOTE: we are automatically unsubscribed from the event upon the response from dcs.
            });
            App.DCSQueryResponseReceived += responseHandler;

            string str = this.ToString();
            if (!string.IsNullOrEmpty(str))
            {
#if DEBUG_CMD_LOGGING
                FileManager.Log($"QueryBuilderBase stream data size is {str.Length}");
                FileManager.Log($"QueryBuilderBase stream:\n****************\n{str}\n****************");
#endif
                if (CockpitCmdTx.Send(str))
                {
                    for (int i = 0; (i < 20) && (preflight == null); i++)
                    {
                        await Task.Delay(50);
                    }
                    App.DCSQueryResponseReceived -= responseHandler;
                }
            }
            if (preflight == null)
            {
                FileManager.Log("QueryBuilderBase query failed to send or response timed out, aborting...");
            }
            return preflight;
        }

        /// <summary>
        /// post the command stream for the builder over the network as a query to dcs and block for a response.
        /// returns the string response from dcs on success, null on failure. the command stream must contain a
        /// query command (added via AddQuery()).
        /// 
        /// this method is asynchronous with any other command sequences being sent to dcs by the upload agent and
        /// blocks on the response or a time-out.
        /// </summary>
        public string Query()
        {
            string response = null;
            Task.Run(async () => { response = await SendAsQuery(); }).Wait();
            return response;
        }
    }
}
