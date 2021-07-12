using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using HashTable = ExitGames.Client.Photon.Hashtable;


namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        #region Singleton
        private static NetworkManager ins = null;
        public static NetworkManager Ins { get {
            if (ins == null) ins = new NetworkManager();
            return ins;
        } }
        private NetworkManager() {}

        void Awake()
        {
            NetworkManager.ins = this;
        }
        #endregion



        public static Dictionary<string, RoomInfo> roomDict = new Dictionary<string, RoomInfo>();
        public static Dictionary<ActualPlayerID, byte> pidTeamIdxDict = new Dictionary<ActualPlayerID, byte>();
        public static Dictionary<byte, List<ActualPlayerID>> teamPidsDict = new Dictionary<byte, List<ActualPlayerID>>();
        public static Dictionary<ActualPlayerID, Player> pidPlayerDict = new Dictionary<ActualPlayerID, Player>();


        public static bool isJoiningRoom { get; private set; }
        public static string roomName { get { return PhotonNetwork.CurrentRoom!=null? PhotonNetwork.CurrentRoom.Name : null;}}

        public static ActualPlayerID localPid { get { return ActualPlayerID.Local(); }}
        public static byte localTeamIdx { get { return pidTeamIdxDict.ContainsKey(localPid)? pidTeamIdxDict[localPid] : (byte)255; } }
        public static bool ready { get { return IsAcutalPlayerReady(localPid); } }
        public static int pidCount { get { return pidPlayerDict.Count; } }



        public static bool IsRoomNameAvailable(string name)
        {
            name = name.Trim();
            return !roomDict.ContainsKey(name) && name.Length > 0;
        }
        public static bool HasActualPlayer(ActualPlayerID pid)
        {
            return pidPlayerDict.ContainsKey(pid);
        }

        public static List<byte> GetTeamIdxs()
        {
            return new List<byte>(teamPidsDict.Keys);
        }



        #region Functions

        public static bool JoinRoom(string name)
        {
            if (isJoiningRoom)
            {
                Debug.LogWarning("Try to join room ["+ name + "] while already joining another room.");
                return false;
            }
            else
            {
                Debug.Log("Man: JoinRoom " + name);
                isJoiningRoom = true;
                PhotonNetwork.JoinRoom(name);
                return true;
            }
        }

        public static void CreateRoom(
            string name, RoomPropEnum_Env env, RoomPropEnum_Mode mode, string quizSetting, byte maxPlayers, byte time=60
        ){
            RoomOptions option = new RoomOptions(){
                PublishUserId = true,
                MaxPlayers = maxPlayers,
                CustomRoomProperties = new HashTable()
                {
                    {RoomPropKey_Env, env},
                    {RoomPropKey_Mode, mode},
                    {RoomPropKey_QuizSetting, quizSetting},
                    {RoomPropKey_Time, time},
                    {RoomPropKey_AcutalPlayerCount, 1}
                },
                CustomRoomPropertiesForLobby = new string[] {
                    RoomPropKey_Env, RoomPropKey_Mode, RoomPropKey_AcutalPlayerCount
                }
            };

            isJoiningRoom = true;
            PhotonNetwork.CreateRoom(name, option, default);
        }
        public static void CreateRoom(string name, int env, int mode, string quizSetting, int maxPlayers, int time)
        { CreateRoom(name, (RoomPropEnum_Env)env, (RoomPropEnum_Mode)mode, quizSetting, (byte)maxPlayers, (byte)time); }


        public static void SetTeam(ActualPlayerID pid, byte teamIdx)
        {
            SetActualPlayerTeamIdx(pid, (byte)teamIdx);
        }
        public static bool JoinTeam(byte teamIdx)
        {
            if (!IsMasterClient && ready) return false;
            SetActualPlayerTeamIdx(localPid, (byte)teamIdx);
            return true;
        }

        public static string GetAcutalPlayerName(ActualPlayerID pid)
        {
            if (!pidPlayerDict.ContainsKey(pid)) return null;
            var p = pidPlayerDict[pid];
            if (pid.LocalNumber == 0) return p.NickName;
            return "AI-" + pid.LocalNumber;
        }
        public static bool IsAcutalPlayerReady(ActualPlayerID pid)
        {
            if (!pidPlayerDict.ContainsKey(pid)) return false;
            var p = pidPlayerDict[pid];
            return pid.LocalNumber == 0 ? GetPlayerReady(p) : true;
        }
        public static bool IsAcutalPlayerHost(ActualPlayerID pid)
        {
            if (!pidPlayerDict.ContainsKey(pid)) return false;
            var p = pidPlayerDict[pid];
            return pid.LocalNumber == 0 ? p.IsMasterClient : false;
        }

        #endregion




        #region PUN Events

        protected virtual void OnEvent(EventData photonEvent)
        {
            GameEventCode code = (GameEventCode)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;
            if (code == GameEventCode.EnterGame)
            {
            }
            else if (code == GameEventCode.SetTeam)
            {
                (ActualPlayerID pid, byte teamIdx) = ParseSetTeam(data);
                OnSetTeam(pid, teamIdx);
            }
            else if (code == GameEventCode.Kick)
            {
                ActualPlayerID pid = ParseKick(data);
                RemovePid(pid);
            }
        }


        public override void OnConnectedToMaster()
        {
            Debug.Log("Man: OnConnectedToMaster()");
            PhotonNetwork.JoinLobby();

        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarningFormat("Man: OnDisconnected() with reason {0}", cause);
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("Man: OnJoinedLobby");

            roomDict.Clear();
        }


        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log("Man: OnRoomListUpdate");

            // Cache room list
            foreach (var room in roomList)
            {
                // Remove
                if (!room.IsOpen || !room.IsVisible || room.RemovedFromList)
                {
                    if (roomDict.ContainsKey(room.Name))
                    {
                        roomDict.Remove(room.Name);
                        Debug.Log("Remove room " + room.Name);
                    }
                }
                // Update
                else if (roomDict.ContainsKey(room.Name))
                {
                    roomDict[room.Name] = room;
                    Debug.Log("Update room " + room.Name);
                }
                // Add
                else
                {
                    roomDict.Add(room.Name, room);
                    Debug.Log("Add room " + room.Name);
                }
            }

        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.Log("Man: OnCreateRoomFailed");

            isJoiningRoom = false;
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Man: OnJoinedRoom()");

            isJoiningRoom = false;
            roomDict.Clear();

            // Get existed players
            foreach (var p in PlayerList)
                pidPlayerDict.Add(new ActualPlayerID(p), p);

            // Set team if host
            if (IsMasterClient) SetTeam(localPid, 0);

            // Set initial ready status
            SetPlayerReady(IsMasterClient);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.Log("Man: OnJoinRoomFailed: " + message);

            isJoiningRoom = false;
        }

        public override void OnPlayerEnteredRoom(Player p)
        {
            Debug.Log("Man: OnPlayerEnteredRoom");

            pidPlayerDict.Add(new ActualPlayerID(p.ActorNumber), p);

            // Set Team
            if (IsMasterClient)
            {
                foreach (var pid in pidTeamIdxDict.Keys)
                {
                    SetTeam(pid, pidTeamIdxDict[pid]);
                }
                var newpid = new ActualPlayerID(p.ActorNumber);
                byte t = 0;
                var mode = GetRoomMode();
                if (mode == RoomPropEnum_Mode.Quiz)
                {
                    if (!teamPidsDict.ContainsKey(TeamIdx_Erazer)) t = TeamIdx_Erazer;
                    else if (!teamPidsDict.ContainsKey(TeamIdx_Painter)) t = TeamIdx_Painter;
                    else if (teamPidsDict[TeamIdx_Erazer].Count < teamPidsDict[TeamIdx_Painter].Count)
                        t = TeamIdx_Erazer;
                    else t = TeamIdx_Painter;
                }
                else
                {
                    for (byte i=0; i<4; i++)
                        if (!teamPidsDict.ContainsKey(i))
                        { t = i; break; }
                }
                SetTeam(newpid, t);
            }
        }

        protected void OnSetTeam(ActualPlayerID pid, byte teamIdx)
        {
            Debug.Log("Man: OnSetTeam " + pid + " team " + teamIdx);

            // Update pidPlayerDict
            if (!pidPlayerDict.ContainsKey(pid))
                pidPlayerDict.Add(pid, pidPlayerDict[new ActualPlayerID(pid.ActorNumber)] );

            // Update pidTeamIdxDict, teamPidsDict
            if (!teamPidsDict.ContainsKey(teamIdx)) teamPidsDict.Add(teamIdx, new List<ActualPlayerID>());
            if (!pidTeamIdxDict.ContainsKey(pid))
            {
                pidTeamIdxDict.Add(pid, teamIdx);
                teamPidsDict[teamIdx].Add(pid);
            }
            else
            {
                var oldIdx = pidTeamIdxDict[pid];
                teamPidsDict[oldIdx].Remove(pid);
                teamPidsDict[teamIdx].Add(pid);
                if (teamPidsDict[oldIdx].Count == 0) teamPidsDict.Remove(oldIdx);

                pidTeamIdxDict[pid] = teamIdx;
            }

            // Update room prop
            SetRoomAcutalPlayers(pidCount);
        }

        private void OnKick(ActualPlayerID pid)
        {
            Debug.Log("Man: OnKick " + pid);

            RemovePid(pid);

            // Update room prop
            SetRoomAcutalPlayers(pidCount);
        }

        public override void OnPlayerLeftRoom(Player p)
        {
            Debug.Log("Man: OnPlayerLeftRoom");

            List<ActualPlayerID> todel = new List<ActualPlayerID>();
            foreach (var pid in pidPlayerDict.Keys)
                if (pid.ActorNumber == p.ActorNumber) todel.Add(pid);

            foreach (var pid in todel) RemovePid(pid);

            // Update room prop
            SetRoomAcutalPlayers(pidCount);
        }

        public override void OnLeftRoom()
        {
            Debug.Log("Man: OnLeftRoom()");

            pidTeamIdxDict.Clear();
            teamPidsDict.Clear();
            pidPlayerDict.Clear();
        }


        #endregion



        private void RemovePid(ActualPlayerID pid)
        {
            pidPlayerDict.Remove(pid);

            var teamIdx = pidTeamIdxDict[pid];
            teamPidsDict[teamIdx].Remove(pid);
            if (teamPidsDict[teamIdx].Count == 0) teamPidsDict.Remove(teamIdx);

            pidTeamIdxDict.Remove(pid);
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
