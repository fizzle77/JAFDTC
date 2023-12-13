// ********************************************************************************************************************
//
// BuilderBase.cs -- base class for command builder
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

using System.Text;

namespace JAFDTC.Models.DCS
{
    public abstract class BuilderBase
    {
        protected readonly IAirframeDeviceManager _aircraft;
        private readonly StringBuilder _sb;

        public BuilderBase(IAirframeDeviceManager aircraft, StringBuilder sb)
        {
            _aircraft = aircraft;
            _sb = sb;
        }

        public abstract void Build();

        protected void AppendCommand(string s)
        {
            _sb.Append(s);
        }

        protected static string BuildDigits(Device d, string s)
        {
            StringBuilder sb = new();
            foreach (var c in s.ToCharArray())
            {
                sb.Append(d.GetCommand(c.ToString()));
            }
            return sb.ToString();
        }

        protected static string BuildAlphaNumString(Device d, string s)
        {
            StringBuilder sb = new();
            foreach (var c in s.ToCharArray())
            {
                sb.Append(d.GetCommand(c.ToString()));
            }
            return sb.ToString();
        }

        protected static string Wait()
        {
            string str = "{'device':'wait', 'delay': 200},";
            return str.Replace("'", "\"");
        }

        protected static string WaitLong()
        {
            string str = "{'device':'wait', 'delay': 600},";
            return str.Replace("'", "\"");
        }

        protected static string WaitVeryLong()
        {
            string str = "{'device':'wait', 'delay': 17000},";
            return str.Replace("'", "\"");
        }

        protected static string Marker(string mark)
        {
            string str = "{'marker': '" + mark + "'},";
            return str.Replace("'", "\"");
        }

        protected static string StartUploadMarker()
        {
            string str = "{'start_upload': '1'},";
            return str.Replace("'", "\"");
        }

        protected static string StartCondition(string condition, params string[] parameters)
        {
            string str = "{'start_condition': '" + condition + "'";
            if (parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    str += ",'param" + (i + 1) + "': '" + parameters[i] + "'";
                }
            }
            str += "},";
            return str.Replace("'", "\"");
        }

        protected static string EndCondition(string condition)
        {
            string str = "{'end_condition': '" + condition + "'},";
            return str.Replace("'", "\"");
        }

        protected static string DeleteLeadingZeros(string s)
        {
            while (s.StartsWith("0"))
            {
                s = s.Remove(0, 1);
            }
            if (s == "") s = "0";
            return s;
        }

        protected static string RemoveSeparators(string s)
        {
            return s.Replace(",", "").Replace(".", "").Replace("°", "").Replace("’", "").Replace("”", "")
                    .Replace("\"", "").Replace("'","").Replace(":", "");
        }
    }
}
