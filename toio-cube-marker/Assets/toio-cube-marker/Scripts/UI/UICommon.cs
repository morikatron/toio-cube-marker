using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using HashTable = ExitGames.Client.Photon.Hashtable;


namespace CubeMarker
{
    using static PUNProtocolUtils;

    public enum UIState
    {
        title, lobby, roomcreate, room, connect, game
    }


    public class UICommon : MonoBehaviourPunCallbacks
    {

        #region Proterties in Inspector
        public UITitle uiTitle;
        public UILobby uiLobby;
        public UIRoomCreate uiRoomCreate;
        public UIRoom uiRoom;
        public UIConnect uiConnect;
        public UIGame uiGames;

        public UICustomQuiz uiCustomQuiz;

        public FloatingJoystick joystick;
        #endregion


        protected UIState _state = UIState.title;
        public UIState state { get { return _state; } set { SetUIState(value); } }


        void Start()
        {
            #if UNITY_ANDROID || UNITY_IOS
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            #endif

            this.SetUIState(UIState.title);
            uiCustomQuiz?.SetActive(false);
        }


        public void SetUIState(UIState state)
        {
            this._state = state;
            // var canvasTransform = GameObject.FindObjectOfType<Canvas>().transform;
            // canvasTransform.Find("PageTitle")

            uiTitle?.SetActive( state == UIState.title );
            uiLobby?.SetActive( state == UIState.lobby );
            uiRoomCreate?.SetActive( state == UIState.roomcreate );
            uiRoom?.SetActive( state == UIState.room || state == UIState.game );
            uiConnect?.SetActive( state == UIState.connect );
            uiGames?.SetActive( state == UIState.game );

            joystick.gameObject.SetActive(state == UIState.game);
        }
        public void OpenCustomQuiz()
        {
            uiCustomQuiz?.SetActive(true);
        }


    }


}
