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
                        // 1=GBU-54;2=AGM-65D;...
                        string[] keyVals = Query().Split(';');
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
            private Dictionary<int, int> _munitionProfileIndexMap { set; get; }

            private List<string > _profiles;
            public List<string> Profiles
            {
                get 
                {
                    if (_profiles == null)
                        DoQuery();
                    return _profiles;
                }
            }

            /// <summary>
            /// In the returned dictionary, keys are munition IDs and values are the 0-based index in the jet's default profiles.
            /// </summary>
            public Dictionary<int, int> MunitionProfileIndexMap
            {
                get
                {
                    if (_munitionProfileIndexMap == null)
                        DoQuery();
                    return _munitionProfileIndexMap;
                }
            }

            // lazy load and cache the query data
            private void DoQuery()
            {
                _munitionProfileIndexMap = new Dictionary<int, int>();
                _profiles = new List<string>();
                Build();
                // 1=WPNS OFF;2=GBU-54;3=MAVERICK;...
                string[] keyVals = Query().Split(';');
                foreach (string keyVal in keyVals)
                {
                    string[] kv = keyVal.Split("=");
                    if (kv.Length == 2)
                    {
                        _profiles.Add(kv[1]);

                        A10CMunition m = A10CMunition.GetMunitionFromProfile(kv[1]);
                        if (m != null)
                            _munitionProfileIndexMap.Add(m.ID, int.Parse(kv[0]) - 1);
                    }
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
