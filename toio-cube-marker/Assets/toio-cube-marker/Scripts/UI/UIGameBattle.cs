using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;


namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class UIGameBattle : MonoBehaviour, IUIGameBattle
    {
        [Header("Game Impl")]
        public GameBattleHost gameHost;
        public GameBattleClient gameClient;

        [Header("UI References")]
        public UIGame ui;
        public GameObject stat0;
        public GameObject stat1;
        public GameObject stat2;
        public GameObject stat3;

        [Header("UI for results")]
        public GameObject overlayResult;
        public Transform resultRank0;
        public Transform resultRank1;
        public Transform resultRank2;
        public Transform resultRank3;



        private bool active = false;
        public void SetActive(bool value)
        {
            if (active == value && this.gameObject.activeSelf == value) return;
            this.gameObject.SetActive(value);
            active = value;

            if (value)
            {
                Clear();

                gameClient.ui = ui;
                gameClient.uib = this;
                gameHost.EnterGame();
                gameClient.EnterGame();
            }
            else
            {
                gameHost.StopGame();
                gameClient.StopGame();
                Clear();
            }
        }

        public void Clear()
        {
            overlayResult.SetActive(false);
        }


        #region Stat

        public void SetStat(int i, byte teamIdx, float ratio)
        {
            SetStatRatio(i, ratio); SetStatColor(i, teamIdx);
        }
        public void ShowNStat(int num)
        {
            for (int i=0; i<4; i++)
                GetStatFromIdx(i).SetActive(num > i);
        }
        private void SetStatRatio(int i, float ratio)
        {
            var statObj = GetStatFromIdx(i);

            // Set Bar
            RectTransform bar = statObj.GetComponentInChildren<RawImage>().transform.GetComponent<RectTransform>();
            bar.sizeDelta = new Vector2(2+148*ratio, bar.sizeDelta.y);

            // Set Text
            string text = ((int)(ratio*100)).ToString() + "%";
            if (text.Length == 2) text = "0" + text;
            statObj.GetComponentInChildren<TMP_Text>().text = text;
        }
        private void SetStatColor(int i, byte teamIdx)
        {
            var statObj = GetStatFromIdx(i);
            statObj.GetComponentInChildren<RawImage>().color = TeamColors[teamIdx];
        }
        protected GameObject GetStatFromIdx(int idx)
        {
            switch (idx)
            {
                case 0: return stat0;
                case 1: return stat1;
                case 2: return stat2;
                case 3: return stat3;
                default: return stat0;
            }
        }

        #endregion



        public void ShowResult(Dictionary<ActualPlayerID, float> pidRatioDict)
        {
            overlayResult.SetActive(true);

            // Init vars
            var teamPidsDict = NetworkManager.teamPidsDict;
            List<float> teamRatios = new List<float>(new float[]{0, 0, 0, 0});

            // Set Rank Num
            int nteams = teamPidsDict.Count;
            SetResultRankNum(nteams);

            // Get team ratios
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
            {
                float ratio = pidRatioDict[pid];
                var teamIdx = NetworkManager.pidTeamIdxDict[pid];
                teamRatios[teamIdx] += ratio;
            }
            // Sort
            var orderedTeamIdxs = teamRatios
                .Select((x, i) => new KeyValuePair<float, byte>(x, (byte)i))
                .OrderBy(x => -x.Key)
                .ToList().Select(x => x.Value).ToList();
            for (byte teamIdx = 0; teamIdx < 4; teamIdx ++)
                if (!teamPidsDict.ContainsKey(teamIdx) && orderedTeamIdxs.Contains(teamIdx))
                    orderedTeamIdxs.Remove(teamIdx);

            // Update UI
            for (int irank = 0; irank < nteams; irank++)
            {
                byte teamIdx = orderedTeamIdxs[irank];
                var pids = teamPidsDict[teamIdx];
                var rank = GetResultRank(irank);

                // Set Slot Num
                SetResultRankPlayerNum(rank, pids.Count);

                // Set team result
                SetResultRankTeam(rank, teamIdx, teamRatios[teamIdx]);

                // Set slots
                for (int islot = 0; islot < pids.Count; islot++)
                {
                    var pid = pids[islot];
                    string name = NetworkManager.GetAcutalPlayerName(pid);
                    SetResultPlayerSlot(rank, islot, name, pidRatioDict[pid]);
                }

            }
        }



        #region ====== UI Utils ======

        private Transform GetResultRank(int idx)
        {
            switch (idx)
            {
                case 1: return resultRank1;
                case 2: return resultRank2;
                case 3: return resultRank3;
                default: case 0: return resultRank0;
            }
        }
        private void SetResultRankNum(int num)
        {
            for (int i = 0; i < 4; i++)
            {
                var rank = GetResultRank(i);
                rank.gameObject.SetActive(i < num);
            }
        }
        private void SetResultRankTeam(Transform rank, byte teamIdx, float ratio)
        {
            // Set Team ColorBlock
            rank.Find("TeamTitle").Find("ColorBlock").GetComponent<RawImage>().color = TeamColors[teamIdx];

            // Set Team Time
            rank.Find("TextTeamRatio").GetComponent<TMP_Text>().text = (int)(ratio*100) + "%";
        }
        private void SetResultRankPlayerNum(Transform rank, int num)
        {
            var list = rank.Find("PlayerList");
            for (int i = 0; i < 4; i++)
            {
                list.Find("PlayerSlot" + i).gameObject.SetActive(i < num);
            }
        }
        private Transform GetResultPlayerSlot(Transform rank, int slotIdx)
        {
            var list = rank.Find("PlayerList");
            return list.Find("PlayerSlot" + slotIdx);
        }

        private void SetResultPlayerSlot(Transform rank, int slotIdx, string name, float ratio)
        {
            var slot = GetResultPlayerSlot(rank, slotIdx);
            slot.Find("Name").GetComponent<TMP_Text>().text = name;
            slot.Find("Ratio").GetComponent<TMP_Text>().text = (int)(ratio*100) + "%";
        }
        #endregion

    }

}

