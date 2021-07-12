using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;


namespace CubeMarker
{
    public static class CustomQuizDatabase
    {
        public static Dictionary<string, string> dict = new Dictionary<string, string>();
        public static Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
    }

    public class UICustomQuiz : MonoBehaviour
    {
        public GameObject quizListItemPrefab;
        public Transform quizListContent;
        public ImageViewer preview;
        public Dictionary<GameObject, string> objNameDict = new Dictionary<GameObject, string>();
        public Dictionary<GameObject, string> objURLDict = new Dictionary<GameObject, string>();


        public void SetActive(bool value)
        {
            if (gameObject.activeSelf == value) return;
            this.gameObject.SetActive(value);

            if (value) {}
        }


        void LoadImage(string url)
        {
            StartCoroutine(IE_LoadImage(url));
        }

        IEnumerator IE_LoadImage(string url)
        {
            Debug.Log("load " + url);
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
                Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
                preview.SetTexture(tex);
            }
        }



        private void OnURLInput(GameObject item, string url)
        {
            // Trim
            url = url.Trim();
            item.transform.Find("InputURL").GetComponent<TMP_InputField>().text = url;

            // Preview
            LoadImage(url);

            objURLDict[item] = url;
        }
        private void OnNameInput(GameObject item, string name)
        {
            // Trim
            var inputName = item.transform.Find("InputName");
            name = name.Trim();
            inputName.GetComponent<TMP_InputField>().text = name;

            // Check existed
            bool existed = false;
            foreach (var obj in objNameDict.Keys)
            {
                if (obj!=item && objNameDict[obj]==name)
                {
                    existed = true; break;
                }
            }

            // Set Color
            if (existed || name == "") inputName.GetComponent<Image>().color = new Color32(255, 185, 185, 255);
            else inputName.GetComponent<Image>().color = Color.white;

            objNameDict[item] = name;
        }
        private void OnBtnDel(GameObject item)
        {
            objNameDict.Remove(item);
            objURLDict.Remove(item);
            Destroy(item);
        }

        public void OnBtnNew()
        {
            var item = Instantiate(quizListItemPrefab);
            item.transform.SetParent(quizListContent, false);

            item.transform.Find("InputURL").GetComponent<TMP_InputField>().onEndEdit.AddListener((url)=>OnURLInput(item, url));
            item.transform.Find("InputName").GetComponent<TMP_InputField>().onEndEdit.AddListener((name)=>OnNameInput(item, name));
            item.GetComponentInChildren<Button>().onClick.AddListener(()=>OnBtnDel(item));

            objNameDict.Add(item, "");
            objURLDict.Add(item, "");

            // Set name field red
            item.transform.Find("InputName").GetComponent<Image>().color = new Color32(255, 185, 185, 255);
        }
        public void OnBtnBack()
        {
            CustomQuizDatabase.dict.Clear();

            foreach (var obj in objNameDict.Keys)
            {
                var name = objNameDict[obj];
                var url = objURLDict[obj];
                if (name != "" && url != "" && !CustomQuizDatabase.dict.ContainsKey(name) && !CustomQuizDatabase.dict.ContainsValue(url))
                {
                    CustomQuizDatabase.dict.Add(name, url);
                }
            }

            SetActive(false);
        }
    }

}

