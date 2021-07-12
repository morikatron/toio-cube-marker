using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using ExitGames.Client.Photon;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class GameBaseClient : MonoBehaviourPunCallbacks
    {
        public IUIGame ui;
        public Transform controllerContainer;
        public byte countdownToQuit = 30;

        protected RoomPropEnum_Env env;
        protected RoomPropEnum_Mode mode;
        protected int maxPlayers;
        protected int numPlayers { get{return NetworkManager.pidPlayerDict.Count;}}
        protected int timeLimit;


        private float timeStart;
        protected float timeStarted { get {return Time.realtimeSinceStartup - timeStart;} }
        protected GamePhase phase = GamePhase.Ended;


        // protected Dictionary<IController, ActualPlayerID> controllerIdDict = new Dictionary<IController, ActualPlayerID>();
        protected Dictionary<ActualPlayerID, Vector3Int> poses = new Dictionary<ActualPlayerID, Vector3Int>();
        protected Dictionary<ActualPlayerID, int> pidMarkerDict = new Dictionary<ActualPlayerID, int>();
        protected Dictionary<int, ActualPlayerID> markerPidDict = new Dictionary<int, ActualPlayerID>();



        protected virtual void Update() {}



        #region ====== Controller ======

        private Observation MakeObservation(ActualPlayerID pid, byte[] occupancy)
        {
            var obs = new Observation();
            obs.teamIdx = NetworkManager.pidTeamIdxDict[pid];
            obs.occupancy = occupancy;
            obs.pose = poses[pid];

            Dictionary<byte, List<Vector3Int>> teamPoses = new Dictionary<byte, List<Vector3Int>>();
            foreach (var t in NetworkManager.teamPidsDict.Keys)
                teamPoses.Add(t, new List<Vector3Int>());
            foreach (var p in pidMarkerDict.Keys)
            {
                if (p == pid) continue;
                var t = NetworkManager.pidTeamIdxDict[p];
                teamPoses[t].Add(poses[p]);
            }
            obs.teamPoses = teamPoses;
            return obs;
        }

        private void InitControllers()
        {
            var cons = controllerContainer.GetComponents<IController>();

            foreach (var con in cons)
            {
                if (con.RequestObservation)
                    con.SetObservationAsker(ConObservationAsker);
                con.SetCommandTeller(ConCommandTeller);
            }

        }

        private Observation ConObservationAsker(IController con)
        {
            if (phase == GamePhase.Started)
            {
                var pid = ActualPlayerID.Local();
                var occupancy = con.RequestOccupancy? GetTeamOccupancyMap() : null;

                return MakeObservation(pid, occupancy);
            }
            return null;
        }
        private void ConCommandTeller(IController con, int uL, int uR)
        {
            if (phase == GamePhase.Started)
            {
                var pid = ActualPlayerID.Local();
                SendCubeCommand(pid, uL, uR);
            }
        }

        private void ClearControllers()
        {
            var cons = controllerContainer.GetComponents<IController>();
            foreach (var con in cons)
                con.Clear();
        }

        #endregion



        #region ====== Game Control ======

        public virtual void EnterGame()
        {
            phase = GamePhase.Entered;
            StartCoroutine(IE_Init());
        }

        protected virtual void StartGame()
        {
            ui.SetDrawable(true);
        }

        public virtual void StopGame()
        {
            EndGame();
        }

        /// <summary>
        /// Called by Game Program to normally end a game and cast results
        /// </summary>
        protected void EndGame()
        {
            if (phase == GamePhase.Ended) return;

            Clear();
            ui?.Back();
        }

        public virtual void Clear()
        {
            StopAllCoroutines();
            phase = GamePhase.Ended;

            pidMarkerDict.Clear();
            markerPidDict.Clear();
            poses.Clear();
            ClearControllers();
        }

        #endregion



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
            maxPlayers = CurrentRoom.MaxPlayers;

            InitControllers();
            InitMarkers();

            StartCoroutine(IE_DelayedInit());
        }

        private IEnumerator IE_DelayedInit()
        {
            yield return new WaitUntil( () => (DuelCubeManager.Ins.NumCubes == numPlayers) );
            // yield return new WaitForSeconds(1);
            DelayedInit();
        }
        protected virtual void DelayedInit() {}

        private IEnumerator IE_CountdownToQuit(byte sec)
        {
            for (byte t = sec; t > 0; t--)
            {
                ui.CountdownToQuit(t);
                yield return new WaitForSecondsRealtime(1);
            }

            EndGame();
        }

        #endregion



        #region ====== PUN Events ======

        protected virtual void OnEvent(EventData photonEvent)
        {
            if (phase == GamePhase.Ended) return;
            GameEventCode code = (GameEventCode)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;

            if (code == GameEventCode.StartCount)
            {
                byte countdown = (byte)data[0];
                Receive_CountdownToStart(countdown);
            }
            else if (code == GameEventCode.TimeLeft)
            {
                byte t = (byte)data[0];
                Receive_TimeLeft(t);
            }
            else if (code == GameEventCode.CubePose)
            {
                (ActualPlayerID pid, int x, int y, int deg) = ParseCubePose(data);
                Receive_CubePose(pid, x, y, deg);
            }
            else if (code == GameEventCode.CubeStatus)
            {
                (ActualPlayerID pid, GameCubeStatus status) = ParseCubeStatus(data);
                Receive_CubeStatus(pid, status);
            }
            else if (code == GameEventCode.Result)
            {
                Receive_Result(data);
            }
            else if (code == GameEventCode.PlayerInfoToAll)
            {
                Receive_PlayerInfoToAll(data);
            }
            else if (code == GameEventCode.TeamInfoToAll)
            {
                Receive_TeamInfoToAll(data);
            }
            else if (code == GameEventCode.End)
            {
                Receive_End(data);
            }
            else if (code == GameEventCode.Stop)
            {
                Receive_Stop(data);
            }
        }

        private void Receive_CountdownToStart(byte count)
        {
            // Update UI
            ui.Countdown(count, duration:(count == 0 ? 0.5f : 0.75f));

            if (count == 0) // Start
            {
                phase = GamePhase.Started;
                timeStart = Time.realtimeSinceStartup;
                StartGame();
            }
        }

        private void Receive_TimeLeft(byte time)
        {
            ui.StartTimer(time, timeLimit);
        }

        private void Receive_CubePose(ActualPlayerID pid, int x, int y, int deg)
        {
            if (!pidMarkerDict.ContainsKey(pid)) return;
            int i = pidMarkerDict[pid];

            Vector3Int pose = new Vector3Int(x, y, deg);
            if (!poses.ContainsKey(pid)) poses.Add(pid, pose);
            poses[pid] = pose;

            ui.MoveCubeMarker(i, x, y, deg);
        }
        private void Receive_CubeStatus(ActualPlayerID pid, GameCubeStatus status)
        {
            int i = pidMarkerDict[pid];
            ui.SetCubeMarkerStatus(i, status);
        }

        protected virtual void Receive_PlayerInfoToAll(object[] data) {}
        protected virtual void Receive_TeamInfoToAll(object[] data) {}
        protected virtual void Receive_End(object[] data)
        {
            phase = GamePhase.Result;
            ui.SetDrawable(false);
            ui.StopTimer();

            StartCoroutine(IE_CountdownToQuit(countdownToQuit));
        }
        protected virtual void Receive_Stop(object[] data)
        {
            ui.SetDrawable(false);
            ui.StopTimer();

            StopGame();
        }
        protected virtual void Receive_Result(object[] data) {}

        #endregion


        internal Dictionary<ActualPlayerID, float> GetPidRatios()
        {
            var markerIdxRatios = ui.GetMarkerIdxRatioDict();
            Dictionary<ActualPlayerID, float> pidRatios = new Dictionary<ActualPlayerID, float>();
            foreach (var i in markerIdxRatios.Keys)
            {
                var pid = markerPidDict[i];
                pidRatios.Add(pid, markerIdxRatios[i]);
            }
            return pidRatios;
        }

        internal byte[] GetTeamOccupancyMap()
        {
            var markerMap = ui.GetOccupancyMap();
            return Array.ConvertAll(markerMap, markerIdx => markerIdx==(byte)255? markerIdx : NetworkManager.pidTeamIdxDict[markerPidDict[markerIdx]] );
        }


        protected virtual void InitMarkers()
        {
            int i=0;
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
            {
                int teamIdx = NetworkManager.pidTeamIdxDict[pid];
                string name = NetworkManager.GetAcutalPlayerName(pid);
                ui.CreateCubeMarker(teamIdx, name);
                pidMarkerDict.Add(pid, i);
                markerPidDict.Add(i, pid);
                ui.SetCubeMarkerPen(i, TeamColors[teamIdx]);
                i++;
            }
        }

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

