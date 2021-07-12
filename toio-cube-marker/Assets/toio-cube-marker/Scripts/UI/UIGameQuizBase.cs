using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.Networking;



namespace CubeMarker
{
    using static PUNProtocolUtils;

    public class UIGameQuizBase : MonoBehaviour, IUIGameQuiz
    {
        public static readonly float buttonAnswerXOffset = 185;
        public static readonly float buttonAnswerYOffset = -108;
        public static readonly float buttonAnswerXDelta = 327;
        public static readonly float buttonAnswerYDelta = -126;
        public static readonly int buttonAnswerRows = 6;
        public static readonly int buttonAnswerCols = 4;

        public UIGame ui;

        [Header("Game Impl")]
        public GameQuizClient gameClient;
        public GameQuizHost gameHost;

        [Header("UI to answer")]
        public GameObject buttonToAnswer;
        public GameObject overlayAnswer;
        public GameObject buttonAnswerPrefab;

        [Header("UI showing answers")]
        public GameObject answerSlot0;
        public GameObject answerSlot1;
        public GameObject answerSlot2;
        public GameObject answerSlot3;
        [Header("UI for results")]
        public GameObject overlayResult;


        protected List<GameObject> buttonAnswerObjs = new List<GameObject>();
        protected List<string> textureNames = new List<string>();
        protected Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
        protected Dictionary<ActualPlayerID, GameObject> pidSlotDict = new Dictionary<ActualPlayerID, GameObject>();
        protected bool answerable = false;


        private bool active = false;
        public void SetActive(bool value)
        {
            if (active == value && this.gameObject.activeSelf == value) return;
            this.gameObject.SetActive(value);
            active = value;

            if (value)
            {
                Clear();
                InitButtonToAnswer();
                InitAnswerSlots();

                gameClient.ui = ui;
                gameClient.uiq = this;
                gameHost.ui = ui;
                gameHost.uiq = this;
                gameHost.EnterGame();
                gameClient.EnterGame();

            }
            else
            {
                gameHost.StopGame();
                gameClient.StopGame();
                Clear();
            }
        }

        public void Clear()
        {
            answerable = false;

            overlayResult.SetActive(false);
            overlayAnswer.SetActive(false);

            // Clear button answers
            foreach (var obj in buttonAnswerObjs) Destroy(obj);
            buttonAnswerObjs.Clear();

            // Hide all answer slots
            pidSlotDict.Clear();
            for (int j=0; j<4; j++) GetAnswerSlotByIdx(j).SetActive(false);

            ClearTextures();
        }


        protected virtual bool IsTeamIdxEraser(byte teamIdx) { return false; }


        #region ====== Init Impl ======

        protected virtual void InitAnswerPanel()
        {
            int row = 0; int col = 0;
            foreach (var texName in textureNames)
            {
                float x = buttonAnswerXOffset + buttonAnswerXDelta * col;
                float y = buttonAnswerYOffset + buttonAnswerYDelta * row;

                GameObject obj = Instantiate(buttonAnswerPrefab, Vector3.zero, Quaternion.identity);
                obj.transform.SetParent(overlayAnswer.transform, false);
                obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
                obj.GetComponentInChildren<TMP_Text>().text = texName.Replace(".", "\n");
                obj.GetComponentInChildren<Button>().onClick.AddListener( () => {OnButtonAnswer(texName);} );

                buttonAnswerObjs.Add(obj);

                // Iteration
                col ++;
                row += col / buttonAnswerCols;
                col = col % buttonAnswerCols;
                if (row >= buttonAnswerRows) break;
            }
        }

        protected virtual void InitButtonToAnswer()
        {
            buttonToAnswer.GetComponentInChildren<Button>().interactable = true;
        }

        protected virtual void InitAnswerSlots()
        {
            int i = 0;
            foreach (var pid in NetworkManager.pidTeamIdxDict.Keys)
            {
                var teamIdx = NetworkManager.pidTeamIdxDict[pid];
                if (!IsTeamIdxEraser(teamIdx) && pid.LocalNumber==0)
                {
                    var slot = GetAnswerSlotByIdx(i);
                    slot.SetActive(true);
                    pidSlotDict.Add(pid, slot);
                    string name = NetworkManager.GetAcutalPlayerName(pid);
                    slot.transform.Find("TextName").GetComponent<TMP_Text>().text = name;
                    slot.transform.Find("TextAnswer").GetComponent<TMP_Text>().text = "";
                    i++;
                }
            }
        }

        #endregion


        public Dictionary<string, string> GetLoadableTextures()
        {
            string quizSetting = GetRoomQuizSetting();
            System.Random rnd = new System.Random();
            int maxAnswers = buttonAnswerRows * buttonAnswerCols;

            if (quizSetting == "Custom")
            {
                var texs = CustomQuizDatabase.dict;
                int nsamples = Mathf.Min(maxAnswers, texs.Count);
                var sampled = texs.OrderBy(kv => rnd.Next()).Take(nsamples).OrderBy(kv => kv.Key);

                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach (var kv in sampled)
                    dict.Add(kv.Key, kv.Value);
                return dict;
            }
            else
            {
                var texs = Resources.LoadAll<Texture2D>("Quiz_Images\\" + quizSetting);

                int nsamples = Mathf.Min(maxAnswers, texs.Length);
                var sampled = texs.OrderBy(item => rnd.Next()).Take(nsamples).OrderBy(item => item.name);

                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach (var tex in sampled)
                    dict.Add(tex.name, null);
                return dict;
            }
        }

        public void SetupQuiz(List<string> allNames, Dictionary<string, string> nameURLs, Dictionary<int, string> cubeIdxName)
        {
            ClearTextures();

            string quizSetting = GetRoomQuizSetting();

            // Set Names
            textureNames = allNames;
            InitAnswerPanel();

            // Load, Set Textures
            if (quizSetting == "Custom")
            {
                StartCoroutine(IE_LoadSetCustomTextures(nameURLs, cubeIdxName));
            }
            else
            {
                // Load
                var texs = Resources.LoadAll<Texture2D>("Quiz_Images\\" + quizSetting);
                for (int i = 0; i < texs.Length; i++)
                    loadedTextures.Add(texs[i].name, texs[i]);

                // Set
                foreach (var cubeIdx in cubeIdxName.Keys)
                {
                    var name = cubeIdxName[cubeIdx];
                    if (loadedTextures.ContainsKey(name)) ui.SetCubeMarkerPen(cubeIdx, loadedTextures[name]);
                    else Debug.LogWarning("loadedTextures does not contain " + name);
                }
            }

        }

        private IEnumerator IE_LoadSetCustomTextures(Dictionary<string, string> nameURLs, Dictionary<int, string> cubeIdxName)
        {
            string quizSetting = GetRoomQuizSetting();

            // Load Textures
            foreach (var name in nameURLs.Keys)
            {
                var url = nameURLs[name];
                var cache = CustomQuizDatabase.cache;
                // Load from cache if existed
                if (cache.ContainsKey(url))
                {
                    var tex = CustomQuizDatabase.cache[url];
                    loadedTextures.Add(name, tex);
                }
                // Download
                else
                {
                    #if !UNITY_EDITOR && UNITY_WEBGL
                        UnityWebRequest www = UnityWebRequestTexture.GetTexture("https://ai.dev.morikatron.net/tcmsim/corsskip.php?url=" + url);
                    #else
                        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
                    #endif
                    yield return www.SendWebRequest();
                    if (www.isNetworkError || www.isHttpError)
                    {
                        Debug.LogWarning(www.error);
                    }
                    else
                    {
                        var tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
                        loadedTextures.Add(name, tex);
                        cache.Add(url, tex);
                    }
                }
            }

            // Set Pens
            foreach (var cubeIdx in cubeIdxName.Keys)
            {
                var name = cubeIdxName[cubeIdx];
                if (loadedTextures.ContainsKey(name)) ui.SetCubeMarkerPen(cubeIdx, loadedTextures[name]);
                else Debug.LogWarning("loadedTextures does not contain " + name);
            }
        }



        protected Action<string> answerCallback = null;
        public void SetAnswerCallback(Action<string> callback)
        {
            answerCallback = callback;
        }
        public void SetAnswerable(bool value)
        {
            answerable = value;
        }
        public void SetAnswerSlotAnswer(ActualPlayerID pid, string ans)
        {
            GameObject slot = pidSlotDict[pid];
            slot.transform.Find("TextAnswer").GetComponent<TMP_Text>().text = ans;
        }

        public virtual void ShowResult(Dictionary<byte, string> teamIdxTexNameDict, Dictionary<ActualPlayerID, string> pidAnsDict, Dictionary<ActualPlayerID, float> pidTimeDict=null) {}



        #region ====== UI Utils ======

        protected void ClearTextures()
        {
            textureNames.Clear();
            loadedTextures.Clear();

            foreach (var obj in buttonAnswerObjs) Destroy(obj);
            buttonAnswerObjs.Clear();
        }

        protected GameObject GetAnswerSlotByIdx(int idx)
        {
            switch (idx)
            {
                case 1: return answerSlot1;
                case 2: return answerSlot2;
                case 3: return answerSlot3;
                default: case 0: return answerSlot0;
            }
        }

        #endregion



        #region ====== Button Callbacks ======
        public void OnButtonToAnswer()
        {
            // Switch Answer Panel
            overlayAnswer.SetActive(!overlayAnswer.activeSelf);
        }

        private void OnButtonAnswer(string name)
        {
            if (!answerable) return;

            // Send answer
            answerCallback?.Invoke(name);

            // Close answer panel
            overlayAnswer.SetActive(false);
            // Disable to answer button
            buttonToAnswer.GetComponentInChildren<Button>().interactable = false;
        }
        #endregion


    }

}

