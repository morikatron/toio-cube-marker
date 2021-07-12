using UnityEngine;
using System.Collections.Generic;
using System.Linq;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class GameBattleClient : GameBaseClient
    {

        public IUIGameBattle uib;

        private List<float> teamsOccupancy = new List<float>();

        private float statUpdateInterval = 0.5f;
        private float statUpdateLastTime = 0;


        protected override void Update()
        {
            base.Update();

            if (phase == GamePhase.Started)
            {
                var now = Time.realtimeSinceStartup;
                if (now - statUpdateLastTime > statUpdateInterval)
                {
                    StatUpdate();
                    statUpdateLastTime = now;
                }
            }
        }

        protected override void Init()
        {
            base.Init();

            InitStats();
        }


        protected void InitStats()
        {
            // Reset teamsOccupancy
            teamsOccupancy.Clear();
            for (int i=0; i<4; i++) teamsOccupancy.Add(0);

            // Init UI Stats
            uib.ShowNStat(NetworkManager.teamPidsDict.Count);

            int istat = 0;
            foreach (var teamIdx in NetworkManager.teamPidsDict.Keys)
                uib.SetStat(istat++, teamIdx, 0);

        }

        protected void StatUpdate()
        {
            // Calc. team occupancy
            teamsOccupancy[0] = 0; teamsOccupancy[1] = 0; teamsOccupancy[2] = 0; teamsOccupancy[3] = 0;

            var markerIdxRatioDict = ui.GetMarkerIdxRatioDict();
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
            {
                var markerIdx = pidMarkerDict[pid];
                float ratio = markerIdxRatioDict[markerIdx];
                var teamIdx = NetworkManager.pidTeamIdxDict[pid];
                teamsOccupancy[teamIdx] += ratio;
            }

            var order = teamsOccupancy
                .Select((x, i) => new KeyValuePair<float, int>(x, i))
                .OrderBy(x => -x.Key)
                .ToList().Select(x => x.Value).ToList();

            for (int i = 0; i < NetworkManager.teamPidsDict.Count; i++)
            {
                byte teamIdx = (byte)order[i];
                float ratio = teamsOccupancy[teamIdx];

                // Set UI
                uib.SetStat(i, teamIdx, ratio);
            }
        }


        protected override void Receive_Result(object[] data)
        {
            // Parse
            Dictionary<ActualPlayerID, float> pidRatioDict = ParseResult(data);

            uib.ShowResult(pidRatioDict);
        }


    }

}
