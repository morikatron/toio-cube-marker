using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class UIRoom : MonoBehaviourPunCallbacks
    {

        public UICommon ui;

        [Header("UI References")]
        public Button buttonGo;
        public RoomAreaTeam teamRed;
        public RoomAreaTeam teamBlue;
        public RoomAreaTeam teamGreen;
        public RoomAreaTeam teamYellow;
        public TMP_Text textTip;
        public TMP_Text textName;


        private bool ready = false;


        void Update()
        {
            // Generate flashing color
            var a = (Mathf.Cos(Time.time * 6)-1)/2;     // 0 ~ -1
            a = 1 + a*0.2f;
            var color = new Color(a, a, a);

            // flashing ready/start button
            if (!ready || IsMasterClient && buttonGo.interactable)
                buttonGo.transform.GetComponent<Image>().color = color;

            // flashing join buttons
            if (!ready || IsMasterClient)
                for (byte teamIdx=0; teamIdx<4; teamIdx++)
                    if (teamIdx == NetworkManager.localTeamIdx)
                        GetTeamFromIdx(teamIdx).transform.Find("ButtonJoin").GetComponent<Image>().color = Color.white;
                    else
                        GetTeamFromIdx(teamIdx).transform.Find("ButtonJoin").GetComponent<Image>().color = color;
        }


        public void SetActive(bool value)
        {
            if (gameObject.activeSelf == value) return;

            this.gameObject.SetActive(value);

            if (value)
            {
                // Set Name
                textName.text = NetworkManager.roomName;

                // Init UI teams
                for (int i=0; i<4; i++)
                {
                    var team = GetTeamFromIdx(i);
                    team.ShowButtonAddAI(IsMasterClient);
                    team.ShowButtonDelete(IsMasterClient);
                    team.EnableButtonJoin(true);
                    team.deleteCallback = pid => OnBtnDelete(i, pid);
                }

                var mode = GetRoomMode();
                if (mode == RoomPropEnum_Mode.Quiz)
                {
                    teamGreen.gameObject.SetActive(false);
                    teamYellow.gameObject.SetActive(false);
                }
                else
                {
                    teamGreen.gameObject.SetActive(true);
                    teamYellow.gameObject.SetActive(true);
                }

                UpdateBtnGo();
            }
        }


        protected void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom(becomeInactive: false);
            ui.SetUIState(UIState.lobby);
            ClearTeams();
        }

        internal void BackFromGame()
        {
            var room = PhotonNetwork.CurrentRoom;
            room.IsVisible = true;
        }



        #region PUN Callbacks

        protected virtual void OnEvent(EventData photonEvent)
        {
            GameEventCode code = (GameEventCode)photonEvent.Code;
            object[] data = (object[])photonEvent.CustomData;
            if (code == GameEventCode.EnterGame)
            {
                ui.SetUIState(UIState.game);
                if (!IsMasterClient) SetPlayerReady(false);
            }
            else if (code == GameEventCode.SetTeam)
            {
                (ActualPlayerID pid, byte teamIdx) = ParseSetTeam(data);
                Receive_SetTeam(pid, teamIdx);
            }
            else if (code == GameEventCode.Kick)
            {
                ActualPlayerID pid = ParseKick(data);
                Receive_Kick(pid);
            }
        }

        public override void OnPlayerEnteredRoom(Player p)
        {
            UpdateBtnGo();
        }

        public override void OnPlayerLeftRoom(Player p)
        {
            RemoveActualPlayerFromTeams(new ActualPlayerID(p));
            UpdateBtnGo();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            LeaveRoom();
            UpdateBtnGo();
        }

        public override void OnPlayerPropertiesUpdate(Player p, Hashtable props)
        {
            int id = p.ActorNumber;
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
            {
                if (pid.ActorNumber == p.ActorNumber)
                {
                    var team = GetTeamFromIdx(NetworkManager.pidTeamIdxDict[pid]);
                    team.SetPlayer(pid, p.NickName, GetPlayerReady(p), p.IsMasterClient, p.IsLocal);
                }
            }

            // Set Join Buttons
            if (p.IsLocal && !p.IsMasterClient)
                for (int i=0; i<4; i++) GetTeamFromIdx(i).EnableButtonJoin(!GetPlayerReady(p));

            UpdateBtnGo();
        }

        #endregion



        protected void Receive_SetTeam(ActualPlayerID pid, byte teamIdx)
        {
            // Remove player slot if existed
            RemoveActualPlayerFromTeams(pid, teamIdx);

            // Add player slot
            var team = GetTeamFromIdx(teamIdx);
            string name = NetworkManager.GetAcutalPlayerName(pid);
            bool ready = NetworkManager.IsAcutalPlayerReady(pid);
            bool host = NetworkManager.IsAcutalPlayerHost(pid);
            team.AddPlayer(pid, name, ready, host);

            // Update tips if self'team changed
            if (pid == ActualPlayerID.Local())
                UpdateTips();
        }

        protected void Receive_Kick(ActualPlayerID pid)
        {
            if (pid == ActualPlayerID.Local())
            {
                LeaveRoom();
            }
            else if (pid.LocalNumber > 0)   // AI
            {
                RemoveActualPlayerFromTeams(pid);

                UpdateBtnGo();
            }
        }



        #region UI Callbacks

        public void OnBtnAddAI(int i)
        {
            if (!IsMasterClient) return;
            if (NetworkManager.pidCount >= CurrentRoom.MaxPlayers) return;

            for (int l=1; l<4; l++)
            {
                ActualPlayerID pid = ActualPlayerID.Local(l);
                if (NetworkManager.HasActualPlayer(pid)) continue;
                NetworkManager.SetTeam(pid, (byte)i);
                break;
            }
        }

        public void OnBtnDelete(int teamIdx, ActualPlayerID pid)
        {
            if (!IsMasterClient) return;
            if (pid == ActualPlayerID.Local()) return;

            KickActualPlayer(pid);
        }

        public void OnBtnJoinTeam(int teamIdx)
        {
            if (GetTeamFromIdx(teamIdx).IsFull()) return;

            NetworkManager.JoinTeam((byte)teamIdx);
        }

        public void OnBtnBack()
        {
            LeaveRoom();
        }

        public void OnBtnGo()
        {
            // Host: Enter Game
            if (IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsVisible = false;
                CastToAllEvent(GameEventCode.EnterGame, new object[]{});
            }
            // Client: Switch Ready
            else
            {
                SetPlayerReady(!NetworkManager.ready);
            }

        }

        #endregion



        #region UI Utils

        private void UpdateBtnGo()
        {
            if (IsMasterClient)
            {
                buttonGo.transform.GetComponentInChildren<TMP_Text>().text = "START!";
                if (IsAllReady())
                {
                    buttonGo.interactable = true;
                }
                else
                {
                    buttonGo.interactable = false;
                }
                ready = true;
                buttonGo.transform.GetComponent<Image>().color = Color.white;
            }
            else
            {
                buttonGo.interactable = true;
                ready = NetworkManager.ready;
                if (ready)
                {
                    buttonGo.transform.GetComponentInChildren<TMP_Text>().text = "READY!";
                    buttonGo.transform.GetComponent<Image>().color = new Color32(220, 220, 220, 255);
                }
                else
                    buttonGo.transform.GetComponentInChildren<TMP_Text>().text = "READY?";
            }

        }

        private void UpdateTips()
        {
            var mode = GetRoomMode();
            var pid = ActualPlayerID.Local();
            var teamIdx = NetworkManager.HasActualPlayer(pid)? NetworkManager.pidTeamIdxDict[pid] : 0;
            // Show tips
            if (mode == RoomPropEnum_Mode.Battle)
                textTip.text = "Paint more area to win!\n自チームの色で塗りつぶそう!";
            else if (mode == RoomPropEnum_Mode.Quiz)
            {
                if (teamIdx == TeamIdx_Painter)
                    textTip.text = "Uncover image and guess what's on it.\n画像を浮かび上がらせて内容を当てよう!";
                else
                    textTip.text = "Prevent opponents from finding what's on image.\n相手が答えを見つけるのを阻止せよ!";
            }
            else if (mode == RoomPropEnum_Mode.QuizDiff)
            {
                textTip.text = "Uncover own image and guess faster!\n自チームの画像を浮かび上がらせて内容を一はやく当てよう!";
            }
        }

        private void RemoveActualPlayerFromTeams(ActualPlayerID pid, byte skipTeamIdx=255)
        {
            // Remove player slot if existed
            for (byte i=0; i<4; i++)
            {
                if (i == skipTeamIdx) continue;
                var _team = GetTeamFromIdx(i);
                if (_team.Has(pid))
                {
                    _team.RemovePlayer(pid);
                    break;
                }
            }
        }

        private RoomAreaTeam GetTeamFromIdx(int i)
        {
            switch (i){
                case 0: return teamRed;
                case 1: return teamBlue;
                case 2: return teamGreen;
                case 3: return teamYellow;
                default: return teamRed;
            }
        }

        private void ClearTeams()
        {
            teamRed.Clear(); teamBlue.Clear(); teamGreen.Clear(); teamYellow.Clear();
        }

        #endregion



        private bool IsAllReady()
        {
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p != PhotonNetwork.LocalPlayer)
                {
                    if (!GetPlayerReady(p)) return false;
                }
            }
            return true;
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
