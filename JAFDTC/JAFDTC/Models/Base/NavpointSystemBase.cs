// ********************************************************************************************************************
//
// NavpointSystemBase.cs -- navigation point system abstract base class
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

using JAFDTC.Utilities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace JAFDTC.Models.Base
{
    /// <summary>
    /// abstract base class for a navigation point system (such as steerpoints or waypoints), system consists of an
    /// array of navigation points of type T (where T should be conform to INavpointInfo and is typically derived
    /// from the NavpointInfoBase abstract base class).
    /// </summary>
    public abstract class NavpointSystemBase<T> : BindableObject, ISystem
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- INotifyPropertyChanged properties

        public virtual ObservableCollection<T> Points { get; set; }

        // ---- synthesized properties

        /// <summary>
        /// returns true if the instance indicates a default setup (no navpoints are defined), false otherwise.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault => (Points.Count == 0);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// reset the navpoint system to defaults by removing all navpoints.
        /// </summary>
        public virtual void Reset()
        {
            Points.Clear();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // steerpoint management
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// returns the serialized form of the navpoint currently in the system, null on error.
        /// </summary>
        public virtual string SerializeNavpoints()
        {
            return JsonSerializer.Serialize(Points, Configuration.JsonOptions);
        }

        /// <summary>
        /// deserialize an array of navpoint and incorporate them into the navpoint list. the deserialized navpoints
        /// can either replace the existing navpoints or be appended to the end of the navpoint list. returns true on
        /// success, false on error.
        /// </summary>
        public virtual bool DeserializeNavpoints(string json, bool isReplace = true)
        {
            ObservableCollection<T> prevStpts = Points;
            try
            {
                ObservableCollection<T> newStpts;
                newStpts = JsonSerializer.Deserialize<ObservableCollection<T>>(json);
                if (isReplace)
                {
                    Points.Clear();
                }
                foreach (T navpt in newStpts)
                {
                    Add(navpt);
                }
                return true;
            }
            catch
            {
                Points = prevStpts;
            }
            return false;
        }

        /// <summary>
        /// returns the number of navpoints in the system.
        /// </summary>
        [JsonIgnore]
        public int Count { get => Points.Count; }

        /// <summary>
        /// returns index of given navpoint.
        /// </summary>
        public virtual int IndexOf(T navpt)
        {
            return Points.IndexOf(navpt);
        }

        /// <summary>
        /// add an existing navpoint to the end of the navpoint list. returns the navpoint added.
        /// </summary>
        public virtual T Add(T navpt)
        {
            Points.Add(navpt);
            return navpt;
        }

        /// <summary>
        /// remove the given navpoint from the list of navpoints.
        /// </summary>
        public virtual void Delete(T navpt)
        {
            Points.Remove(navpt);
        }
    }
}
