// ********************************************************************************************************************
//
// HTSBuilder.cs -- f-16c hts threat command builder
//
// Copyright(C) 2021-2023 the-paid-actor & others
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
    /// command builder for the hts system in the viper. translates cmds setup in F16CConfiguration into commands
    /// that drive the dcs clickable cockpit.
    /// </summary>
    internal class HTSBuilder : F16CBuilderBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HTSBuilder(F16CConfiguration cfg, F16CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure hts system via the icp/ded according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            Device ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.HTS.IsDefault)
            {
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("8"));

                AppendCommand(Wait());

                AppendCommand(StartCondition("NotInAAMode"));

                AppendCommand(ufc.GetCommand("SEQ"));
                AppendCommand(StartCondition("NotInAGMode"));

                BuildHTSManualTable(ufc);

                AppendCommand(EndCondition("NotInAGMode"));

                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("8"));
                AppendCommand(ufc.GetCommand("SEQ"));

                AppendCommand(EndCondition("NotInAAMode"));

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }

        /// <summary>
        /// configure hts manual table via the icp/ded according to the non-default programming settings. the manual
        /// table is only populated if it is non-default.
        /// <summary>
        private void BuildHTSManualTable(Device ufc)
        {
            if (_cfg.HTS.IsMANTablePopulated)
            {
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("0"));

                AppendCommand(Wait());

                AppendCommand(StartCondition("HTSOnDED"));

                AppendCommand(ufc.GetCommand("ENTR"));

                for (int row = 0; row < _cfg.HTS.MANTable.Count; row++)
                {
                    if (!PredAppendDigitsWithEnter(ufc, _cfg.HTS.MANTable[row].Code))
                    {
                        AppendCommand(ufc.GetCommand("DOWN"));
                    }
                }

                AppendCommand(EndCondition("HTSOnDED"));

                AppendCommand(ufc.GetCommand("RTN"));
            }
        }
    }
}