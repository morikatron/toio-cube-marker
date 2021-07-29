using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class UIGameQuizDiff : UIGameQuizBase, IUIGameQuiz
    {
        public Transform resultRank0;
        public Transform resultRank1;
        public Transform resultRank2;
        public Transform resultRank3;



        public override void ShowResult(Dictionary<byte, string> teamIdxTexNameDict, Dictionary<ActualPlayerID, string> pidAnsDict, Dictionary<ActualPlayerID, float> pidTimeDict=null)
        {
            overlayResult.SetActive(true);

            var teamPlayers = NetworkManager.teamPidsDict;

            // Set Rank Num
            int nteams = teamPlayers.Count;
            SetResultRankNum(nteams);

            // Aggregate min correct time for each team
            List<float> teamTimes = new List<float>(new float[]{999, 999, 999, 999});
            foreach (var teamIdx in teamPlayers.Keys)
            {
                if (!teamIdxTexNameDict.ContainsKey(teamIdx)) continue;     // Don't have correct answer because Quiz not enough

                var pids = teamPlayers[teamIdx];
                string correct = teamIdxTexNameDict[teamIdx];

                foreach (var pid in pids)
                {
                    if (!pidAnsDict.ContainsKey(pid)) continue;     // Not answered
                    string ans = pidAnsDict[pid];
                    float t = pidTimeDict[pid];
                    if (ans == correct && t < teamTimes[teamIdx])
                        teamTimes[teamIdx] = t;
                }
            }

            // Sort
            var orderedTeamIdxs = teamTimes
                .Select((x, i) => new KeyValuePair<float, byte>(x, (byte)i))
                .OrderBy(x => x.Key)
                .ToList().Select(x => x.Value).ToList();
            for (byte teamIdx = 0; teamIdx < 4; teamIdx ++)
                if (!teamPlayers.ContainsKey(teamIdx) && orderedTeamIdxs.Contains(teamIdx))
                    orderedTeamIdxs.Remove(teamIdx);

            // Set UI
            for (int irank = 0; irank < nteams; irank++)
            {
                byte teamIdx = orderedTeamIdxs[irank];
                var pids = teamPlayers[teamIdx];
                var rank = GetResultRank(irank);

                // Set Slot Num
                SetResultRankPlayerNum(rank, pids.Count);

                // Set team result
                string correct = teamIdxTexNameDict.ContainsKey(teamIdx)? teamIdxTexNameDict[teamIdx] : null;
                SetResultRankTeam(rank, teamIdx, teamTimes[teamIdx], correct);

                // Set WinnerFrame
                if (irank == 0)
                    overlayResult.transform.Find("WinnerFrame").gameObject.SetActive(teamTimes[teamIdx] < 60);

                // Set slots
                for (int islot = 0; islot < pids.Count; islot++)
                {
                    var pid = pids[islot];
                    string name = NetworkManager.GetAcutalPlayerName(pid);
                    string time = pidTimeDict.ContainsKey(pid)? (int)pidTimeDict[pid] + "s" : "---";
                    string ans = pidAnsDict.ContainsKey(pid)? pidAnsDict[pid] : "--------";
                    SetResultPlayerSlot(rank, islot, name, ans, time);
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
        private void SetResultRankTeam(Transform rank, byte teamIdx, float time, string ans)
        {
            bool noResult = time > 60;

            // Set Team ColorBlock
            rank.Find("TeamTitle").Find("ColorBlock").GetComponent<RawImage>().color = TeamColors[teamIdx];

            // Set Team Time
            string timeStr = noResult? "---" : (int)time + "s";
            rank.Find("TextTeamTime").GetComponent<TMP_Text>().text = timeStr;

            // Set Rank
            var rankTitle = rank.Find("TextRank").GetComponent<TMP_Text>();
            if (noResult)
            {
                rankTitle.text = "Lose";
                rankTitle.color = new Color32(100, 100, 100, 255);
            }
            else switch (rank.gameObject.name)
            {
                case "Rank0" :
                    rankTitle.text = "WIN";
                    rankTitle.color = new Color32(255, 224, 0, 255); break;
                case "Rank1" :
                    rankTitle.text = "2nd";
                    rankTitle.color = new Color32(207, 207, 207, 255); break;
                case "Rank2" :
                    rankTitle.text = "3rd";
                    rankTitle.color = new Color32(166, 118, 41, 255); break;
                case "Rank3" :
                    rankTitle.text = "4th";
                    rankTitle.color = new Color32(78, 120, 105, 255); break;
            }

            // Set Answer Image, Name
            if (ans != null && loadedTextures.ContainsKey(ans))
                rank.GetComponentInChildren<ImageViewer>().SetTexture(loadedTextures[ans]);
            rank.Find("BGAnswer").GetComponentInChildren<TMP_Text>().text = ans==null? "------" : ans;
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

        private void SetResultPlayerSlot(Transform rank, int slotIdx, string name, string ans, string time)
        {
            var slot = GetResultPlayerSlot(rank, slotIdx);
            slot.Find("Name").GetComponent<TMP_Text>().text = name;
            slot.Find("Ans").GetComponent<TMP_Text>().text = ans;
            slot.Find("Time").GetComponent<TMP_Text>().text = time;
        }

        #endregion


    }

}

