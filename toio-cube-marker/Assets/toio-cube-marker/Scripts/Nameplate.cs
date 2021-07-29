using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace CubeMarker
{

    public class Nameplate : MonoBehaviour
    {
        public Vector3 locationOffset = new Vector3(0, 100, 0);
        public TMP_Text textName;
        public TMP_Text textStatus;
        public RawImage bgName;
        public RawImage bgStatus;

        internal CubeMarker marker = null;


        void Update()
        {
            if (marker != null)
            {
                GetComponent<RectTransform>().anchoredPosition = marker.GetUIPos() + locationOffset;
            }
        }

        public void SetName(string name)
        {
            textName.text = name;
            RectTransform bg = bgName.transform.GetComponent<RectTransform>();
            bg.sizeDelta = new Vector2( 25 * name.Length + 20, bg.sizeDelta.y);
        }

        public void SetStatus(GameCubeStatus status)
        {
            string text; Color32 color;
            switch (status)
            {
                case GameCubeStatus.SpeedUp : text = "SPD UP!"; color = new Color32(100, 255, 100, 250); break;
                case GameCubeStatus.SpeedDown : text = "SPD DOWN!"; color = new Color32(170, 100, 50, 250); break;
                case GameCubeStatus.Stagger : text = "Stagger!"; color = new Color32(200, 150, 80, 250); break;
                case GameCubeStatus.FreezeOthers : text = "Freeze!"; color = new Color32(180, 180, 230, 250); break;
                case GameCubeStatus.Reverse : text = "Reverse"; color = new Color32(100, 0, 200, 250); break;
                default : case GameCubeStatus.Normal : text = ""; color = new Color32(100, 100, 100, 0); break;
            }
            textStatus.text = text;
            textStatus.color = color;
            bgStatus.gameObject.SetActive(text != "");
        }
    }

}
