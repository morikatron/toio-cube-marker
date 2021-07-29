using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using HashTable = ExitGames.Client.Photon.Hashtable;
using toio;


namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class UIConnect : MonoBehaviourPunCallbacks
    {


        #region ======== Inspector ========
        public UICommon manager;

        [Header("UI References")]
        public Field field;
        public RectTransform landmarkRed;
        public RectTransform landmarkBlue;
        public UICubeInfo info0;
        public UICubeInfo info1;
        public UICubeInfo info2;
        public UICubeInfo info3;
        public GameObject tipConnect;
        public TMP_Text tipCalibrate;
        public Button btnConnect;
        public Button btnDisconnect;
        public Button btnCalibrate;
        public Button btnOK;
        #endregion ======== Inspector ========

        private int calibPhase = 0;


        float elipsed = 0;
        void Update()
        {
            elipsed += Time.deltaTime;
            if (elipsed < 0.25f) return;
            elipsed = 0;
            var connectedCubes = DuelCubeManager.Ins.RealCubes;
            if (connectedCubes.Count == 0) return;

            for (int i=0; i<connectedCubes.Count; i++)
            {
                var cube = connectedCubes[i];
                switch (i){
                    case 0: info0.SetPos(cube.x, cube.y, cube.angle); break;
                    case 1: info1.SetPos(cube.x, cube.y, cube.angle); break;
                    case 2: info2.SetPos(cube.x, cube.y, cube.angle); break;
                    case 3: info3.SetPos(cube.x, cube.y, cube.angle); break;
                }
            }
        }



        public void SetActive(bool value)
        {
            if (this.gameObject.activeSelf == value) return;
            this.gameObject.SetActive(value);

            calibPhase = 0;

            if (value)
            {
                DuelCubeManager.Ins.isReal = true;
                int cnt = DuelCubeManager.Ins.NumRealCubes;
                Debug.Log("UIConnect.SetActive: cnt=" + cnt);
                UpdateConnectUI(cnt, false);
                UpdateCalib();
                UpdateCubeInfos(cnt);
            }
        }


        private void UpdateConnectUI(int connectedCount, bool connecting)
        {
            if (connectedCount >= 4)
            {
                btnConnect.transform.GetComponentInChildren<TMP_Text>().text = "Connected";
                btnConnect.interactable = false;
                btnDisconnect.interactable = true;
                btnOK.interactable = true;
                btnCalibrate.interactable = true;
            }
            else if (connecting)
            {
                btnConnect.transform.GetComponentInChildren<TMP_Text>().text = "Connecting";
                btnConnect.interactable = false;
                btnDisconnect.interactable = false;
                btnOK.interactable = false;
                btnCalibrate.interactable = false;

            }
            else if (connectedCount > 0)
            {
                btnConnect.transform.GetComponentInChildren<TMP_Text>().text = "Connect";
                btnConnect.interactable = true;
                btnDisconnect.interactable = true;
                btnOK.interactable = true;
                btnCalibrate.interactable = true;
            }
            else // Not connected
            {
                btnConnect.transform.GetComponentInChildren<TMP_Text>().text = "Connect";
                btnConnect.interactable = true;
                btnDisconnect.interactable = false;
                btnOK.interactable = true;
                btnCalibrate.interactable = false;
            }
            tipConnect.SetActive(connectedCount == 0);
        }

        private void UpdateCalib()
        {
            if (calibPhase == 0)
            {
                btnOK.interactable = true; btnConnect.interactable = true;
                landmarkRed.gameObject.SetActive(false); landmarkBlue.gameObject.SetActive(false);
                var cubes = DuelCubeManager.Ins.RealCubes;
                if (cubes.Count > 0) cubes[0].TurnLedOff();
                btnCalibrate.transform.GetComponentInChildren<TMP_Text>().text = "Calibrate";
                tipCalibrate.text = "If you overlay a mat on a monitor, press the button above to adjust the size.\n\n開発マットをモニター上に敷く場合、上のボタンを押してサイズを調整してください。";
            }
            else if (calibPhase == 1)
            {
                btnOK.interactable = false; btnConnect.interactable = false;
                landmarkRed.gameObject.SetActive(true); landmarkBlue.gameObject.SetActive(false);
                DuelCubeManager.Ins.RealCubes[0].TurnLedOn(255, 0, 0, 0);
                btnCalibrate.transform.GetComponentInChildren<TMP_Text>().text = "Next";
                tipCalibrate.text = "Put the cube with red LED on the mark.\n\n赤く点灯しているキューブをマークに重ねてください。";
            }
            else if (calibPhase == 2)
            {
                btnOK.interactable = false; btnConnect.interactable = false;
                landmarkRed.gameObject.SetActive(false); landmarkBlue.gameObject.SetActive(true);
                DuelCubeManager.Ins.RealCubes[0].TurnLedOn(0, 0, 255, 0);
                btnCalibrate.transform.GetComponentInChildren<TMP_Text>().text = "OK";
                tipCalibrate.text = "Put the cube with blue LED on the mark.\n\n青く点灯しているキューブをマークに重ねてください。";
            }


        }

        private void UpdateCubeInfos(int connectedCount)
        {
            info0.SetActive(connectedCount>0); info1.SetActive(connectedCount>1);
            info2.SetActive(connectedCount>2); info3.SetActive(connectedCount>3);
            tipConnect.SetActive(connectedCount == 0);
        }



        #region =========== UI Callbacks ===========
        public async void OnBtnConnect()
        {
            Debug.Log("On UIConnect BtnConnect");
            var ins = DuelCubeManager.Ins;
            int cnt = ins.NumRealCubes;

            try
            {
                if (ins.NumRealCubes >= 4) {}
                else if (ins.isRealConnecting)
                {
                    UpdateConnectUI(cnt, true);
                }
                else
                {
                #if UNITY_WEBGL && !UNITY_EDITOR
                    UpdateConnectUI(cnt, true);  // Connecting
                    var cubes = await ins.SingleConnectRealCube();
                #else
                    UpdateConnectUI(cnt, true);  // Connecting
                    var cubes = await ins.MultiConnectRealCubes(4);
                #endif
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("UIConnect.OnBtnConnect()\n" + e);
            }

            cnt = ins.NumRealCubes;
            UpdateConnectUI(cnt, false);
            UpdateCubeInfos(cnt);
        }

        public void OnBtnDisconnect()
        {
            Debug.Log("On UIConnect BtnDisconnect");
            var ins = DuelCubeManager.Ins;
            ins.DisconnectRealCubes();

            UpdateConnectUI(0, false);
            UpdateCubeInfos(0);
        }

        private float redX, redY, redDeg;
        public void OnBtnCalibrate()
        {
            Debug.Log("On UIConnect BtnCalibrate");

            if (DuelCubeManager.Ins.isRealConnecting || !DuelCubeManager.Ins.isRealConnected) return;

            // Process
            var cube = DuelCubeManager.Ins.RealCubes[0];
            if (calibPhase == 1)
            {
                redX = cube.x; redY = cube.y; redDeg = cube.angle;
            }
            else if (calibPhase == 2)
            {
                field.Calibrate(
                    landmarkRed.anchoredPosition.x, landmarkRed.anchoredPosition.y,
                    redX, redY,
                    landmarkBlue.anchoredPosition.x, landmarkBlue.anchoredPosition.y,
                    cube.x, cube.y
                );
            }

            // Transit and Update UI
            calibPhase ++; calibPhase %= 3;
            UpdateCalib();
        }

        public void OnBtnOK()
        {
            Debug.Log("On UIConnect BtnOK");

            manager.SetUIState(UIState.roomcreate);

            DuelCubeManager.Ins.ClearSimCubes();
        }
        #endregion =========== UI Callbacks ===========


    }

}
