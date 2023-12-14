// ********************************************************************************************************************
//
// HARMBuilder.cs -- f-16c harm alic command builder
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
using JAFDTC.Models.F16C.HARM;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F16C.Upload
{
    /// <summary>
    /// command builder for the harm alic system in the viper. translates harm alic setup in F16CConfiguration into
    /// commands that drive the dcs clickable cockpit.
    /// </summary>
    internal class HARMBuilder : F16CBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public HARMBuilder(F16CConfiguration cfg, F16CCommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// configure harm alic system via the icp/ded according to the non-default programming settings (this function
        /// is safe to call with a configuration with default settings: defaults are skipped as necessary).
        /// <summary>
        public override void Build()
        {
            Device ufc = _aircraft.GetDevice("UFC");

            if (!_cfg.HARM.IsDefault)
            {
                AppendCommand(ufc.GetCommand("RTN"));
                AppendCommand(ufc.GetCommand("RTN"));

                AppendCommand(ufc.GetCommand("LIST"));
                AppendCommand(ufc.GetCommand("8"));

                AppendCommand(Wait());

                AppendCommand(StartCondition("NotInAAMode"));

                AppendCommand(ufc.GetCommand("SEQ"));
                AppendCommand(StartCondition("NotInAGMode"));

                BuildHARM(ufc);

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
        /// configure harm alic tables via the icp/ded according to the non-default programming settings.
        /// tables are only updated if they are non-default.
        /// <summary>
        private void BuildHARM(Device ufc)
        {
            AppendCommand(ufc.GetCommand("RTN"));
            AppendCommand(ufc.GetCommand("RTN"));

            AppendCommand(ufc.GetCommand("LIST"));
            AppendCommand(ufc.GetCommand("0"));

            //AppendCommand(StartCondition("NAV"));
            AppendCommand(ufc.GetCommand("AG"));

            //condition
            AppendCommand(StartCondition("HARM"));
            AppendCommand(ufc.GetCommand("0"));

            foreach (ALICTable table in _cfg.HARM.Tables)
            {
                if (!table.IsDefault)
                {
                    for (int i = 0; i < table.Table.Count; i++)
                    {
                        PredAppendDigitsWithEnter(ufc, table.Table[i].Code);
                        AppendCommand(ufc.GetCommand("DOWN"));
                    }
                }
                AppendCommand(ufc.GetCommand("INC"));
            }

            AppendCommand(EndCondition("HARM"));

            AppendCommand(ufc.GetCommand("AG"));

            AppendCommand(ufc.GetCommand("RTN"));
        }
    }
}