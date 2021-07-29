using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class UIGame : MonoBehaviourPunCallbacks, IUIGame
    {
        #region Inspector Props

        public UICommon common;

        [Header("Prefabs")]
        public CubeMarker cubeMarkerPrefab;
        public Nameplate nameplatePrefab;

        [Header("Game Implements")]
        public UIGameBattle gameBattle;
        public UIGameQuiz gameQuiz;
        public UIGameQuizDiff gameQuizDiff;


        [Header("UI References")]
        public Transform markersLayer;
        public Transform nameplateLayer;
        public TMP_Text textCountdownStart;
        public RectTransform timerTransform;
        public Field field;

        public GameObject overlayBG;
        public TMP_Text textCountdownQuit;

        #endregion


        private bool isTimerRunning = false;
        private float lastRealTime;
        private float timeLimit = 0;
        private float timeLeft;


        private List<CubeMarker> cubeMarkers = new List<CubeMarker>();
        private List<Nameplate> nameplates = new List<Nameplate>();
        private Dictionary<int, IPen> markerPenDict = new Dictionary<int, IPen>();


        internal bool drawable { get; private set; }


        void Update()
        {
            // Update Timer
            TimerUpdate();
        }


        public void SetActive(bool value)
        {
            if (this.gameObject.activeSelf == value) return;
            this.gameObject.SetActive(value);

            Clear();

            if (value)
            {
                var mode = GetRoomMode();

                field.Clear();
                field.LoadSettings();
                drawable = false;

                gameBattle?.SetActive(mode == RoomPropEnum_Mode.Battle);
                gameQuiz?.SetActive(mode == RoomPropEnum_Mode.Quiz);
                gameQuizDiff?.SetActive(mode == RoomPropEnum_Mode.QuizDiff);
            }
            else
            {
                gameBattle?.SetActive(false);
                gameQuiz?.SetActive(false);
                gameQuizDiff?.SetActive(false);
            }
        }

        public void Back()
        {
            Clear();

            common.SetUIState(UIState.room);
        }

        private void Clear()
        {
            ResetTimer();

            overlayBG.SetActive(false);
            textCountdownQuit.gameObject.SetActive(false);

            ClearCubeMarkers();
        }


        public void SetDrawable(bool value)
        {
            this.drawable = value;
        }

        #region UI Impl.
        public void Countdown(byte count, string zeroText="GO!", float duration=0.75f)
        {
            StartCoroutine(IE_Countdown1Digit(count, zeroText, duration));
        }
        private IEnumerator IE_Countdown1Digit(byte count, string zeroText="GO!", float duration=0.75f)
        {
            if (duration > 1) duration = 1;
            if (duration < 0) duration = 0.05f;

            textCountdownStart.gameObject.SetActive(true);
            if (count > 0) textCountdownStart.text = count.ToString();
            else textCountdownStart.text = zeroText;
            yield return new WaitForSecondsRealtime(duration);
            textCountdownStart.gameObject.SetActive(false);
        }

        private void TimerUpdate()
        {
            if (isTimerRunning && timeLeft > 0)
            {
                float now = Time.realtimeSinceStartup;
                timeLeft -= now - lastRealTime;
                if (timeLeft < 0) timeLeft = 0;

                SetTimerTransform(timeLeft, timeLimit);
            }
            lastRealTime = Time.realtimeSinceStartup;
        }
        public void StartTimer(float timeLeft, float timeLimit)
        {
            this.timeLeft = timeLeft; this.timeLimit = timeLimit;
            this.isTimerRunning = true;
        }
        public void StopTimer()
        {
            this.isTimerRunning = false;
        }
        public void ResetTimer()
        {
            this.isTimerRunning = false;
            if (timeLimit > 0) timeLeft = timeLimit;
            else { timeLeft = 5; timeLimit = 5; }
            SetTimerTransform(timeLeft, timeLimit);
        }
        private void SetTimerTransform(float timeLeft, float timeLimit)
        {
            if (timeLeft < 0) timeLeft = 0;

            timerTransform.sizeDelta = new Vector2(Screen.width*(timeLeft/timeLimit), timerTransform.sizeDelta.y);
            if (timeLeft < 1)
                timerTransform.GetComponent<Image>().color = new Color(0.8f, 0f, 0f);
            else if (timeLeft < 3)
                timerTransform.GetComponent<Image>().color = new Color(0.8f, 0.8f - (3-timeLeft)*0.4f, 0f);
            else if (timeLeft < 5)
                timerTransform.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f - (5-timeLeft)*0.4f);
            else
                timerTransform.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f);
        }

        public void CountdownToQuit(byte sec)
        {
            overlayBG.SetActive(true);
            textCountdownQuit.gameObject.SetActive(true);
            textCountdownQuit.text = sec.ToString();
        }
        #endregion



        #region Markers

        public void CreateCubeMarker(int team, string name)
        {
            // Create Marker
            CubeMarker marker = Instantiate(cubeMarkerPrefab, new Vector3(-10000, 0, 0), Quaternion.identity);
            marker.transform.SetParent(markersLayer, false);
            marker.SetLED(TeamColors[team]);
            marker.field = field;
            marker.UpdateSize();
            cubeMarkers.Add(marker);

            // Create Nameplate
            Nameplate plate = Instantiate(nameplatePrefab, new Vector3(-10000, 0, 0), Quaternion.identity);
            plate.transform.SetParent(nameplateLayer, false);
            plate.SetName(name);
            plate.marker = marker;
            nameplates.Add(plate);
        }

        public void ClearCubeMarkers()
        {
            foreach (var m in cubeMarkers)
                GameObject.Destroy(m.gameObject);
            cubeMarkers.Clear();

            markerPenDict.Clear();

            foreach (var n in nameplates)
                GameObject.Destroy(n.gameObject);
            nameplates.Clear();
        }

        public void MoveCubeMarker(int idx, int matX, int matY, int matDeg)
        {
            cubeMarkers[idx].SetPose(matX, matY, matDeg);
            if (drawable)
                field.Draw(matX, matY, 11, markerPenDict[idx], (byte)idx);
        }

        public void SetCubeMarkerStatus(int idx, GameCubeStatus status)
        {
            var plate = nameplates[idx];
            plate.SetStatus(status);
        }

        public void SetCubeMarkerPen(int idx, Color color)
        {
            if (!markerPenDict.ContainsKey(idx))
                markerPenDict.Add(idx, new SolidPen(color));
            else
                markerPenDict[idx] = new SolidPen(color);
        }

        public void SetCubeMarkerPen(int idx, Texture2D tex)
        {
            if (!markerPenDict.ContainsKey(idx))
                markerPenDict.Add(idx, new TexturePen(tex));
            else
                markerPenDict[idx] = new TexturePen(tex);
        }

        public Dictionary<int, float> GetMarkerIdxRatioDict()
        {
            Dictionary<int, float> res = new Dictionary<int, float>();
            for (int i = 0; i < cubeMarkers.Count; i++)
            {
                float ratio = field.GetOccupancyByID((byte)i);
                res.Add(i, ratio);
            }
            return res;
        }

        public byte[] GetOccupancyMap()
        {
            return field.GetOccupancyMap();
        }

        #endregion



        #region UI Callback
        public void OnBtnBack()
        {
            Debug.Log("UIGame: OnBtnBack");

            Back();
        }
        #endregion


    }


}