// ********************************************************************************************************************
//
// BuilderBase.cs -- base class for command builder
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

#define noDEBUG_CMD_FORMAT

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// abstract base class for a command builder. command builders generate a list of commands to send to dcs that
    /// effect actions on airframe devices within the clickable cockpit to arrive at a desired configuration.
    /// derived classes may extend the base to handle airframe- or system-specific needs (for example, to generate
    /// the correct set of actions to specify a negative number in an avionics system).
    /// </summary>
    public abstract class BuilderBase : IBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // types & constants
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// delegate to emit commands (using the "Add" methods the builder provides) for a block (eg, the body of an
        /// if/for/while construct) to the command stream the builder is building.
        /// </summary>
        public delegate void AddBlockCommandsDelegate();

        // common wait durations (ms) for AddWait().
        //
        protected const int WAIT_NONE = 0;
        protected const int WAIT_SHORT = 100;
        protected const int WAIT_BASE = 200;
        protected const int WAIT_LONG = 600;
        protected const int WAIT_VERY_LONG = 17000;

        // in debug command format, add a newline after every command to make it easier to read the command sequences
        // that the builder generates.
        //
#if DEBUG_CMD_FORMAT
        private const string CMD_EOL = "\n";
#else
        private const string CMD_EOL = "";
#endif

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        protected readonly IAirframeDeviceManager _aircraft;

        private readonly StringBuilder _sb;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public BuilderBase(IAirframeDeviceManager aircraft, StringBuilder sb) => (_aircraft, _sb) = (aircraft, sb);

        // ------------------------------------------------------------------------------------------------------------
        //
        // IBuilder methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public abstract void Build();

        // ------------------------------------------------------------------------------------------------------------
        //
        // private methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// append a string to the command the builder is building.
        /// </summary>
        private void AddCommand(string s)
        {
            _sb.Append(s + CMD_EOL);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // command building methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// add an abort command to the command the builder is building.
        /// </summary>
        protected void AddAbort(string message)
        {
            string cmd = $"{{\"f\":\"Abort\",\"a\":{{\"msg\":\"{message}\"}}}},";
            AddCommand(cmd);
        }

        /// <summary>
        /// add an action for the key to the command the builder is buidling.
        /// </summary>
        protected void AddAction(AirframeDevice device, string key, int dtWaitPost = WAIT_NONE)
        {
            AddCommand(device[key]);
            AddWait(dtWaitPost);
        }

        /// <summary>
        /// add a dyamic action for the key to the command the builder is buidling. a dynamic action has
        /// caller-specified values for the up/down values. this allows programatic switching of controls from
        /// windows.
        /// </summary>
        protected void AddDynamicAction(AirframeDevice device, string key, double valueDn, double valueUp)
        {
            AddCommand(device.CustomizedDCSActionCommand(key, valueDn, valueUp));
        }

        /// <summary>
        /// add actions for the keys in the provided list followed by a post-list set of keys to the command the
        /// builder is building. each key must be an action the key pad device supports
        /// </summary>
        protected void AddActions(AirframeDevice device, List<string> keys, List<string> keysPost = null,
                                  int dtWaitPost = WAIT_NONE)
        {
            foreach (string key in keys)
                AddAction(device, key);
            if (keysPost != null)
            {
                foreach (string key in keysPost)
                    AddAction(device, key);
            }
            AddWait(dtWaitPost);
        }

        /// <summary>
        /// add a wait command to the command the builder is building.
        /// </summary>
        protected void AddWait(int dt)
        {
            if (dt > 0)
            {
                string cmd = $"{{\"f\":\"Wait\",\"a\":{{\"dt\":{dt}}}}},";
                AddCommand(cmd);
            }
        }

        /// <summary>
        /// add a marker command to the command the builder is building.
        /// </summary>
        protected void AddMarker(string marker)
        {
            string cmd = $"{{\"f\":\"Marker\",\"a\":{{\"mark\":\"{marker}\"}}}},";
            AddCommand(cmd);
        }

        /// <summary>
        /// add a run function command to the command the builder is building.
        /// </summary>
        protected void AddRunFunction(string fn)
        {
            string cmd = $"{{\"f\":\"RunFunc\",\"a\":{{\"fn\":\"{fn}\"}}}},";
            AddCommand(cmd);
        }

        /// <summary>
        /// add an if block to the command the builder is building. the block is delimited by "If" and "EndIf"
        /// commands with the AddBlockCommandsDelegate emitting the commands within the block that are exectued
        /// if the condition is true.
        /// 
        /// NOTE: nested if blocks are assumed to have unique cond values.
        /// </summary>
        protected void AddIfBlock(string cond, List<string> argsCond, AddBlockCommandsDelegate addBlockDelegate)
        {
            string cmd = $"{{\"f\":\"If\",\"a\":{{\"cond\":\"{cond}\"";
            if (argsCond != null)
            {
                for (int i = 0; i < argsCond.Count; i++)
                {
                    cmd += $",\"prm{i}\":\"{argsCond[i]}\"";
                }
            }
            cmd += $"}}}},";
            AddCommand(cmd);

            addBlockDelegate();

            cmd = $"{{\"f\":\"EndIf\",\"a\":{{\"cond\":\"{cond}\"}}}},";
            AddCommand(cmd);
        }

        /// <summary>
        /// add a while block to the command the builder is building. the block is delimited by "While" and
        /// "EndWhile" commands with the AddBlockCommandsDelegate emitting the commands within the block that
        /// are exectued while the condition is true.
        /// 
        /// NOTE: nested while blocks are assumed to have unique cond values.
        /// </summary>
        protected void AddWhileBlock(string cond, List<string> argsCond, AddBlockCommandsDelegate addBlockDelegate)
        {
            string cmd = $"{{\"f\":\"While\",\"a\":{{\"cond\":\"{cond}\"";
            if (argsCond != null)
            {
                for (int i = 0; i < argsCond.Count; i++)
                {
                    cmd += $",\"prm{i}\":\"{argsCond[i]}\"";
                }
            }
            cmd += $"}}}},";
            AddCommand(cmd);

            addBlockDelegate();

            cmd = $"{{\"f\":\"EndWhile\",\"a\":{{\"cond\":\"{cond}\"}}}},";
            AddCommand(cmd);
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // action list methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns a list of actions to enter the string value. all characters that appear in the string must have
        /// corresponding actions in the airframe device that the actions will target.
        /// </summary>
        protected List<string> ActionsForString(string value)
        {
            List<string> actions = new();
            foreach (var c in value.ToCharArray())
            {
                actions.Add(c.ToString());
            }
            return actions;
        }

        /// <summary>
        /// returns a list of actions to enter the numeric value (with leading zeros and separators removed). returns
        /// the list of actions. all characters that appear in the string must have corresponding actions in the
        /// airframe device that the actions will target.
        /// </summary>
        public List<string> ActionsForCleanNum(string value)
        {
            return ActionsForString(AdjustNoSeparators(AdjustNoLeadZeros(value)));
        }

        /// <summary>
        /// build the list of actions necessary to enter a lat/lon coordinate into a navpoint system that uses
        /// the 2/8/6/4 buttons to enter N/S/E/W directions. coordinate is specified as a string. prior to processing,
        /// all separators are removed. the coordinate string should start with N/S/E/W followed by the digits
        /// and/or characters that should be typed in to the device. they device must have single-character actions
        /// that map to the non-separator characters that may appear in the coordinate string.
        /// <summary>
        protected List<string> ActionsFor2864CoordinateString(string coord)
        {
            coord = AdjustNoSeparators(coord.Replace(" ", ""));

            List<string> actions = new();
            foreach (char c in coord.ToUpper().ToCharArray())
            {
                switch (c)
                {
                    case 'N': actions.Add("2"); break;
                    case 'S': actions.Add("8"); break;
                    case 'E': actions.Add("6"); break;
                    case 'W': actions.Add("4"); break;
                    default: actions.Add(c.ToString()); break;
                }
            }
            return actions;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // string adjustment methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// adjust a string by removing all non-alphanumeric characters. returns adjusted value.
        /// </summary>
        protected static string AdjustOnlyAlphaNum(string s)
        {
            return (string.IsNullOrEmpty(s)) ? s : Regex.Replace(s, "[^a-zA-Z0-9 ]", "");
        }

        /// <summary>
        /// adjust a numeric string by removing all leading zeros. returns adjusted value.
        /// </summary>
        protected static string AdjustNoLeadZeros(string s)
        {
            bool isEmpty = string.IsNullOrEmpty(s);
            while (s.StartsWith("0"))
            {
                s = s.Remove(0, 1);
            }
            return (!isEmpty && (s == "")) ? "0" : s;
        }

        /// <summary>
        /// adjust a string by removing all separator characters. returns adjusted value.
        /// </summary>
        protected static string AdjustNoSeparators(string s)
        {
            return s.Replace(",", "").Replace(".", "").Replace("°", "").Replace("’", "").Replace("”", "")
                    .Replace("\"", "").Replace("'", "").Replace(":", "");
        }

        /// <summary>
        /// adjust a hh:mm:ss value by a zulu delta for entry into a navpoint system. the hour field is adjusted by
        /// adding dZ, where dZ is the +/- delta from local to zulu. returns adjusted value, empty string if the
        /// input is invalid.
        /// </summary>
        protected static string AdjustHMSForZulu(string tos, int dz)
        {
            string[] hms = tos.Split(':');
            if ((hms.Length == 3) &&
                int.TryParse(hms[0], out int h) && int.TryParse(hms[1], out int m) && int.TryParse(hms[2], out int s))
            {
                h += dz;
                while (h < 0)
                    h += 24;
                h %= 24;
                return $"{h:D2}{m:D2}{s:D2}";
            }
            return "";
        }
    }
}
