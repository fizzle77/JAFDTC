// ********************************************************************************************************************
//
// PointOfInterestDbase.cs -- point of interest "database" model
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

using JAFDTC.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace JAFDTC.Models.DCS
{
    /// <summary>
    /// flags to control paramters of a query in the point of interest database via Find().
    /// </summary>
    [Flags]
    public enum PointOfInterestDbQueryFlags
    {
        NONE = 0,                                               // no flags
        NAME_PARTIAL_MATCH = 1 << 0,                            // allow partial match of name
        TAGS_ANY_MATCH = 1 << 1,                                // allow at least one tag match
        TAG_PARTIAL_MATCH = 1 << 2,                             // allow partial match of a tag
    }

    // ================================================================================================================

    /// <summary>
    /// parameters for a query of the point of interest database via Find(). for a poi to match a query,
    /// the following must hold:
    /// 
    ///     1) query.Types contains poi.Type
    ///     2) query.Theater matches poi.Theater exactly
    ///     3) query.Campaign matches poi.Campaign exactly
    ///     4) query.Name matches poi.Name per query.Flags, given poi.Name "abcdef"
    ///             NAME_PARTIAL_MATCH => Name "bcd" matches
    ///            !NAME_PARTIAL_MATCH => Name "bcd" does not match
    ///     4) query.Tags matches poi.Tags per query.Flags, given poi.Tags "aa ; bb"
    ///             TAGS_ANY_MATCH => to match, at least one tag in query.Tags must match a tag in poi.Tags
    ///            !TAGS_ANY_MATCH => to match, all tags in query.Tags must match a tag in poi.Tags
    ///             TAG_PARTIAL_MATCH => allows partial tag matches, "a" matches "aa"
    ///            !TAG_PARTIAL_MATCH => disallows partial tag matches, "a" does not match "aa"
    ///
    /// string comparisons are always case-insensitive.
    /// </summary>
    internal class PointOfInterestDbQuery
    {
        public readonly PointOfInterestTypeMask Types;          // types of points of interest to search

        public readonly string Theater;                         // theater (null => match any)

        public readonly string Campaign;                        // campaign name (null => match any)

        public readonly string Name;                            // name (null => match any)

        public readonly string Tags;                            // tags (";"-separated list, null => match any)

        public readonly PointOfInterestDbQueryFlags Flags;      // query flags

        public PointOfInterestDbQuery(PointOfInterestTypeMask types = PointOfInterestTypeMask.ANY, string theater = null,
                                      string campaign = null, string name = null, string tags = null,
                                      PointOfInterestDbQueryFlags flags = PointOfInterestDbQueryFlags.NONE)
            => (Types, Theater, Campaign, Name, Tags, Flags) = (types, theater, campaign, name, tags, flags);
    }

    // ================================================================================================================

    /// <summary>
    /// point of interest (poi) database holds information (PointOfInterest instances) known to jafdtc. the database
    /// class is a singleton that supports find operations to query the known pois. the database is built from fixed
    /// dcs pois (such as airfields) as well as user-defined pois.
    /// </summary>
    internal class PointOfInterestDbase
    {
        // ------------------------------------------------------------------------------------------------------------
        //
        // singleton
        //
        // ------------------------------------------------------------------------------------------------------------

        private static readonly Lazy<PointOfInterestDbase> lazy = new(() => new PointOfInterestDbase());

        public static PointOfInterestDbase Instance { get => lazy.Value; }

        // ------------------------------------------------------------------------------------------------------------
        //
        // properties
        //
        // ------------------------------------------------------------------------------------------------------------

        // the point of interest database is a dictionary that maps PointOfInterestType keys (the primary key) to
        // dictionary values. the dictionary value maps string keys (the auxiliary key) onto a list of
        // PointOfInterest. the auxiliary key is either a campaign name (for CAMPAIGN types) or theater names (for
        // all other types).
        //
        // note that auxiliary keys are case-insensitive. callers are responsible for managing capitalization.

        private readonly Dictionary<PointOfInterestType, Dictionary<string, List<PointOfInterest>>> _dbase;

        /// <summary>
        /// return a list of the names of known campaigns.
        /// </summary>
        public List<string> KnownCampaigns => ((_dbase != null) && _dbase.ContainsKey(PointOfInterestType.CAMPAIGN))
                                              ? _dbase[PointOfInterestType.CAMPAIGN].Keys.ToList<string>() : new();

        /// <summary>
        /// return a list of strings for all of the theaters represented in the database.
        /// 
        /// TODO: consider allowing user-defined theaters.
        /// </summary>
        public static List<string> KnownTheaters => new()
        {
            "Afghanistan",
            "Caucasus",
            "Iraq",
            "Kola",
            "Marianas",
            "Nevada",
            "Persian Gulf",
            "Sinai",
            "South Atlantic",
            "Syria",
            "Other"
        };

        // ------------------------------------------------------------------------------------------------------------
        //
        // construction
        //
        // ------------------------------------------------------------------------------------------------------------

        private PointOfInterestDbase()
        {
            _dbase = new();
            Reset();
        }

        // ------------------------------------------------------------------------------------------------------------
        //
        // functions
        //
        // ------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// return the auxiliary key (key for the second level dictionary) for a point of interest.
        /// </summary>
        private static string AuxKey(PointOfInterest poi)
            => (poi.Type == PointOfInterestType.CAMPAIGN) ? poi.Campaign : poi.Theater;

        /// <summary>
        /// return list of points of interest containing all points of interest that match the specified query
        /// criteria: theater name, tags, type, and poi name. using the default values for these parameters
        /// matches "any". database seraches are always case insensitive. results are optionally sorted using
        /// SortPoIs().
        /// </summary>
        public List<PointOfInterest> Find(PointOfInterestDbQuery query, bool isSorted = false)
        {
            string theater = query.Theater?.ToLower();
            string campaign = query.Campaign?.ToLower();
            string tags = query.Tags?.ToLower();
            string name = query.Name?.ToLower();
            PointOfInterestDbQueryFlags flags = query.Flags;
            PointOfInterestTypeMask types = query.Types;

            bool isNamePartial = flags.HasFlag(PointOfInterestDbQueryFlags.NAME_PARTIAL_MATCH);
            bool isTagsAny = flags.HasFlag(PointOfInterestDbQueryFlags.TAGS_ANY_MATCH);
            bool isTagsPartial = flags.HasFlag(PointOfInterestDbQueryFlags.TAG_PARTIAL_MATCH);

            List<PointOfInterest> results = new();
            foreach (KeyValuePair<PointOfInterestType, Dictionary<string, List<PointOfInterest>>> kvpMain in _dbase)
            {
                if (!types.HasFlag((PointOfInterestTypeMask)(1 << (int)kvpMain.Key)))
                    continue;

                foreach (KeyValuePair<string, List<PointOfInterest>> kvpAux in kvpMain.Value)
                {
                    foreach (PointOfInterest poi in kvpAux.Value)
                    {
                        string poiName = poi.Name.ToLower();

                        if ((!string.IsNullOrEmpty(theater) && (theater != poi.Theater.ToLower())) ||
                            (!string.IsNullOrEmpty(name) && ((!isNamePartial && (name != poiName)) ||
                                                             (isNamePartial && !poiName.Contains(name)))) ||
                            (!string.IsNullOrEmpty(campaign) && (campaign != poi.Campaign.ToLower())))
                        {
                            continue;
                        }

                        bool isMatch = true;
                        if (!string.IsNullOrEmpty(tags))
                        {
                            List<string> tagVals = tags.Split(';').ToList<string>();
                            List<string> poiTagVals = (string.IsNullOrEmpty(poi.Tags))
                                ? new() : poi.Tags.ToLower().Split(';').ToList<string>();

                            if (isTagsAny)
                            {
                                isMatch = false;
                                foreach (string tagVal in tagVals)
                                {
                                    foreach (string poiTagVal in poiTagVals)
                                    {
                                        if ((isTagsPartial && (tagVal.Trim().Contains(poiTagVal.Trim()))) ||
                                            (tagVal.Trim() == poiTagVal.Trim()))
                                        {
                                            isMatch = true;
                                            break;
                                        }
                                    }
                                    if (isMatch)
                                        break;
                                }
                            }
                            else
                            {
                                isMatch = true;
                                foreach (string tagVal in tagVals)
                                {
                                    bool isFound = false;
                                    foreach (string poiTagVal in poiTagVals)
                                    {
                                        if ((isTagsPartial && (tagVal.Trim().Contains(poiTagVal.Trim()))) ||
                                            (tagVal.Trim() == poiTagVal.Trim()))
                                        {
                                            isFound = true;
                                            break;
                                        }
                                    }
                                    if (!isFound)
                                    {
                                        isMatch = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if (isMatch)
                            results.Add(poi);
                    }
                }
            }
            return  (isSorted) ? SortPoIs(results) : results;
        }

        /// <summary>
        /// sort the poi list by theater, type, campaign, then name. returns the sorted list.
        /// </summary>
        public static List<PointOfInterest> SortPoIs(List<PointOfInterest> pois)
        {
            pois.Sort((a, b) =>
            {
                int cmp = a.Theater.CompareTo(b.Theater);
                if (cmp != 0)
                    return cmp;
                else if (a.Type != b.Type)
                {
                    if (a.Type == PointOfInterestType.USER)
                        return -1;
                    else if (a.Type == PointOfInterestType.DCS_CORE)
                        return 1;
                    else if (b.Type == PointOfInterestType.USER)
                        return 1;
                    return -1;
                }
                else if (a.Type == PointOfInterestType.CAMPAIGN)
                {
                    cmp = a.Campaign.CompareTo(b.Campaign);
                    if (cmp != 0)
                        return cmp;
                }
                return a.Name.CompareTo(b.Name);
            });
            return pois;
        }

        /// <summary>
        /// parse a multi-line comma-separated value string to build a list of points of interest.
        /// </summary>
        public static List<PointOfInterest> ParseCSV(string csv)
        {
            // TODO: this needs to change to csv...
            List<PointOfInterest> pois = new();
            string[] lines = (string.IsNullOrEmpty(csv)) ? Array.Empty<string>() : csv.Replace("\r", "").Split('\n');
            foreach (string line in lines)
            {
                PointOfInterest poi = new PointOfInterest(line);
                if (poi.Type != PointOfInterestType.UNKNOWN)
                    pois.Add(poi);
            }
            return pois;
        }

        /// <summary>
        /// reset the database by clearing its current contents and reloading from storage.
        /// </summary>
        public void Reset()
        {
            _dbase.Clear();

            List<PointOfInterest> pois = FileManager.LoadPointsOfInterest();
            foreach (PointOfInterest poi in pois)
                AddPointOfInterest(poi, false);
        }

        /// <summary>
        /// add a campaign to the database if one with a matching name is not already present, persisting the database
        /// to storage if requested.
        /// </summary>
        public void AddCampaign(string campaign, bool isPersist = true)
        {
            if (!_dbase.ContainsKey(PointOfInterestType.CAMPAIGN))
                _dbase[PointOfInterestType.CAMPAIGN] = new Dictionary<string, List<PointOfInterest>>();
            if (!_dbase[PointOfInterestType.CAMPAIGN].ContainsKey(campaign))
                _dbase[PointOfInterestType.CAMPAIGN][campaign] = new List<PointOfInterest>();

            if (isPersist)
                Save(campaign);
        }

        /// <summary>
        /// add a campaign to the database if one with a matching name is not already present, persisting the database
        /// to storage if requested.
        /// </summary>
        public void DeleteCampaign(string campaign, bool isPersist = true)
        {
            PointOfInterestType type = PointOfInterestType.CAMPAIGN;
            if (_dbase.ContainsKey(type) && _dbase[type].ContainsKey(campaign))
                _dbase[type].Remove(campaign);

            if (isPersist)
                Save(campaign, true);
        }

        /// <summary>
        /// return the number of points of interest in a campaign.
        /// </summary>
        public int CountPoIInCampaign(string campaign)
        {
            PointOfInterestType type = PointOfInterestType.CAMPAIGN;
            return (_dbase.ContainsKey(type) && _dbase[type].ContainsKey(campaign)) ? _dbase[type][campaign].Count : 0;
        }

        /// <summary>
        /// add a point of interest to the database, persisting the database to storage if requested.
        /// </summary>
        public void AddPointOfInterest(PointOfInterest poi, bool isPersist = true)
        {
            string auxKey = AuxKey(poi);

            if (!_dbase.ContainsKey(poi.Type))
                _dbase[poi.Type] = new Dictionary<string, List<PointOfInterest>>();
            if (!_dbase[poi.Type].ContainsKey(auxKey))
                _dbase[poi.Type][auxKey] = new List<PointOfInterest>();
            _dbase[poi.Type][auxKey].Add(poi);

            if (isPersist)
                Save(poi.Campaign);
        }

        /// <summary>
        /// remove a point of interest from the database, persisting the database to storage if requested.
        /// </summary>
        public void RemovePointOfInterest(PointOfInterest poi, bool isPersist = true)
        {
            string campaign = poi.Campaign;
            _dbase[poi.Type][AuxKey(poi)].Remove(poi);

            if (isPersist)
                Save(campaign);
        }

        /// <summary>
        /// persist points of interest to storage. a null campaign persists the user pois, a non-null campaign persists
        /// the pois for the specified campaign. the isCullCampaign parameter controls whether or not empty campaigns
        /// are removed from storage.
        /// </summary>
        public bool Save(string campaign = null, bool isCullEmptyCampaign = false)
        {
            PointOfInterestDbQuery query;
            if (string.IsNullOrEmpty(campaign))
            {
                query = new PointOfInterestDbQuery(PointOfInterestTypeMask.USER);
                return FileManager.SaveUserPointsOfInterest(Find(query));
            }
            else
            {
                query = new PointOfInterestDbQuery(PointOfInterestTypeMask.CAMPAIGN, null, campaign);
                List<PointOfInterest> pois = Find(query);
                if (isCullEmptyCampaign && (pois.Count == 0))
                    FileManager.DeleteCampaignPointsOfInterest(campaign);
                return (isCullEmptyCampaign) ? true : FileManager.SaveCampaignPointsOfInterest(campaign, pois);
            }
        }
    }
}
