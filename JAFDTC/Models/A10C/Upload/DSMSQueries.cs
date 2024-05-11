using JAFDTC.Models.DCS;
using System.Collections.Generic;
using System.Text;

namespace JAFDTC.Models.A10C.Upload
{
    internal partial class DSMSBuilder : A10CBuilderBase, IBuilder
    {
        /// <summary>
        /// Retrieves A-10 weapon loadout from the DSMS INV page.
        /// </summary>
        private class LoadoutQueryBuilder : QueryBuilderBase, IBuilder
        {
            private Dictionary<int, string> _stationMunitionMap;

            // lazy load and cache the query data
            public Dictionary<int, string> StationMunitionMap
            {
                get
                {
                    if (_stationMunitionMap == null)
                    {
                        _stationMunitionMap = new Dictionary<int, string>(11);
                        Build();
                        string queryResponse = Query();
                        string[] keyVals = queryResponse.Split(';');
                        foreach (string keyVal in keyVals)
                        {
                            string[] kv = keyVal.Split("=");
                            if (kv.Length == 2)
                            {
                                string store = kv[1] == "---" ? null : kv[1];
                                _stationMunitionMap.Add(int.Parse(kv[0]), store);
                            }
                        }
                    }

                    return _stationMunitionMap;
                }
            }

            public LoadoutQueryBuilder(IAirframeDeviceManager adm, StringBuilder sb, string query, List<string> argsQuery)
                : base(adm, sb, query, argsQuery) { }

            public override void Build()
            {
                AirframeDevice lmfd = _aircraft.GetDevice("LMFD");
                AddActions(lmfd, new() { "LMFD_14", "LMFD_05" }, null, WAIT_BASE); // Go to DSMS INV

                // adds query from constructor
                base.Build();
            }
        }

        /// <summary>
        /// Retrieves the A-10's weapon profiles from the DSMS profile page.
        /// </summary>
        private class DSMSProfileQueryBuilder : QueryBuilderBase, IBuilder
        {
            private Dictionary<string, int> _munitionProfileMap { set; get; }

            // lazy load and cache the query data
            public Dictionary<string, int> MunitionProfileMap
            {
                get
                {
                    if (_munitionProfileMap == null)
                    {
                        _munitionProfileMap = new Dictionary<string, int>();
                        Build();
                        string[] keyVals = Query().Split(';');
                        foreach (string keyVal in keyVals)
                        {
                            string[] kv = keyVal.Split("=");
                            if (kv.Length == 2)
                                _munitionProfileMap.Add(A10CMunition.GetInvKeyFromDefaultProfileName(kv[1]), int.Parse(kv[0]) - 1);
                        }
                    }

                    return _munitionProfileMap;
                }
            }

            public DSMSProfileQueryBuilder(IAirframeDeviceManager adm, StringBuilder sb, string query, List<string> argsQuery)
                : base(adm, sb, query, argsQuery) { }

            public override void Build()
            {
                AirframeDevice lmfd = _aircraft.GetDevice("LMFD");
                AddActions(lmfd, new() { "LMFD_14", "LMFD_01" }, null, WAIT_BASE); // Go to DSMS Profile

                // adds query from constructor
                base.Build();
            }
        }
    }
}
