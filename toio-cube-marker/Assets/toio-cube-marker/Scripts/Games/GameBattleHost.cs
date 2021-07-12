

namespace CubeMarker
{

    public class GameBattleHost : GameBaseHost
    {

        protected override void CastResult()
        {
            var pidRatios = client.GetPidRatios();
            PUNProtocolUtils.CastResult(pidRatios);
        }

    }

}
