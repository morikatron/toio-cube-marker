using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace CubeMarker
{
    public class UICubeInfo : MonoBehaviour
    {
        public RawImage led;
        public TMP_Text textState;
        public TMP_Text textPos;



        public void SetActive(bool value)
        {
            this.gameObject.SetActive(value);
        }

        public void SetLEDColor(Color color)
        {
            led.color = color;
        }

        public void SetStateDetected()
        {
            textState.text = "Detected";
            textState.color = Color.grey;
        }
        public void SetStateConnecting()
        {
            textState.text = "Connecting";
            textState.color = Color.yellow;
        }
        public void SetStateConnected()
        {
            textState.text = "Connected";
            textState.color = Color.green;
        }
        public void SetPos(float x, float y, float deg)
        {
            textPos.text = "(" + (int)x + ", " + (int)y + ", " + (int)deg + ")";
        }

    }

}
