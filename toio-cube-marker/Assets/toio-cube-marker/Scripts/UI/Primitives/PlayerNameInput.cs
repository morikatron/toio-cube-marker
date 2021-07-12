using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace CubeMarker
{

    [RequireComponent(typeof(TMP_InputField))]
    public class PlayerNameInput : MonoBehaviour
    {
        const string playerNamePrefKey = "PlayerName";


        void Start()
        {
            TMP_InputField _inputField = this.GetComponent<TMP_InputField>();
            if (_inputField!=null)
            {
                if (PlayerPrefs.HasKey(playerNamePrefKey))
                {
                    _inputField.text = PlayerPrefs.GetString(playerNamePrefKey);
                }
            }

        }

    }

}
