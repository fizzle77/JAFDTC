// ********************************************************************************************************************
//
// BuilderBase.cs -- base class for command builder
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023-2025 ilominar/raven
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
#define noDEBUG_LOG_ACTIONS
#define DEBUG_LOG_BOGUS_ACTIONS

using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// Dictionary extension to provide TryGetValueAs() on Dictionary instances. this allows the use of generic
    /// value types (say, object) that can be cast to the right known type inline. this might be handy for state
    /// dictionaries in BuilderBase...
    /// 
    /// hat tip to: https://stackoverflow.com/a/63203652
    /// 
    /// </summary>
    public static class DictionaryExtensions
    {
        public static bool TryGetValueAs<Key, Value, ValueAs>(this IDictionary<Key, Value> dict,
                                                              Key key, out ValueAs valueAs) where ValueAs : Value
        {
            if (dict.TryGetValue(key, out Value value))
            {
                valueAs = (ValueAs)value;
                return true;
            }

            valueAs = default;
            return false;
        }
    }

    // ================================================================================================================

    /// <summary>
    /// debug command builder class to inject a single exec function into the command stream.
    /// </summary>
    public class DebugBuilder : BuilderBase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        private string _debugFn;
        private List<string> _debugArgs;

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public DebugBuilder(IAirframeDeviceManager dm, StringBuilder sb, string fn, List<string> args)
            : base(dm, sb)
        {
            _debugFn = fn;
            _debugArgs = args;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // build methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// build the command stream with the specified debug function exec and add it to internal object state.
        /// <summary>
        public override void Build(Dictionary<string, object> state = null)
        {
            AddExecFunction(_debugFn, _debugArgs);
        }
    }

    // ================================================================================================================

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

        // common wait durations (ms) for AddWait() and other delay parameters.
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

        public BuilderBase(IAirframeDeviceManager aircraft, StringBuilder sb)
        {
            _aircraft = aircraft;
            _sb = sb ?? new StringBuilder();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // IBuilder methods
        //
        // ------------------------------------------------------------------------------------------------------------

        public override string ToString()
        {
            // NOTE: Add* methods always end the command they add with a "," delimiter. remove this delimeter prior
            // NOTE: to returning the string.
            //
            string value = _sb.ToString();
            return (value.Length > 0) ? value.Remove(value.Length - 1, 1) : value;
        }

        public abstract void Build(Dictionary<string, object> state = null);

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

        /// <summary>
        /// return an argument list to append to a command string. the argument list is of the form,
        /// 
        ///   "prm" : { "arg[0]", "arg[1]", ... arg[n-1]" }
        ///   
        /// where elements of args parameter are output in a list of strings with "prm" key. return value starts
        /// with "," if argument list is non-empty.
        /// 
        /// NOTE: delimeters between elements around the parameter list are left to caller to insert as needed.
        /// </summary>
        private static string BuildArgList(List<string> args)
        {
            string retVal = "";
            if ((args != null) && (args.Count > 0))
            {
                string prefix = ",\"prm\":[";
                for (int i = 0; i < args.Count; i++)
                {
                    retVal += $"{prefix}\"{args[i]}\"";
                    prefix = ",";
                }
                retVal += "]";
            }
            return retVal;
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // protected methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// clear the contents fo the command stream.
        /// </summary>
        protected void ClearCommands()
        {
            _sb.Clear();
        }

        /// <summary>
        /// add the results of a buid to the builder. the method invokes Build() on the IBuilder instance and then
        /// appends the resulting command stream to this builder.
        /// </summary>
        protected void AddBuild(IBuilder builder, Dictionary<string, object> state = null)
        {
            builder.Build(state);
            //
            // NOTE: adds always leave a trailing "," on the build string as it is constructed. ToString() pulls this,
            // NOTE: so we put an extra one here so that following the AddCommand() the build string will still meet
            // NOTE: expectations.
            //
            string commandStream = builder.ToString();
            if (commandStream.Length > 0)
                AddCommand($"{commandStream},");
        }

        /// <summary>
        /// add an abort command to the command stream the builder is building. if the message starts with "ERROR: ",
        /// the message (excluding "ERROR: ") will be output to the user through a dcs message.
        /// </summary>
        protected void AddAbort(string message)
        {
            string cmd = $"{{\"f\":\"Abort\",\"a\":{{\"msg\":\"{message}\"}}}},";
            AddCommand(cmd);
        }

        /// <summary>
        /// add an action for the key to the command stream the builder is buidling.
        /// </summary>
        protected void AddAction(AirframeDevice device, string key, int dtWaitPost = WAIT_NONE)
        {
#if DEBUG_LOG_ACTIONS
            FileManager.Log($"{device.Name}.{key} -- {device.ActionToString(key)}");
#endif
#if DEBUG_LOG_BOGUS_ACTIONS
            if (string.IsNullOrEmpty(device[key]))
            {
                FileManager.Log($"Action {device.Name}.{key} is undefined");
            }
#endif
            AddCommand(device[key]);
            AddWait(dtWaitPost);
        }

        /// <summary>
        /// add a action for the key to the command stream the builder is buidling. the action is updated to use the
        /// specified delay between up and down rather than the base delay.
        /// </summary>
        protected void AddActionWithDelay(AirframeDevice device, string key, int delay)
        {
            AddCommand(device.CustomizedDCSActionCommand(key, delay));
        }

        /// <summary>
        /// add a dyamic action for the key to the command stream the builder is buidling. a dynamic action has
        /// caller-specified values for the up/down values. this allows programatic switching of controls from
        /// windows.
        /// </summary>
        protected void AddDynamicAction(AirframeDevice device, string key, double valueDn, double valueUp, int delay = 0)
        {
            AddCommand(device.CustomizedDCSActionCommand(key, delay, true, valueDn, valueUp));
        }

        /// <summary>
        /// add actions for the keys in the provided list followed by a post-list set of keys to the command stream
        /// the builder is building. each key must be an action the key pad device supports
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
        /// Perform the action count number of times, followed by a wait of dtWaitPost milliseconds.
        /// </summary>
        protected void AddActions(AirframeDevice device, string action, int count, int dtWaitPost = WAIT_NONE)
        {
            if (count < 1)
                return;

            for (int i = 0; i < count; i++)
                AddAction(device, action);

            AddWait(dtWaitPost);
        }

        /// <summary>
        /// add a wait command to the command stream the builder is building.
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
        /// add a marker command to the command stream the builder is building.
        /// </summary>
        protected void AddMarker(string marker)
        {
            string cmd = $"{{\"f\":\"Marker\",\"a\":{{\"mark\":\"{marker}\"}}}},";
            AddCommand(cmd);
        }

        /// <summary>
        /// add a query command to the command stream the builder is building. dcs returns query results
        /// asynchronously, this command should only be used in conjunction with UploadAgentBase::Query() method with
        /// no more than one per sequence.
        /// </summary>
        protected void AddQuery(string fn, List<string> argsQuery = null)
        {
            string cmd = $"{{\"f\":\"Query\",\"a\":{{\"fn\":\"{fn}\"" + BuildArgList(argsQuery) + $"}}}},";
            AddCommand(cmd);
        }

        /// <summary>
        /// add an exec function command to the command stream the builder is building.
        /// </summary>
        protected void AddExecFunction(string fn, List<string> argsFunc = null, int dtWaitPost = WAIT_NONE)
        {
            string cmd = $"{{\"f\":\"Exec\",\"a\":{{\"fn\":\"{fn}\"" + BuildArgList(argsFunc) + $"}}}},";
            AddCommand(cmd);
            AddWait(dtWaitPost);
        }

        /// <summary>
        /// add an if block to the command stream the builder is building. the block is delimited by "If" and "EndIf"
        /// commands with the AddBlockCommandsDelegate emitting the commands within the block that are exectued
        /// if the condition matches the expect value.
        /// 
        /// NOTE: nested if blocks are assumed to have unique cond values.
        /// </summary>
        protected void AddIfBlock(string cond, bool expect, List<string> argsCond,
                                  AddBlockCommandsDelegate addBlockDelegate)
        {
            string cmd = $"{{\"f\":\"If\",\"a\":{{\"cond\":\"{cond}\",\"expt\":\"{expect}\"" + BuildArgList(argsCond)
                       + $"}}}},";
            AddCommand(cmd);

            addBlockDelegate();

            cmd = $"{{\"f\":\"EndIf\",\"a\":{{\"cond\":\"{cond}\"}}}},";
            AddCommand(cmd);
        }

        /// <summary>
        /// add a while block to the command stream the builder is building. the block is delimited by "While" and
        /// "EndWhile" commands with the AddBlockCommandsDelegate emitting the commands within the block that
        /// are exectued while the condition matches the expect value. the loop will exit with an error if more than
        /// the timeOut number of iterations are encountered.
        /// 
        /// NOTE: nested while blocks are assumed to have unique cond values.
        /// </summary>
        protected void AddWhileBlock(string cond, bool expect, List<string> argsCond,
                                     AddBlockCommandsDelegate addBlockDelegate, int timeOut = 0)
        {
            string cmd = $"{{\"f\":\"While\",\"a\":{{\"cond\":\"{cond}\",\"expt\":\"{expect}\"";
            if (timeOut != 0)
            {
                cmd += $",\"tout\":\"{timeOut}\"";
            }
            cmd += BuildArgList(argsCond) + $"}}}},";
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
        /// adjust a string by removing all separator characters. returns adjusted value.
        /// </summary>
        protected static string AdjustNoSeparatorsFloatSafe(string s)
        {
            return s.Replace(",", "").Replace("°", "").Replace("’", "").Replace("”", "").Replace("\"", "")
                    .Replace("'", "").Replace(":", "");
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

        // ------------------------------------------------------------------------------------------------------------
        //
        // other builder utilities
        //
        // ------------------------------------------------------------------------------------------------------------

        /// A "wraparound setting" is a setting where a single button moves through the possible values, cycling back to
        /// the first option after the last.

        /// <summary>
        /// When values are in the same order as in the jet (e.g. 0 represents the first option, 1 the second, etc.) this 
        /// function returns the number of button presses necessary to get to desiredVal, starting from currentVal, where
        /// there are maxVal total options.
        /// </summary>
        protected static int GetNumClicksForWraparoundSetting(int currentVal, int desiredVal, int numOptions)
        {
            int clicks = desiredVal - currentVal;
            if (clicks < 0)
                clicks = numOptions - currentVal + desiredVal;
            return clicks;
        }

        /// <summary>
        /// When values are in the same order as in the jet (e.g. 0 represents the first option, 1 the second, etc.) and 
        /// the provided zero-based enum has the same number of options as the jet, this function will return the number
        /// of button presses necessary to get to desiredVal, starting from currentVal.
        /// </summary>
        protected static int GetNumClicksForWraparoundSetting<T>(int currentVal, int desiredVal) where T : Enum
        {
            int numOptions = Enum.GetValues(typeof(T)).Length;
            return GetNumClicksForWraparoundSetting(currentVal, desiredVal, numOptions);
        }
    }
}
