// ********************************************************************************************************************
//
// STPTBuilder.cs -- f-15e steerpoint command builder
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

using JAFDTC.Models.DCS;
using JAFDTC.Models.F15E.STPT;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace JAFDTC.Models.F15E.Upload
{
    /// <summary>
    /// command stream builder for the mudhen steerpoint system that covers steerpoints, target points, reference
    /// points, and other navigation-related settings.
    /// </summary>
    internal class STPTBuilder : F15EBuilderBase, IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public STPTBuilder(F15EConfiguration cfg, F15ECommands dcsCmds, StringBuilder sb) : base(cfg, dcsCmds, sb) { }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// TODO: document
        /// <summary>
        public override void Build()
        {
            ObservableCollection<SteerpointInfo> stpts = _cfg.STPT.Points;
            Device ufc = _aircraft.GetDevice("UFC_PILOT");

            if (!_cfg.STPT.IsDefault)
            {
                AppendCommand(StartCondition("GoToFrontCockpit"));
                AppendCommand(EndCondition("GoToFrontCockpit"));

                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("CLR"));
                AppendCommand(ufc.GetCommand("MENU"));
                AppendCommand(ufc.GetCommand("SHF"));
                AppendCommand(ufc.GetCommand("3"));         // B
                AppendCommand(ufc.GetCommand("PB10"));
                AppendCommand(ufc.GetCommand("PB10"));

                BuildSteerpoints(ufc, stpts);

                AppendCommand(ufc.GetCommand("MENU"));
                AppendCommand(ufc.GetCommand("1"));
                AppendCommand(ufc.GetCommand("SHF"));
                AppendCommand(ufc.GetCommand("1"));
                AppendCommand(ufc.GetCommand("PB10"));
            }
        }

        /// <summary>
        /// TODO: document
        /// <summary>
        private void BuildSteerpoints(Device ufc, ObservableCollection<SteerpointInfo> stpts)
        {
            foreach (SteerpointInfo stpt in stpts)
            {
                if (stpt.IsValid)
                {
                    string stptNum = stpt.Number.ToString();
                    AppendCommand(BuildDigits(ufc, stptNum));
                    AppendCommand(ufc.GetCommand("SHF"));               // TODO: SHF+1 => route a?
                    AppendCommand(ufc.GetCommand("1"));
                    AppendCommand(ufc.GetCommand("PB1"));

                    AppendCommand(StartCondition("IsStrDifferent", $"STR {stptNum}{stpt.Route}"));
                    AppendCommand(ufc.GetCommand("CLR"));
                    AppendCommand(ufc.GetCommand("CLR"));
                    AppendCommand(BuildDigits(ufc, stptNum));
                    AppendCommand(ufc.GetCommand("."));
                    AppendCommand(ufc.GetCommand("SHF"));               // TODO: SHF+1 => route a?
                    AppendCommand(ufc.GetCommand("1"));
                    AppendCommand(ufc.GetCommand("PB1"));
                    AppendCommand(EndCondition("IsStrDifferent"));

                    AppendCommand(StartCondition("IsStrDifferent", $"STR {stptNum}{stpt.Route}"));
                    AppendCommand(BuildDigits(ufc, stptNum));
                    AppendCommand(ufc.GetCommand("SHF"));               // TODO: SHF+1 => route a?
                    AppendCommand(ufc.GetCommand("1"));
                    AppendCommand(ufc.GetCommand("PB1"));
                    AppendCommand(EndCondition("IsStrDifferent"));

                    if (stpt.IsTarget)
                    {
                        AppendCommand(BuildDigits(ufc, stptNum));
                        AppendCommand(ufc.GetCommand("."));
                        AppendCommand(ufc.GetCommand("SHF"));           // TODO: SHF+1 => route a?
                        AppendCommand(ufc.GetCommand("1"));
                        AppendCommand(ufc.GetCommand("PB1"));
                    }

                    AppendCommand(Build2864Coordinate(ufc, stpt.LatUI));
                    AppendCommand(ufc.GetCommand("PB2"));

                    AppendCommand(Build2864Coordinate(ufc, stpt.LonUI));
                    AppendCommand(ufc.GetCommand("PB3"));

                    AppendCommand(BuildDigits(ufc, stpt.Alt));
                    AppendCommand(ufc.GetCommand("PB7"));

                    foreach (RefPointInfo rfpt in stpt.RefPoints)
                    {
                        AppendCommand(BuildDigits(ufc, stptNum));
                        AppendCommand(ufc.GetCommand("."));
                        if (stpt.IsTarget)
                        {
                            AppendCommand(BuildDigits(ufc, "0"));
                        }
                        AppendCommand(BuildDigits(ufc, rfpt.Number.ToString()));
                        AppendCommand(ufc.GetCommand("SHF"));           // TODO: SHF+1 => route a?
                        AppendCommand(ufc.GetCommand("1"));
                        AppendCommand(ufc.GetCommand("PB1"));

                        AppendCommand(Build2864Coordinate(ufc, rfpt.LatUI));
                        AppendCommand(ufc.GetCommand("PB2"));

                        AppendCommand(Build2864Coordinate(ufc, rfpt.LonUI));
                        AppendCommand(ufc.GetCommand("PB3"));

                        AppendCommand(BuildDigits(ufc, rfpt.Alt));
                        AppendCommand(ufc.GetCommand("PB7"));
                    }
                }
            }
        }
    }
}
