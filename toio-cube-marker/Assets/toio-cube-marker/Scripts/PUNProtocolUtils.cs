using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using HashTable = ExitGames.Client.Photon.Hashtable;


namespace CubeMarker
{

    public static class PUNProtocolUtils
    {
        public static readonly Color TeamColor_Red = new Color(229f/255, 83f/255, 83f/255);
        public static readonly Color TeamColor_Blue = new Color(83f/255, 124f/255, 229f/255);
        public static readonly Color TeamColor_Green = new Color(88f/255, 178f/255, 94f/255);
        public static readonly Color TeamColor_Yellow = new Color(224f/255, 188f/255, 0f/255);
        public static readonly Color[] TeamColors = {TeamColor_Red, TeamColor_Blue, TeamColor_Green, TeamColor_Yellow};
        public static readonly byte TeamIdx_Red = 0;
        public static readonly byte TeamIdx_Blue = 1;
        public static readonly byte TeamIdx_Green = 2;
        public static readonly byte TeamIdx_Yellow = 3;
        public static readonly byte TeamIdx_Painter = 0;    // Quiz
        public static readonly byte TeamIdx_Erazer = 1;     // Quiz


        #region Game Event
        public static readonly RaiseEventOptions ToHost = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient};
        public static readonly RaiseEventOptions ToClient = new RaiseEventOptions { Receivers = ReceiverGroup.Others};
        public static readonly RaiseEventOptions ToAll = new RaiseEventOptions { Receivers = ReceiverGroup.All};
        public static readonly SendOptions DefaultSendOptions = new SendOptions { Reliability = true };

        public enum GameEventCode {
            SetTeam = 40, // {ActualPlayerID pid, byte teamidx}
            Kick = 41, // {ActualPlayerID pid}
            EnterGame = 50,
            StartCount = 51, // {byte countdown} // 0 for start
            TimeLeft = 52,  // {byte time} // 0 for end
            End = 53,
            Stop = 54,
            CubePose = 60, // {ActualPlayerID pid, int x, int y, int deg}
            CubeStatus = 61, // {ActualPlayerID pid, GameCubeStatus status}
            CubeCommand = 62, // {ActualPlayerID pid, int left, int right}
            PlayerInfoToHost = 63,   // {ACtualPlayerID, [Mode Depend Stuff]}
            PlayerInfoToAll = 64,   // {ACtualPlayerID, [Mode Depend Stuff]}
            // ResultTeamRank = 70, // {byte teamidx, byte teamidx, ...}
            TeamInfoToAll = 65,
            QuizDict = 66,
            Result = 70, // {ActualPlayerID pid, float ratio, ...}
        }

        #endregion


        #region Room Props
        public const string RoomPropKey_Env = "Env";
        public enum RoomPropEnum_Env { Sim, Real }

        public const string RoomPropKey_Mode = "Mode";
        public enum RoomPropEnum_Mode { Battle, Quiz, QuizDiff }
        public const string RoomPropKey_QuizSetting = "QuizSetting";
        public const string RoomPropKey_AcutalPlayerCount = "APC";

        public const string RoomPropKey_LocalP = "LocalP";  // value type: byte
        public const string RoomPropKey_Time = "Time"; // value type: byte? // TODO Not used yet
        public static byte[] RoomPropValue_Time = {60, 45, 30, 15};

        #endregion


        #region Player Props
        public const string PlayerPropKey_Ready = "Ready";  // value type: bool
        public const string PlayerPropKey_TeamIdx = "Team"; // value type: byte

        #endregion


        #region Functions

        public static bool GetPlayerReady(Player p)
        {
            bool ready = false;
            object _ready;
            if (p.CustomProperties.TryGetValue(PlayerPropKey_Ready, out _ready))
            {
                ready = (bool) _ready;
            }
            return ready;
        }
        public static void SetPlayerReady(bool ready)
        {
            PhotonNetwork.SetPlayerCustomProperties(
                new HashTable() { {PlayerPropKey_Ready, ready}, }
            );
        }

        public static void SetActualPlayerTeamIdx(ActualPlayerID pid, byte team)
        {
            object[] content = new object[]{pid.ActorNumber, pid.LocalNumber, team};
            CastToAllEvent(GameEventCode.SetTeam, content);
        }
        public static (ActualPlayerID, byte) ParseSetTeam(object[] data)
        {
            ActualPlayerID pid = new ActualPlayerID( (int)data[0], (int)data[1] );
            byte team = (byte)data[2];
            return (pid, team);
        }

        public static void KickActualPlayer(ActualPlayerID pid)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            object[] content = new object[]{pid.ActorNumber, pid.LocalNumber};
            CastToAllEvent(GameEventCode.Kick, content);
        }
        public static ActualPlayerID ParseKick(object[] data)
        {
            ActualPlayerID pid = new ActualPlayerID( (int)data[0], (int)data[1] );
            return pid;
        }

        public static RoomPropEnum_Mode GetRoomMode(RoomInfo room=null)
        {
            RoomPropEnum_Mode mode = RoomPropEnum_Mode.Battle;

            if (room == null)
                room = PhotonNetwork.CurrentRoom;
            if (room == null) return mode;

            object _mode;
            if (room.CustomProperties.TryGetValue(RoomPropKey_Mode, out _mode))
            {
                mode = (RoomPropEnum_Mode) _mode;
            }
            return mode;
        }
        public static string GetRoomQuizSetting()
        {
            string setting = "";

            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return setting;

            object _setting;
            if (room.CustomProperties.TryGetValue(RoomPropKey_QuizSetting, out _setting))
            {
                setting = (string) _setting;
            }
            return setting;
        }
        public static RoomPropEnum_Env GetRoomEnv()
        {
            RoomPropEnum_Env env = RoomPropEnum_Env.Sim;

            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return env;

            object _env;
            if (room.CustomProperties.TryGetValue(RoomPropKey_Env, out _env))
            {
                env = (RoomPropEnum_Env) _env;
            }
            return env;
        }
        public static int GetRoomTime()
        {
            byte time = RoomPropValue_Time[0];

            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return time;

            object _time;
            if (room.CustomProperties.TryGetValue(RoomPropKey_Time, out _time))
            {
                time = (byte) _time;
            }
            return (int)time;
        }
        public static int GetRoomLocalPlayers()
        {
            byte localP = 1;

            var room = PhotonNetwork.CurrentRoom;
            if (room == null) return localP;

            object _localP;
            if (room.CustomProperties.TryGetValue(RoomPropKey_LocalP, out _localP))
            {
                localP = (byte) _localP;
            }
            return localP;
        }
        public static void SetRoomAcutalPlayers(int num)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            var room = PhotonNetwork.CurrentRoom;
            var prop = room.CustomProperties;

            prop[RoomPropKey_AcutalPlayerCount] = num;
            room.SetCustomProperties(prop);

            room.IsOpen = num < room.MaxPlayers;
        }
        public static int GetRoomAcutalPlayers(RoomInfo room=null)
        {
            int num = 1;

            if (room == null)
                room = PhotonNetwork.CurrentRoom;
            if (room == null) return num;

            object _num;
            if (room.CustomProperties.TryGetValue(RoomPropKey_AcutalPlayerCount, out _num))
            {
                num = (int) _num;
            }
            return num;
        }


        #endregion


        public static void CastToAllEvent(GameEventCode code, object[] content)
        {
            PhotonNetwork.RaiseEvent((byte)code, content, ToAll, DefaultSendOptions);
        }
        public static void CastToHostEvent(GameEventCode code, object[] content)
        {
            PhotonNetwork.RaiseEvent((byte)code, content, ToHost, DefaultSendOptions);
        }




        #region Game Event Protocols
        public static void SendCubeCommand(ActualPlayerID pid, int uL, int uR)
        {
            object[] content = {(byte)pid.ActorNumber, (byte)pid.LocalNumber, (byte)uL, (byte)uR};
            CastToHostEvent(GameEventCode.CubeCommand, content);
        }
        public static (ActualPlayerID, int, int) ParseCubeCommand(object[] data)
        {
            ActualPlayerID pid = new ActualPlayerID( (byte)data[0], (byte)data[1] );
            int uL = (sbyte)(byte)data[2];
            int uR = (sbyte)(byte)data[3];
            return (pid, uL, uR);
        }
        public static void CastCubePose(ActualPlayerID pid, int x, int y, int deg)
        {
            object[] content = new object[]{ (byte)pid.ActorNumber, (byte)pid.LocalNumber, (Int16)x, (Int16)y, (Int16)deg };
            CastToAllEvent(GameEventCode.CubePose, content);
        }
        public static (ActualPlayerID, int, int, int) ParseCubePose(object[] data)
        {
            ActualPlayerID pid = new ActualPlayerID( (byte)data[0], (byte)data[1] );
            int x = (Int16)data[2];
            int y = (Int16)data[3];
            int deg = (Int16)data[4];
            return (pid, x, y, deg);
        }
        public static (ActualPlayerID, GameCubeStatus) ParseCubeStatus(object[] data)
        {
            ActualPlayerID pid = new ActualPlayerID( (int)data[0], (int)data[1] );
            GameCubeStatus status = (GameCubeStatus)data[2];
            return (pid, status);
        }
        public static void CastEnd()
        {
            CastToAllEvent(GameEventCode.End, new object[]{});
        }
        public static void CastStop()
        {
            CastToAllEvent(GameEventCode.Stop, new object[]{});
        }
        public static void CastResult(Dictionary<ActualPlayerID, float> pidRatioDict)
        {
            List<object> content = new List<object>();
            foreach (var pid in pidRatioDict.Keys)
            {
                var ratio = pidRatioDict[pid];
                content.Add(pid.ActorNumber);
                content.Add(pid.LocalNumber);
                content.Add(ratio);
            }
            CastToAllEvent(GameEventCode.Result, content.ToArray());
        }
        public static Dictionary<ActualPlayerID, float> ParseResult(object[] data)
        {
            Dictionary<ActualPlayerID, float> pidRatioDict = new Dictionary<ActualPlayerID, float>();
            for (int i = 0; i < data.Length/3; i++)
            {
                ActualPlayerID pid = new ActualPlayerID( (int)data[i*3+0], (int)data[i*3+1] );
                float ratio = (float)data[i*3+2];
                pidRatioDict.Add(pid, ratio);
            }
            return pidRatioDict;
        }

        #endregion



        #region PUN Wrapper
        public static bool IsMasterClient { get {return PhotonNetwork.IsMasterClient;} }
        public static Player LocalPlayer { get {return PhotonNetwork.LocalPlayer;}}
        public static Player[] PlayerList { get {return PhotonNetwork.PlayerList;}}
        public static RoomInfo CurrentRoom { get {return PhotonNetwork.CurrentRoom;}}
        #endregion

    }

    public enum GameCubeStatus
    {
        Normal = 0, SpeedUp = 1, SpeedDown = 2, Reverse = 3, Stagger = 4, FreezeOthers = 5
    }

    public struct ActualPlayerID
    {
        public int ActorNumber { get; private set; }
        public int LocalNumber { get; private set; }
        public ActualPlayerID (int actorNumber, int localNumber=0)
        {
            this.ActorNumber = actorNumber;
            this.LocalNumber = localNumber;
        }
        public ActualPlayerID (Player p, int localNumber=0)
        {
            this.ActorNumber = p.ActorNumber;
            this.LocalNumber = localNumber;
        }

        public bool isNull { get {return ActorNumber==-1;}}

        public static ActualPlayerID Local(int localNumber=0)
        {
            return new ActualPlayerID(PhotonNetwork.LocalPlayer, localNumber);
        }
        public static ActualPlayerID Null { get {
            return new ActualPlayerID(-1);
        }}


        public override int GetHashCode()
        {
            return Tuple.Create(ActorNumber, LocalNumber).GetHashCode();
        }
        public override string ToString()
        {
            return "(" + ActorNumber + ", " + LocalNumber + ")";
        }

        public static bool operator ==(ActualPlayerID a, ActualPlayerID b)
        {
            return a.ActorNumber==b.ActorNumber && a.LocalNumber==b.LocalNumber;
        }
        public static bool operator !=(ActualPlayerID a, ActualPlayerID b)
        {
            return !(a==b);
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

    }


}
