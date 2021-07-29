using System.Collections.Generic;
using UnityEngine;
using System.Linq;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class GameQuizHost : GameBaseHost
    {
        public IUIGameQuiz uiq;

        protected Dictionary<byte, string> teamIdxTexNameDict = new Dictionary<byte, string>();
        protected Dictionary<ActualPlayerID, float> answerTimes = new Dictionary<ActualPlayerID, float>();  // HOST
        protected Dictionary<ActualPlayerID, string> answers = new Dictionary<ActualPlayerID, string>();    // HOST



        protected override void Init()
        {
            base.Init();

            InitQuiz();
        }

        protected virtual void InitQuiz()
        {
            // Clear
            answers.Clear();
            answerTimes.Clear();
            teamIdxTexNameDict.Clear();

            // Get textures' names
            var nameURLs = uiq.GetLoadableTextures();

            // Sample
            int n = 1;
            System.Random rnd = new System.Random();

            if (n > nameURLs.Count) n = nameURLs.Count;

            var sampled = new List<string>( nameURLs.Keys.OrderBy(item => rnd.Next()).Take(n) );
            if (sampled.Count > 0)
                teamIdxTexNameDict.Add(TeamIdx_Painter, sampled[0]);

            // Cast
            CastQuizSetting(nameURLs);
        }

        protected virtual void CastQuizSetting(Dictionary<string, string> nameURLs)
        {
            List<object> content = new List<object>();
            content.Add(teamIdxTexNameDict.Count);
            foreach (var teamIdx in teamIdxTexNameDict.Keys)
            {
                content.Add(teamIdx);
                content.Add(teamIdxTexNameDict[teamIdx]);
                content.Add(nameURLs[teamIdxTexNameDict[teamIdx]]);
            }
            foreach (var name in nameURLs.Keys)
                content.Add(name);
            CastToAllEvent(GameEventCode.TeamInfoToAll, content.ToArray());
        }


        protected override void Receive_PlayerInfoToHost(object[] data)
        {
            if (!IsMasterClient) return;

            ActualPlayerID pid = new ActualPlayerID((int)data[0], (int)data[1]);
            string name = (string) data[2];

            // Assert
            if (answers.ContainsKey(pid))
            {
                Debug.LogWarning("Player " + NetworkManager.pidPlayerDict[pid].NickName + " submitted answer twice!!");
                return;
            }

            // Cast
            CastToAllEvent(GameEventCode.PlayerInfoToAll, data);

            // Save answer
            answers.Add(pid, name);
            answerTimes.Add(pid, timeStarted);

            // End game if All answered
            EndIfAllAnswered();
        }

        protected virtual void EndIfAllAnswered()
        {
            bool isAllAnswered = true;
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
            {
                if (pid.LocalNumber > 0) continue;
                if (NetworkManager.pidTeamIdxDict[pid] != TeamIdx_Erazer)
                {
                    if (!answers.ContainsKey(pid))
                    {
                        isAllAnswered = false;
                        break;
                    }
                }
            }
            if (isAllAnswered) EndGame();
        }


        protected override void CastResult()
        {
            List<object> content = new List<object>();

            // content.Add(teamIdxTexNameDict[TeamIdx_Painter]); // Correct Ans

            foreach (var pid in answers.Keys)
            {
                content.Add(pid.ActorNumber);
                content.Add(pid.LocalNumber);
                content.Add(answers[pid]);
                content.Add(answerTimes[pid]);
            }
            CastToAllEvent(GameEventCode.Result, content.ToArray());
        }

    }

}
