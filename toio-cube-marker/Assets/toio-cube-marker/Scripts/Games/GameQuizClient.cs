using System.Collections.Generic;


namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class GameQuizClient : GameBaseClient
    {
        internal IUIGameQuiz uiq;

        protected Dictionary<byte, string> teamIdxTexNameDict = new Dictionary<byte, string>();


        protected void SendAnswer(string name, int localNumber=0)
        {
            ActualPlayerID pid = ActualPlayerID.Local(localNumber);
            object[] content = new object[]{pid.ActorNumber, pid.LocalNumber, name};
            CastToHostEvent(GameEventCode.PlayerInfoToHost, content);
        }


        protected override void Receive_TeamInfoToAll(object[] data)
        {
            teamIdxTexNameDict.Clear();

            List<string> names = new List<string>();
            Dictionary<string, string> nameURLs = new Dictionary<string, string>();
            Dictionary<int, string> cubeIdxNames = new Dictionary<int, string>();

            int painters = (int)data[0];

            // Parse names
            for (int i=painters*3+1; i < data.Length; i++)
                names.Add( (string)data[i] );

            // Parse nameURLs
            for (int i=1; i < painters*3+1; i+=3)
            {
                teamIdxTexNameDict.Add((byte)data[i], (string)data[i+1]);
                nameURLs.Add((string)data[i+1], (string)data[i+2]);
            }

            // Parse cubeIdxNames
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
            {
                byte teamIdx = NetworkManager.pidTeamIdxDict[pid];
                if (teamIdxTexNameDict.ContainsKey(teamIdx))
                {
                    var cubeIdx = pidMarkerDict[pid];
                    var name = teamIdxTexNameDict[teamIdx];
                    cubeIdxNames.Add(cubeIdx, name);
                }
            }

            uiq.SetupQuiz(names, nameURLs, cubeIdxNames);
        }

        protected override void Receive_PlayerInfoToAll(object[] data)
        {
            // Parse Answer
            ActualPlayerID pid = new ActualPlayerID((int)data[0], (int)data[1]);
            string name = (string) data[2];

            // Update UI
            uiq.SetAnswerSlotAnswer(pid, name.Replace(".", "\n"));
        }

        protected override void StartGame()
        {
            base.StartGame();
            uiq.SetAnswerable(true);
            uiq.SetAnswerCallback(AnswerCallback);
        }

        protected virtual void AnswerCallback(string ans)
        {
            SendAnswer(ans);
        }


        protected override void Receive_Result(object[] data)
        {
            Dictionary<ActualPlayerID, string> answers = new Dictionary<ActualPlayerID, string>();
            Dictionary<ActualPlayerID, float> answerTimes = new Dictionary<ActualPlayerID, float>();

            for (int i=0; i<data.Length; i+=4)
            {
                ActualPlayerID pid = new ActualPlayerID((int)data[i], (int)data[i+1]);
                string ans = (string) data[i+2];
                float t = (float) data[i+3];
                answers.Add(pid, ans);
                answerTimes.Add(pid, t);
            }

            uiq.ShowResult(teamIdxTexNameDict, answers, answerTimes);
        }

    }

}
