using System.Collections.Generic;
using System.Linq;


namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class GameQuizDiffHost : GameQuizHost
    {
        protected override void InitQuiz()
        {
            // Clear
            answers.Clear();
            answerTimes.Clear();
            teamIdxTexNameDict.Clear();

            // Get textures' names
            var nameURLs = uiq.GetLoadableTextures();

            // Sample
            var teamIdxs = NetworkManager.GetTeamIdxs();
            int n = teamIdxs.Count;
            System.Random rnd = new System.Random();

            if (n > nameURLs.Count) n = nameURLs.Count;

            var sampled = new List<string>( nameURLs.Keys.OrderBy(item => rnd.Next()).Take(n) );
            for (int i=0; i<n; i++)
                teamIdxTexNameDict.Add(teamIdxs[i], sampled[i]);

            // Cast
            CastQuizSetting(nameURLs);
        }

        protected override void EndIfAllAnswered()
        {
            bool isAllAnswered = true;
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
            {
                if (pid.LocalNumber > 0) continue;
                if (!answers.ContainsKey(pid))
                {
                    isAllAnswered = false;
                    break;
                }
            }
            if (isAllAnswered) EndGame();
        }
    }

}
