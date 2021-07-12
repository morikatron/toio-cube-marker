using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;


namespace CubeMarker
{

    [RequireComponent(typeof(TMP_Dropdown))]
    [DisallowMultipleComponent]
    public class DropdownExtend : MonoBehaviour, IPointerClickHandler
    {

        private TMP_Dropdown dropdown;
        internal List<int> indexesToDisable = new List<int>();
        internal string labelNoOption = "0";

        private void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Apply();
        }
        public void Apply()
        {
            var dplist = GetComponentInChildren<Canvas>();
            Toggle[] toggles = null;
            if (dplist)
            {
                toggles = dplist.GetComponentsInChildren<Toggle>(true);

                // Disable
                for (var i = 1; i < toggles.Length; i++)
                    toggles[i].interactable = !indexesToDisable.Contains(i - 1);
            }

            // Get first available option
            int idxAvailable = -1;
            for (var i = 0; i < dropdown.options.Count; i++)
                if (!indexesToDisable.Contains(i)) { idxAvailable = i; break; }

            if (idxAvailable == -1) // No available option
            {
                dropdown.captionText.text = labelNoOption;

                if (toggles != null) toggles[dropdown.value+1].isOn = false;
            }
            else if (!indexesToDisable.Contains(dropdown.value)) // selected option is available
            {
                dropdown.captionText.text = dropdown.options[dropdown.value].text;

                if (toggles != null) toggles[dropdown.value+1].isOn = true;
            }
            else    // selected option is inavailable
            {
                dropdown.value = idxAvailable;
            }
        }
    }

}
