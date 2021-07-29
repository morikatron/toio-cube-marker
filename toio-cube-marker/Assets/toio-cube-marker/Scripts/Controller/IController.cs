using System;
using System.Collections.Generic;
using UnityEngine;



namespace CubeMarker
{
    public interface IController
    {
        bool RequestObservation { get; }
        bool RequestOccupancy { get; }

        void SetObservationAsker(Func<IController, Observation> func);
        void SetCommandTeller(Action<IController, int, int> action);

        void Clear();
    }

    public class ControllerBase : MonoBehaviour, IController
    {
        public virtual bool RequestObservation { get {return false;} }
        public virtual bool RequestOccupancy { get {return false;} }
        public float updateInterval = 0.05f;
        public float maxTellInterval = 2f;  // Tell after maxTellInterval even if cmd not updated.
        public int maxTranslate = 60;
        public int maxRotate = 15;

        protected Func<IController, Observation> obsAsker = null;
        protected Action<IController, int, int> cmdTeller = null;

        protected int uL = 0, uR = 0;
        protected float lastUpdateTime = 0;
        protected float lastTellTime = 0;



        protected virtual void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval) return;
            lastUpdateTime = Time.time;

            if (cmdTeller == null) return;

            Observation obs = obsAsker==null? null : obsAsker.Invoke(this);
            bool updated = Run(obs);

            if (updated || Time.realtimeSinceStartup-lastTellTime > maxTellInterval)
            {
                cmdTeller.Invoke(this, uL, uR);
                lastTellTime = Time.realtimeSinceStartup;
            }
        }


        protected virtual bool Run(Observation obs = null)
        {
            return false;
        }


        public void SetObservationAsker(Func<IController, Observation> func) {
            obsAsker = func;
        }
        public void SetCommandTeller(Action<IController, int, int> action) {
            cmdTeller = action;
        }

        public void Clear()
        {
            obsAsker = null; cmdTeller = null;
        }
    }

    public class Observation
    {
        public byte teamIdx;
        // public int pixW, pixH;
        // public int matW, matH;
        public byte[] occupancy;   // size of pixW*pixH, each pixel is occupied by which team (teamIdx)
        public Vector3Int pose;    // self pose {matX, matY, matDeg}
        public Dictionary<byte, List<Vector3Int>> teamPoses; // each team's members' poses (except self)
    }
}
