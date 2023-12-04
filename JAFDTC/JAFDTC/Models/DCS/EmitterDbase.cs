// ********************************************************************************************************************
//
// EmitterDbase.cs -- emitter dabase
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

using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace JAFDTC.Models.DCS
{
    internal class EmitterDbase
    {
        private static readonly Lazy<EmitterDbase> lazy = new(() => new EmitterDbase());
        public static EmitterDbase Instance { get => lazy.Value; }

        private Dictionary<int, List<Emitter>> Dbase { get; set; }

        // TODO: may want to add ability to load other files?
        private EmitterDbase()
        {
            Emitter[] emitters = FileManager.LoadEmitters();

            Dbase = new Dictionary<int, List<Emitter>>();
            foreach (Emitter emitter in emitters)
            {
                if (!Dbase.ContainsKey(emitter.ALICCode))
                {
                    Dbase[emitter.ALICCode] = new List<Emitter>();
                }
                Dbase[emitter.ALICCode].Add(emitter);
            }
        }

        public List<Emitter> Find(int alicCode = -1)
        {
            List<Emitter> list;
            if (alicCode < 0)
            {
                list = new List<Emitter>();
                foreach (List<Emitter> emitterList in Dbase.Values)
                {
                    list = list.Concat(emitterList).ToList();
                }
            }
            else
            {
                list = (Dbase.ContainsKey(alicCode)) ? Dbase[alicCode] : new List<Emitter>();
            }
            return list;
        }
    }
}
