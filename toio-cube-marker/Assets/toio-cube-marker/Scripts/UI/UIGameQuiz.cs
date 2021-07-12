using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class UIGameQuiz : UIGameQuizBase, IUIGameQuiz
    {
        public ImageViewer correctImage;
        public TMP_Text correctName;
        public Transform team0slots;
        public Transform team1slots;
        public GameObject win0;
        public GameObject win1
;

        protected bool isEraser { get {return NetworkManager.pidTeamIdxDict[ActualPlayerID.Local()] == TeamIdx_Erazer; } }


        protected override bool IsTeamIdxEraser(byte teamIdx)
        {
            return teamIdx == TeamIdx_Erazer;
        }

        protected override void InitButtonToAnswer()
        {
            base.InitButtonToAnswer();
            buttonToAnswer.gameObject.SetActive(!isEraser);
        }


        public override void ShowResult(Dictionary<byte, string> teamIdxTexNameDict, Dictionary<ActualPlayerID, string> pidAnsDict, Dictionary<ActualPlayerID, float> pidTimeDict=null)
        {
            overlayResult.SetActive(true);

            string correct = teamIdxTexNameDict.ContainsKey(TeamIdx_Painter)? teamIdxTexNameDict[TeamIdx_Painter] : null;

            List<ActualPlayerID> team0pids = new List<ActualPlayerID>();
            List<ActualPlayerID> team1pids = new List<ActualPlayerID>();
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
            {
                if (NetworkManager.pidTeamIdxDict[pid] == TeamIdx_Painter)
                    team0pids.Add(pid);
                else team1pids.Add(pid);
            }
            bool team0win = false;

            // SHow/Hide Player Slots
            SetTeamPlayerNum(team0slots, team0pids.Count);
            SetTeamPlayerNum(team1slots, team1pids.Count);

            // Set Names, Answers
            for (int i = 0; i < team0pids.Count; i++)
            {
                var pid = team0pids[i];

                // Set UI
                string name = NetworkManager.GetAcutalPlayerName(pid);
                string ans = pidAnsDict.ContainsKey(pid)? pidAnsDict[pid] : "------";
                string time = pidTimeDict.ContainsKey(pid)? (int)(pidTimeDict[pid]) + "s" : "---";
                SetTeamPlayer(team0slots, i, name, ans, time);

                // Win or not
                if (pidAnsDict.ContainsKey(pid) && pidAnsDict[pid] == correct)
                    team0win = true;

            }
            for (int i = 0; i < team1pids.Count; i++)
            {
                var pid = team1pids[i];
                string name = NetworkManager.GetAcutalPlayerName(pid);
                SetTeamPlayer(team1slots, i, name, "", "");
            }

            // Set Win
            win0.SetActive(team0win);
            win1.SetActive(!team0win);

            // Set correct answer
            SetCorrectAnswer(correct);
        }



        #region ====== UI Utils ======

        private void SetTeamPlayerNum(Transform team, int num)
        {
            for (int i=0; i<4; i++)
                team.Find("PlayerList").Find("PlayerSlot"+i).gameObject.SetActive(i<num);
        }

        private void SetTeamPlayer(Transform team, int slotIdx, string name, string ans, string time)
        {
            var slot = team.Find("PlayerList").Find("PlayerSlot"+slotIdx);
            slot.Find("Name").GetComponent<TMP_Text>().text = name;

            var ansTr = slot.Find("Ans");
            if (ansTr != null) ansTr.GetComponent<TMP_Text>().text = ans;

            var timeTr = slot.Find("Time");
            if (timeTr != null) timeTr.GetComponent<TMP_Text>().text = time;
        }

        private void SetCorrectAnswer(string ans)
        {
            if (ans != null && loadedTextures.ContainsKey(ans))
            {
                correctImage.SetTexture(loadedTextures[ans]);
                correctName.text = ans;
            }
            else
                correctName.text = "------";
        }

        #endregion


    }

}

