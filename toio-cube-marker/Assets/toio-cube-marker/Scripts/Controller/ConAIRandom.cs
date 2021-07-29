using UnityEngine;



namespace CubeMarker
{

    public class ConAIRandom : ControllerBase, IController
    {
        public override bool RequestObservation { get {return true;} }

        private float tarStartTime = 0;
        private bool goFront = false;

        private float xMin, xMax, yMin, yMax;
        private Vector2 tar = Vector2.zero;

        void Start()
        {
            float margin = 30;
            xMin = Field.matLeft + margin;
            xMax = Field.matLeft + Field.matWidth - margin;
            yMin = Field.matTop + margin;
            yMax = Field.matTop + Field.matHeight - margin;
        }

        protected override bool Run(Observation obs = null)
        {
            if (obs == null) return false;

            // Make Random Target
            Vector2 pos = new Vector2(obs.pose.x, obs.pose.y);
            if (tar == Vector2.zero || (tar-pos).magnitude < 25 || Time.time-tarStartTime > 3+Random.Range(-1,1))
            {
                tar = new Vector2( Random.Range(xMin, xMax), Random.Range(yMin, yMax) );
                tarStartTime = Time.time;
                goFront = !goFront;
                // Debug.LogWarning("new tar = " + tar);
            }
            else
            {
                Vector2 forceAvoid = Vector2.zero;
                foreach (var poses in obs.teamPoses.Values)
                    foreach (var pose in poses)
                    {
                        Vector2 opos = new Vector2(pose.x, pose.y);
                        float dist = (opos - pos).magnitude;
                        if (dist < 35)
                            forceAvoid += pos - opos;
                    }
                if (forceAvoid.magnitude > 0)
                {
                    tar = pos + forceAvoid * 5;
                    tar.x = Mathf.Clamp(tar.x, xMin, xMax);
                    tar.y = Mathf.Clamp(tar.y, yMin, yMax);
                    tarStartTime = Time.time;
                    goFront = Mathf.Abs((Mathf.Atan2((tar-pos).y, (tar-pos).x) *180/Mathf.PI - obs.pose.z +900) %360-180) <= 90;
                }
            }

            // Control
            var dpos = tar - pos;
            var dir = Mathf.Atan2(dpos.y, dpos.x) * 180 / Mathf.PI;
            var orient = goFront? obs.pose.z : obs.pose.z - 180;
            var ddir = (dir - orient + 900) % 360 - 180;    // left = positive

            float steer = (goFront? 1: -1) * Mathf.Sign(ddir) * Mathf.Min( Mathf.Abs(ddir), 90 ) / 90;
            float accel = (goFront? 1: -1) * (1 - Mathf.Abs(steer) * 0.7f);

            // Convert and Output
            float tr = accel * maxTranslate;
            float ro = steer * maxRotate;

            int newL = (int)(tr + ro * Mathf.Sign(tr));
            int newR = (int)(tr - ro * Mathf.Sign(tr));

            if (newL == uL && newR == uR) return false;
            uL = newL; uR = newR; return true;
        }

    }

}
