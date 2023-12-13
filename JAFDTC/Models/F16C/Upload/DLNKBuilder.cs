// ********************************************************************************************************************
//
// DLNKBuilder.cs -- f-16c cmds command builder
//
// Copyright(C) 2023 ilominar/raven
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

using JAFDTC.Models.DCS;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the datalink system in the viper. translates cmds setup in F16CConfiguration into commands
    /// that drive the dcs clickable cockpit.
    /// </summary>
    internal class DLNKBuilder : F16CBuilderBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public DLNKBuilder(F16CConfiguration cfg, F16CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure cmds system via the icp/ded according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            Device ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.DLNK.IsDefault)
            {
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("ENTR"));

                AppendCommand(ufc.GetCommand("SEQ"));

                // TODO: need to check for ded state of flight lead to do this right
#if TODO
                AppendCommand(ufc.GetCommand("DOWN"));
                AppendCommand(ufc.GetCommand("DOWN"));
                AppendCommand(ufc.GetCommand("DOWN"));
                AppendCommand(ufc.GetCommand("DOWN"));
                AppendCommand(ufc.GetCommand("DOWN"));

                // TODO: should wrap this in a check of flight lead
                if (_cfg.DLNK.IsOwnshipLead)
                {
                }
#endif

                AppendCommand(ufc.GetCommand("SEQ"));

                for (int i = 0; i < _cfg.DLNK.TeamMembers.Length; i++)
                {
                    // TODO: need to check state of tdoa in ded to do this right
#if TODO
                    if (_cfg.DLNK.TeamMembers[i].TDOA)
                    {
                    }
#endif
                    AppendCommand(ufc.GetCommand("DOWN"));

                    if (!PredAppendDigitsWithEnter(ufc, _cfg.DLNK.TeamMembers[i].TNDL))
                    {
                        PredAppendDigitsWithEnter(ufc, "00000");
                    }
                }

                PredAppendDigitsWithEnter(ufc, _cfg.DLNK.Ownship);

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }
    }
}
