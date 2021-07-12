using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class UILobby : MonoBehaviourPunCallbacks
    {
        public UICommon ui;

        public Transform roomListContent;
        public Button prefabRoomListItem;



        public void SetActive(bool value)
        {
            this.gameObject.SetActive(value);

            UpdateRoomList();
        }



        #region PUN Callbacks

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log("lobby OnRoomListUpdate");
            UpdateRoomList();
        }

        public override void OnJoinedRoom()
        {
            if (!this.gameObject.activeSelf) return;
            Debug.Log("lobby OnJoinedRoom()");

            ClearRoomList();
            ui.SetUIState(UIState.room);
        }

        #endregion



        #region UI Callbacks

        private void OnRoomClicked(string name)
        {
            NetworkManager.JoinRoom(name);
        }

        public void OnBtnBack()
        {
            ui.SetUIState(UIState.title);
        }

        public void OnBtnCreate()
        {
            ui.SetUIState(UIState.roomcreate);
        }

        #endregion


        private void UpdateRoomList()
        {
            if (!this.gameObject.activeSelf) return;

            // Clear items
            ClearRoomList();

            // Create items
            foreach (var room in NetworkManager.roomDict.Values)
            {
                if (room.IsOpen && room.IsVisible && !room.RemovedFromList && room.MaxPlayers!=0)
                {
                    string name = room.Name;
                    string mode = GetRoomMode(room).ToString();
                    // string players = room.PlayerCount + "/" + room.MaxPlayers;
                    string players = GetRoomAcutalPlayers(room) + "/" + room.MaxPlayers;

                    Button btn = GameObject.Instantiate(prefabRoomListItem, roomListContent.position, Quaternion.identity);
                    btn.name = name;
                    btn.transform.Find("TextName").GetComponent<TMP_Text>().text = name;
                    btn.transform.Find("TextMode").GetComponent<TMP_Text>().text = mode;
                    btn.transform.Find("TextNum").GetComponent<TMP_Text>().text = players;

                    btn.transform.SetParent(roomListContent, false);

                    // Add Click Callback
                    btn.onClick.AddListener( () => {OnRoomClicked(btn.name);} );
                }
            }
        }

        private void ClearRoomList()
        {
            for (int i=0; i<roomListContent.childCount; i++)
                GameObject.Destroy(roomListContent.GetChild(i).gameObject);
        }


    }

}