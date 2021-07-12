using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using ExitGames.Client.Photon;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public enum GamePhase
    {
        Ended, Entered, Started, Result
    }

    public class GameBaseHost : MonoBehaviourPunCallbacks
    {
        public IUIGame ui;
        public GameBaseClient client;
        public Transform AIControllerContainer;

        protected RoomPropEnum_Env env;
        protected RoomPropEnum_Mode mode;
        protected int numPlayers { get{return NetworkManager.pidPlayerDict.Count;}}
        protected int timeLimit;

        private float timeStart;
        protected float timeStarted { get {return Time.realtimeSinceStartup - timeStart;} }
        protected GamePhase phase = GamePhase.Ended;


        protected List<Vector3Int> homePoses = new List<Vector3Int>();

        protected Dictionary<ActualPlayerID, byte> pidCubeDict = new Dictionary<ActualPlayerID, byte>();
        protected Dictionary<byte, ActualPlayerID> cubePidDict = new Dictionary<byte, ActualPlayerID>();
        protected List<uint> usedStandardIDs = new List<uint>();
        protected Dictionary<byte, GameCubeStatus> cubeStatusDict = new Dictionary<byte, GameCubeStatus>();
        private Dictionary<byte, IEnumerator> cubeIEStatusDict = new Dictionary<byte, IEnumerator>();
        protected Dictionary<ActualPlayerID, Vector2Int> pidCommandDict = new Dictionary<ActualPlayerID, Vector2Int>();


        private Dictionary<IController, ActualPlayerID> conPidDict = new Dictionary<IController, ActualPlayerID>();
        private Dictionary<ActualPlayerID, IController> pidConDict = new Dictionary<ActualPlayerID, IController>();

        protected Dictionary<ActualPlayerID, Vector3Int> poses = new Dictionary<ActualPlayerID, Vector3Int>();
        protected float castPoseInterval = 0.1f;
        protected float castPoseLastTime = 0;



        protected virtual void Update() {
            // Cast Cube Poses
            if (phase == GamePhase.Entered || phase == GamePhase.Started)
            {
                var now = Time.realtimeSinceStartup;
                if (now - castPoseLastTime > castPoseInterval)
                {
                    foreach (var pid in poses.Keys)
                    {
                        if (poses[pid] != Vector3Int.zero)
                        {
                            CastCubePose(pid, poses[pid].x, poses[pid].y, poses[pid].z);
                            castPoseLastTime = now;
                        }
                    }
                }
            }
        }



        #region ====== AI Controller ======
        private Observation MakeObservation(ActualPlayerID pid, byte[] occupancy)
        {
            var obs = new Observation();
            obs.teamIdx = NetworkManager.pidTeamIdxDict[pid];
            obs.occupancy = occupancy;
            obs.pose = DuelCubeManager.Ins.GetPose(pidCubeDict[pid]);

            Dictionary<byte, List<Vector3Int>> teamPoses = new Dictionary<byte, List<Vector3Int>>();
            foreach (var t in NetworkManager.teamPidsDict.Keys)
                teamPoses.Add(t, new List<Vector3Int>());
            foreach (var p in pidCubeDict.Keys)
            {
                if (p == pid) continue;
                var t = NetworkManager.pidTeamIdxDict[p];
                teamPoses[t].Add(DuelCubeManager.Ins.GetPose(pidCubeDict[p]));
            }
            obs.teamPoses = teamPoses;
            return obs;
        }

        private void InitAI()
        {
            ClearAI();
            var ais = AIControllerContainer.GetComponents<IController>();

            for (int l=1; l<4; l++)
            {
                ActualPlayerID pid = ActualPlayerID.Local(l);
                if (NetworkManager.pidPlayerDict.ContainsKey(pid))
                {
                    var ai = ais.Length > l-1? ais[l-1] : null;
                    conPidDict.Add(ai, pid);
                    pidConDict.Add(pid, ai);
                    if (ai != null)
                    {
                        if (ai.RequestObservation)
                            ai.SetObservationAsker(AIObservationAsker);
                        ai.SetCommandTeller(AICommandTeller);
                    }
                }
            }
        }

        private Observation AIObservationAsker(IController con)
        {
            if (phase == GamePhase.Started)
            {
                var pid = conPidDict[con];
                var occupancy = con.RequestOccupancy? client.GetTeamOccupancyMap() : null;

                return MakeObservation(pid, occupancy);
            }
            return null;
        }
        private void AICommandTeller(IController con, int uL, int uR)
        {
            if (phase == GamePhase.Started)
            {
                var pid = conPidDict[con];
                Receive_CubeCommand(pid, uL, uR);
            }
        }

        private void ClearAI()
        {
            conPidDict.Clear(); pidConDict.Clear();
            var ais = AIControllerContainer.GetComponents<IController>();
            foreach (var ai in ais)
                ai.Clear();
        }

        #endregion



        #region ====== Game Control ======

        public virtual void EnterGame()
        {
            if (!IsMasterClient) return;
            phase = GamePhase.Entered;

            StartCoroutine(IE_Init());
        }
        public virtual void StopGame()
        {
            if (phase == GamePhase.Ended) return;
            if (phase == GamePhase.Entered || phase == GamePhase.Started) CastStop();
            Clear();

            if (IsMasterClient)
                PhotonNetwork.CurrentRoom.IsVisible = true;
        }

        /// <summary>
        /// Called by Game Program to normally end a game and cast results
        /// </summary>
        protected void EndGame()
        {
            if (phase != GamePhase.Started) return;
            phase = GamePhase.Result;

            StopAllCoroutines();

            DuelCubeManager.Ins.ReleaseCubes();

            CastEnd();
            CastResult();
        }

        public virtual void Clear()
        {
            StopAllCoroutines();
            phase = GamePhase.Ended;

            DuelCubeManager.Ins.ReleaseCubes();
            pidCubeDict.Clear();
            cubePidDict.Clear();
            usedStandardIDs.Clear();
            pidCommandDict.Clear();
            cubeStatusDict.Clear();
            cubeIEStatusDict.Clear();
            ClearAI();
        }

        #endregion ====== Control ======



        #region ====== Game Flow ======

        private IEnumerator IE_Init()
        {
            yield return 0;
            Init();
        }
        protected virtual void Init()
        {
            // Get Room Props
            mode = GetRoomMode();
            env = GetRoomEnv();
            timeLimit = GetRoomTime();

            // Init AIs
            InitAI();

            InitHomePoses();

            poses.Clear();
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
                poses.Add(pid, Vector3Int.zero);

            // Cubes
            DuelCubeManager.Ins.isReal = env == RoomPropEnum_Env.Real;
            DuelCubeManager.Ins.RequestCubes(numPlayers);
            DuelCubeManager.Ins.SetIDCallback(IDCallback);
            DuelCubeManager.Ins.SetStandardIDCallback(StandardIDCallback);

            // Assign
            byte i = 0;
            foreach (var pid in NetworkManager.pidPlayerDict.Keys)
            {
                pidCubeDict.Add(pid, i);
                cubePidDict.Add(i, pid);
                pidCommandDict.Add(pid, Vector2Int.zero);
                i++;
            }

            StartCoroutine(IE_DelayedInit());
        }

        private IEnumerator IE_DelayedInit()
        {
            yield return new WaitUntil( () => (DuelCubeManager.Ins.NumCubes >= numPlayers) );
            // yield return new WaitForSeconds(1);
            DelayedInit();
        }
        protected virtual void DelayedInit()
        {
            // Request for initial positions
            DuelCubeManager.Ins.RequestCallback();

            // MoveHOme
            DuelCubeManager.Ins.MoveHome(homePoses.ToArray());

            // Start Countdown
            StartCoroutine(IE_CountdownToStart());
        }

        private IEnumerator IE_CountdownToStart(byte sec = 3)
        {
            for (byte i=sec; i>0; i--)
            {
                CastToAllEvent(GameEventCode.StartCount, new object[]{ (byte)i });
                yield return new WaitForSecondsRealtime(1);
            }
            CastToAllEvent(GameEventCode.StartCount, new object[]{ (byte)0 });  // Start Signal

            phase = GamePhase.Started;
            timeStart = Time.realtimeSinceStartup;

            StartCoroutine(IE_TimeLeft((byte)timeLimit, (byte)timeLimit));
        }

        private IEnumerator IE_TimeLeft(byte sec, byte interval=1)
        {
            for (int t=sec; t>0; t-=interval)
            {
                CastToAllEvent(GameEventCode.TimeLeft, new object[]{ (byte)t });
                yield return new WaitForSecondsRealtime(interval);
            }
            CastToAllEvent(GameEventCode.TimeLeft, new object[]{ (byte)0 });

            EndGame();
        }

        #endregion


        protected virtual void InitHomePoses()
        {
            homePoses.Clear();
            homePoses.Add(new Vector3Int(121, 165, 90));
            homePoses.Add(new Vector3Int(370, 165, 90));
            homePoses.Add(new Vector3Int(121, 322, 270));
            homePoses.Add(new Vector3Int(370, 322, 270));
        }

        protected virtual void CastResult() {}


        #region ====== Cube Status ======
        private void SetStatus(byte cubeIdx, GameCubeStatus status)
        {
            // Overwrite current status coroutine
            if (cubeIEStatusDict.ContainsKey(cubeIdx) && cubeIEStatusDict[cubeIdx] != null)
                StopCoroutine(cubeIEStatusDict[cubeIdx]);

            // Create coroutine
            var ie = IE_SetStatus(cubeIdx, status);
            if (cubeIEStatusDict.ContainsKey(cubeIdx)) cubeIEStatusDict[cubeIdx] = ie;
            else cubeIEStatusDict.Add(cubeIdx, ie);

            // Start coroutine
            StartCoroutine(ie);
        }
        private IEnumerator IE_SetStatus(byte cubeIdx, GameCubeStatus status)
        {
            // Set status
            if (!cubeStatusDict.ContainsKey(cubeIdx)) cubeStatusDict.Add(cubeIdx, GameCubeStatus.Normal);
            cubeStatusDict[cubeIdx] = status;

            // Cast status
            ActualPlayerID pid = cubePidDict[cubeIdx];
            CastToAllEvent(GameEventCode.CubeStatus, new object[]{pid.ActorNumber, pid.LocalNumber, (int)status});
            // End if normal
            if (status == GameCubeStatus.Normal) yield break;

            // Loop update during status
            for (int i=0; i<6; i++)
            {
                StatusUpdate(cubeIdx, status, i*0.5f);
                yield return new WaitForSeconds(0.5f);
            }
            StatusUpdate(cubeIdx, status, 6*0.5f);

            // End status
            CastToAllEvent(GameEventCode.CubeStatus, new object[]{pid.ActorNumber, pid.LocalNumber, (int)GameCubeStatus.Normal});
            // Back to normal
            cubeStatusDict[cubeIdx] = GameCubeStatus.Normal;
        }
        protected virtual void StatusUpdate(byte cubeIdx, GameCubeStatus status, float time)
        {
            if (status == GameCubeStatus.Stagger)
            {
                var pid = cubePidDict[cubeIdx];
                var (l, r) = GetBuffedCommand(pid);
                MoveCube(cubeIdx, l, r);
            }
            else if (status == GameCubeStatus.FreezeOthers)
            {
                if (time < 0.5f)
                foreach (var c in cubePidDict.Keys)
                {
                    if (c != cubeIdx)
                    {
                        MoveCube(c, 0, 0); // Freeze
                    }
                }
            }
            else
            {
                if (time < 0.5f)
                {
                    var pid = cubePidDict[cubeIdx];
                    var (l, r) = GetBuffedCommand(pid);
                    MoveCube(cubeIdx, l, r);
                }
            }
        }

        protected virtual GameCubeStatus StandardID2Status(uint standardID)
        {
            switch (standardID)
            {
                case 3670320: case 3670066: return GameCubeStatus.SpeedUp;
                case 3670321: case 3670030: return GameCubeStatus.SpeedDown;
                case 3670322: case 3670032: return GameCubeStatus.Reverse;
                case 3670323: case 3670068: return GameCubeStatus.Stagger;
                case 3670324: case 3670034: return GameCubeStatus.FreezeOthers;
                default: return GameCubeStatus.Normal;
            }
        }

        protected virtual (int, int) GetBuffedCommand(ActualPlayerID pid)
        {
            byte cubeIdx = pidCubeDict[pid];
            GameCubeStatus status = cubeStatusDict.ContainsKey(cubeIdx)? cubeStatusDict[cubeIdx] : GameCubeStatus.Normal;

            // If Freezed
            bool freeze = false;
            foreach (var c in cubeStatusDict.Keys)
            {
                if (c != cubeIdx && cubeStatusDict[c] == GameCubeStatus.FreezeOthers)
                {
                    freeze = true; break;
                }
            }
            if (freeze) return (0, 0);

            // Other status
            int uL = pidCommandDict[pid].x; int uR = pidCommandDict[pid].y;
            switch (status)
            {
                case GameCubeStatus.SpeedUp : return ( (int)(uL * 1.5f), (int)(uR * 1.5f) );
                case GameCubeStatus.SpeedDown : return ( (int)(uL * 0.5f), (int)(uR * 0.5f) );
                case GameCubeStatus.Reverse : return ( uR, uL );
                case GameCubeStatus.Stagger : return ( uL + (int)Random.Range(-20, 20), uR + (int)Random.Range(-20, 20) );
            }

            return ( uL, uR );
        }

        protected void MoveCube(byte idx, int uL, int uR)
        {
            DuelCubeManager.Ins.Move(idx, uL, uR);
        }

        #endregion

        #region ====== PUN Events ======

        protected virtual void OnEvent(EventData photonEvent)
        {
            if (phase == GamePhase.Ended || phase == GamePhase.Result) return;

            GameEventCode code = (GameEventCode)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            if (code == GameEventCode.CubeCommand)
            {
                (ActualPlayerID pid, int uL, int uR) = ParseCubeCommand(data);
                Receive_CubeCommand(pid, uL, uR);
            }
            else if (code == GameEventCode.PlayerInfoToHost)
            {
                Receive_PlayerInfoToHost(data);
            }
        }

        private void Receive_CubeCommand(ActualPlayerID pid, int uL, int uR)
        {
            if (phase != GamePhase.Started) return;

            pidCommandDict[pid] = new Vector2Int(uL, uR);
            var (l, r) = GetBuffedCommand(pid);
            MoveCube(pidCubeDict[pid], l, r);
        }
        protected virtual void Receive_PlayerInfoToHost(object[] data) {}

        #endregion



        #region  ====== Cube Events ======

        protected void IDCallback(byte cubeIdx, int x, int y, int deg)
        {
            if (!cubePidDict.ContainsKey(cubeIdx)) return;
            poses[cubePidDict[cubeIdx]] = new Vector3Int(x, y, deg);
        }

        protected void StandardIDCallback(byte cubeIdx, uint id)
        {
            if (!cubePidDict.ContainsKey(cubeIdx)) return;
            if (phase != GamePhase.Started) return;

            if (usedStandardIDs.Contains(id)) return;
            usedStandardIDs.Add(id);

            var status = StandardID2Status(id);
            SetStatus(cubeIdx, status);
        }

        #endregion



        public override void OnEnable()
        {
            base.OnEnable();
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }
        public override void OnDisable()
        {
            base.OnDisable();
            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }

    }


}

