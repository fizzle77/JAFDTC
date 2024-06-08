// ********************************************************************************************************************
//
// A10CMunition.cs -- Properties of A-10C weapons, hydrated from FileManager.LoadA10Munitions().
//
// Copyright(C) 2021-2023 the-paid-actor & others
// Copyright(C) 2023 ilominar/raven
// Copyright(C) 2024 fizzle
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
using System.Collections.Generic;

namespace JAFDTC.Models.A10C
{
    public sealed class A10CMunition
    {
        //
        // properties deserialized from DB JSON
        //
        public int ID { get; set; } // ID used in configuration files. Must be unique.
        public string Name { get; set; } // name as it will appear in UI.
        public string Profile { get; set; } // name of the weapon's default profile
        public string DescrUI { get; set; } // descriptive text for the user interface
        public string Image { get; set; } // name of the weapon's image file

        // values to enable/disable settings UI
        public bool CCIP { get; set; }
        public bool CCRP { get; set; }

        public bool EscMnvr { get; set; }
        
        public string LaserButton { get; set; } // The MFD button to set the laser code varies by weapon
        public bool AutoLase { get; set; }
        
        public bool Pairs { get; set; }
        public bool Ripple { get; set; }
        public bool RipFt { get; set; }
        
        public bool HOF { get; set; }
        public bool RPM { get; set; }
        
        public bool Fuze { get; set; }

        public List<string> INV_Keys { get; set; } // identifiers for this weapon on DSMS INV page
        
        //
        // synthesized properties
        //
        public bool Laser => !string.IsNullOrEmpty(LaserButton);
        public string ImageFullPath => "/Images/" + Image;
        public bool SingleReleaseOnly => !Pairs && !Ripple;

        //
        // static members
        //

        // Munitions DB
        private static List<A10CMunition> _list;
        public static List<A10CMunition> GetMunitions()
        {
            if (_list == null)
                _list = FileManager.LoadA10Munitions();
            return _list;
        }

        private static List<A10CMunition> _profileList;
        public static List<A10CMunition> GetUniqueProfileMunitions()
        {
            if (_profileList == null)
            {
                _profileList = GetMunitions();
                // HACK: duplicate maverick profiles don't  matter, so we remove the duplicate entry.
                // They don't matter because the Maverick has no real profile settings.
                A10CMunition toRemove = null;
                foreach (A10CMunition m in _profileList)
                {
                    if (m.ID == 13)
                    {
                        toRemove = m;
                        break;
                    }
                }
                if (toRemove != null)
                    _profileList.Remove(toRemove);
            }
            return _profileList;
        }

        // INV_Key to Munition Map
        private static Dictionary<string, A10CMunition> _keyMunitionMap;
        public static A10CMunition GetMunitionFromInvKey(string key)
        {
            if (_keyMunitionMap == null)
            {
                _keyMunitionMap = new Dictionary<string, A10CMunition>();
                foreach (A10CMunition munition in GetMunitions())
                {
                    foreach (string k in munition.INV_Keys)
                        _keyMunitionMap.Add(k, munition);
                }
            }
            return _keyMunitionMap[key];
        }

        // ID to Munition Map
        private static Dictionary<int, A10CMunition> _idMunitionMap;
        public static A10CMunition GetMunitionFromID(int ID)
        {
            if (_idMunitionMap == null)
            {
                _idMunitionMap = new Dictionary<int, A10CMunition>();
                foreach (A10CMunition munition in GetMunitions())
                    _idMunitionMap.Add(munition.ID, munition);
            }
            return _idMunitionMap[ID];
        }

        // Profile to Munition Map
        private static Dictionary<string, A10CMunition> _profileMunitionMap;
        public static A10CMunition GetMunitionFromProfile(string profile)
        {
            if (_profileMunitionMap == null)
            {
                _profileMunitionMap = new Dictionary<string, A10CMunition>();
                foreach (A10CMunition munition in GetUniqueProfileMunitions())
                {
                    _profileMunitionMap.Add(munition.Profile, munition);
                }
            }
            if (_profileMunitionMap.TryGetValue(profile, out A10CMunition m))
                return m;
            return null;
        }
    }
}
