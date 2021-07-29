using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using HashTable = ExitGames.Client.Photon.Hashtable;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class UIRoomCreate : MonoBehaviourPunCallbacks
    {


        #region ======== Inspector ========
        public UICommon ui;

        [Header("UI References")]
        public Transform areaEnv;
        public Transform areaName;
        public Transform areaMode;
        public Transform areaMaxP;
        public Transform areaQuizSetting;
        public Transform areaTime;
        public Button btnConnect;
        public Button btnCreate;
        public GameObject btnCustomQuiz;
        #endregion ======== Inspector ========



        public void SetActive(bool value)
        {
            if (gameObject.activeSelf == value) return;
            this.gameObject.SetActive(value);

            if (value)
            {
                OnEnvChanged();
                OnModeChanged();
            }
        }



        #region =========== PUN Callbacks ===========

        public override void OnJoinedRoom()
        {
            if (ui.state != UIState.roomcreate) return;

            ui.SetUIState(UIState.room);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            if (ui.state != UIState.roomcreate) return;

            // Check name again
            OnNameChanged();
        }

        #endregion



        #region =========== UI Callbacks ===========

        public void OnEnvChanged()
        {
            TMP_Text tip = areaEnv.Find("Tip").GetComponentInChildren<TMP_Text>();
            int idx = areaEnv.GetComponentInChildren<TMP_Dropdown>().value;
            if (idx==0) // Simulator
            {
                tip.text = "Host without real cubes.";
                btnConnect.interactable = false;
                btnCreate.interactable = true;
            }
            else  // Real
            {
                tip.text = "Host with real cubes. Requires connection.";
                btnConnect.interactable = true;
                btnCreate.interactable = DuelCubeManager.Ins.NumRealCubes > 0;
            }
            UpdateMapPlayersUI();
        }

        private void UpdateMapPlayersUI()
        {
            int env = areaEnv.GetComponentInChildren<TMP_Dropdown>().value;
            var dp = areaMaxP.GetComponentInChildren<TMP_Dropdown>();
            var dpe = dp.GetComponent<DropdownExtend>();
            var tipObj = areaMaxP.Find("Tip").gameObject;
            var tip = tipObj.transform.GetComponentInChildren<TMP_Text>();

            if (env == 0)   // Sim
            {
                // Enable all options
                dpe.indexesToDisable.Clear();
                dpe.Apply();

                // // Restore maxp
                // int maxP = int.Parse(dp.captionText.text);
                // if (maxP == 0)
                //     dp.transform.Find("Label").GetComponent<TMP_Text>().text = "4";

                // Tips
                tipObj.SetActive(false);
            }
            else    // Real
            {
                int cubes = DuelCubeManager.Ins.NumRealCubes;

                // // Force maxp < cubes
                // int maxP = int.Parse(dp.captionText.text);
                // if (maxP > cubes)
                //     dp.transform.Find("Label").GetComponent<TMP_Text>().text = cubes.ToString();

                // Disable options
                dpe.indexesToDisable.Clear();
                for (int i=0; i<4; i++)
                    if (4-i > cubes) dpe.indexesToDisable.Add(i);
                dpe.Apply();

                // Tips
                tipObj.SetActive(true);
                if (cubes == 0)
                {
                    tip.text = "No cube connected.";
                    tip.color = new Color(250f/255f, 180f/255f, 180f/255f);
                }
                else
                {
                    tip.text = "Max Players must be less than connected cubes.";
                    tip.color = Color.white;
                }
            }
        }

        public void OnNameChanged()
        {
            TMP_Text tip = areaName.Find("Tip").GetComponentInChildren<TMP_Text>();
            string name = areaName.GetComponentInChildren<TMP_InputField>().text.Trim();
            if (name=="" || !NetworkManager.IsRoomNameAvailable(name))
            {
                tip.text = "Inavailable.";
                tip.color = new Color(250f/255f, 180f/255f, 180f/255f);
            }
            else
            {
                tip.text = "Available.";
                tip.color = Color.white;
            }

            // Cannot input space at first
            if (name == "") areaName.GetComponentInChildren<TMP_InputField>().text = "";
        }

        public void OnModeChanged()
        {
            TMP_Text tip = areaMode.Find("Tip").GetComponentInChildren<TMP_Text>();
            int idx = areaMode.GetComponentInChildren<TMP_Dropdown>().value;
            if (idx == 0)   // battle
            {
                tip.text = "A battle to cover as much area as possible with your cube, using the cube as a paint brush.\n\nキューブを走らせて色を塗り、指定した時間内で塗った面積を競い合うバトルゲームです。";
                areaQuizSetting.gameObject.SetActive(false);
                btnCustomQuiz.SetActive(false);
            }
            else if (idx == 1)  // quiz
            {
                tip.text = "A quiz game where you have to guess which image lies underneath using cubes to peel away the mask. (Only one chance to answer) \n\nキューブを走らせることで、浮かび上がる画像の内容をあてるクイズゲームです。対戦側のチームがその邪魔をしてきます。(回答のチャンスは1回のみ)";
                areaQuizSetting.gameObject.SetActive(true);
                OnQuizSettingChanged();
            }
            else if (idx == 2)  // quizdiff
            {
                tip.text = "A quiz game where each player tries to uncover their image while the other are competing for the same space. (Only one chance to answer) \n\nチーム毎に異なる画像を浮かび上がらせ、その内容をあてるクイズゲームです。(回答のチャンスは1回のみ)";
                areaQuizSetting.gameObject.SetActive(true);
                OnQuizSettingChanged();
            }
        }

        public void OnQuizSettingChanged()
        {
            var dropdown = areaQuizSetting.GetComponentInChildren<TMP_Dropdown>();
            string option = dropdown.options[dropdown.value].text;
            if (option == "Preset-Animal")
            {
                btnCustomQuiz.SetActive(false);
            }
            else if (option == "Custom")
            {
                btnCustomQuiz.SetActive(true);
            }
        }


        public void OnBtnBack()
        {
            if (NetworkManager.isJoiningRoom) return;

            ui.SetUIState(UIState.lobby);
        }

        public void OnBtnCreate()
        {
            string name = areaName.GetComponentInChildren<TMP_InputField>().text;
            int env = areaEnv.GetComponentInChildren<TMP_Dropdown>().value;
            int mode = areaMode.GetComponentInChildren<TMP_Dropdown>().value;
            string quizSetting = areaQuizSetting.GetComponentInChildren<TMP_Dropdown>().options[areaQuizSetting.GetComponentInChildren<TMP_Dropdown>().value].text;
            int maxP = int.Parse(areaMaxP.GetComponentInChildren<TMP_Dropdown>().captionText.text);
            int time = RoomPropValue_Time[areaTime.GetComponentInChildren<TMP_Dropdown>().value];

            // Check
            bool ok = true;
            if (!NetworkManager.IsRoomNameAvailable(name))
            {
                OnNameChanged();
                ok = false;
            }
            if (env==1 && maxP > DuelCubeManager.Ins.NumRealCubes) // Real
            {
                OnEnvChanged();
                ok = false;
            }

            if (ok)
                NetworkManager.CreateRoom(name, env, mode, quizSetting, maxP, time);
        }

        public void OnBtnConnect()
        {
            ui.SetUIState(UIState.connect);
        }

        public void OnBtnCustomQuiz()
        {
            ui.OpenCustomQuiz();
        }

        #endregion



    }

}
