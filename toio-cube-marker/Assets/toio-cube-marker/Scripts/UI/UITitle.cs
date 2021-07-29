using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;



namespace CubeMarker
{

    public class UITitle : MonoBehaviourPunCallbacks
    {
        public UICommon manager;
        public TMP_InputField inputField;
        public TMP_Text textConnecting;
        public GameObject btnGo;



        public void SetActive(bool value)
        {
            this.gameObject.SetActive(value);

            if (value)
            {
                this.ShowConnecting(!PhotonNetwork.IsConnected);
                if (!PhotonNetwork.IsConnected) PhotonNetwork.ConnectUsingSettings();
            }
        }



        #region PUN Callbacks

        public override void OnConnectedToMaster()
        {
            if (!this.enabled) return;
            this.ShowConnecting(false);
        }

        #endregion


        protected void ShowConnecting(bool show)
        {
            textConnecting.gameObject.SetActive(show);
            btnGo.SetActive(!show);
        }

        public void OnBtnGo()
        {
            string name = inputField.text;

            // Set name to PUN
            PhotonNetwork.NickName = name;

            // Cache name locally
            PlayerPrefs.SetString("PlayerName", name);

            // Transit
            manager.SetUIState(UIState.lobby);
        }

    }

}
