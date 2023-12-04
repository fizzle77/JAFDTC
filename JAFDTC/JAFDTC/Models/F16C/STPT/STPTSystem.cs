// ********************************************************************************************************************
//
// STPTSystem.cs -- f-16c steerpoint system configuration
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
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using JAFDTC.Models.Base;

namespace JAFDTC.Models.F16C.STPT
{
    /// <summary>
    /// TODO: document
    /// </summary>
    public class STPTSystem : NavpointSystemBase<SteerpointInfo>
    {
        public const string SystemTag = "JAFDTC:F16C:STPT";
        public const string STPTListTag = $"{SystemTag}:LIST";

#if NOPE
        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // ---- following properties post change and validation events.

        public ObservableCollection<SteerpointInfo> Steerpoints { get; set; }

        // ---- following properties are synthesized.

        // returns true if the instance indicates a default setup (all fields are ""), false otherwise.
        //
        [JsonIgnore]
        public bool IsDefault => (Steerpoints.Count == 0);
#endif

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        public STPTSystem() => (Points) = (new ObservableCollection<SteerpointInfo>());

        public STPTSystem(STPTSystem other) => (Points) = (new ObservableCollection<SteerpointInfo>(other.Points));

        public virtual object Clone() => new STPTSystem(this);

        // ------------------------------------------------------------------------------------------------------------
        //
        // methods
        //
        // ------------------------------------------------------------------------------------------------------------

#if NOPE
        // reset the instance to defaults by removing all steerpoints.
        //
        public void Reset()
        {
            Steerpoints.Clear();
        }
#endif

        // TODO: document
        public void CleanUp()
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i].CleanUp();
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // steerpoint management
        //
        // ------------------------------------------------------------------------------------------------------------

#if NOPE
        // returns the serialized form of the steerpoints currently in the system, null on error.
        //
        public string SerializeSteerpoints()
        {
            return JsonSerializer.Serialize(Steerpoints, Configuration.JsonOptions);
        }

        // deserialize an array of steerpoints and incorporate them into the steerpoint list. the deserialized
        // steerpoints can either replace the existing steerpoints or be appended to the end of the steerpoint list.
        // returns true on success, false on error.
        //
        public bool DeserializeSteerpoints(string json, bool isReplace = true)
        {
            ObservableCollection<SteerpointInfo> prevStpts = Steerpoints;
            try
            {
                ObservableCollection<SteerpointInfo> newStpts;
                newStpts = JsonSerializer.Deserialize<ObservableCollection<SteerpointInfo>>(json);
                if (isReplace)
                {
                    Steerpoints.Clear();
                }
                foreach (SteerpointInfo stpt in newStpts)
                {
                    Add(stpt);
                }
                return true;
            }
            catch
            {
                Steerpoints = prevStpts;
            }
            return false;
        }

        // returns number of steerpoints.
        //
        [JsonIgnore]
        public int Count { get => Steerpoints.Count; }

        // returns index of given steerpoint.
        //
        public int IndexOf(SteerpointInfo stpt)
        {
            return Steerpoints.IndexOf(stpt);
        }
#endif

        // create a new steerpoint or take an existing steerpoint and add it to the end of the steerpoint list. the
        // steerpoint is numbered to follow the last steerpoint in the list. returns the steerpoint added, null on
        // error.
        //
        public override SteerpointInfo Add(SteerpointInfo stpt = null)
        {
            stpt ??= new();
            stpt.Number = (Points.Count == 0) ? 1 : Points[^1].Number + 1;
            stpt.Name = (string.IsNullOrEmpty(stpt.Name)) ? $"SP{stpt.Number}" : stpt.Name;
            return base.Add(stpt);

#if NOPE
            stpt.Number = (Steerpoints.Count == 0) ? 1 : Steerpoints[^1].Number + 1;
            stpt.Name = (string.IsNullOrEmpty(stpt.Name)) ? $"SP{stpt.Number}" : stpt.Name;
            Steerpoints.Add(stpt);
            return stpt;
#endif
        }

#if NOPE
        // remove the given steerpoint from the list of steerpoints.
        //
        public void Delete(SteerpointInfo stpt)
        {
            Steerpoints.Remove(stpt);
        }
#endif
    }
}
