using UnityEngine;
using UnityEngine.UI;



namespace CubeMarker
{

    public class CubeMarker : MonoBehaviour
    {
        public RawImage led;
        public Field field { get; set; }


        void Start()
        {
            
        }

        void Update()
        {
            
        }


        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }

        public void UpdateSize()
        {
            float w = field.GetCubeWidthUI();
            GetComponent<RectTransform>().localScale = new Vector3(w/100, w/100, 1);
        }

        public void SetLED(Color color)
        {
            led.color = color;
        }

        public void SetPose(int matX, int matY, int matDeg)
        {
            (Vector3 loc, Vector3 eulers) = field.CvtCoordsMat2UI(matX, matY, matDeg);
            GetComponent<RectTransform>().anchoredPosition = loc;
            GetComponent<RectTransform>().eulerAngles = eulers;
        }

        public void SetStatus(GameCubeStatus status, float duration)
        {

        }

        public Vector3 GetUIPos()
        {
            return GetComponent<RectTransform>().anchoredPosition;
        }

    }

}
