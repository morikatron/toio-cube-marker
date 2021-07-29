using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace CubeMarker
{

    public class RoomAreaTeam : MonoBehaviour
    {
        public readonly int MAX_SLOTS = 4;

        private List<ActualPlayerID> pids = new List<ActualPlayerID>();

        internal Action<ActualPlayerID> deleteCallback = null;


        private Transform GetSlot(int idx)
        {
            return transform.Find("Slot"+idx);
        }

        public bool Has(ActualPlayerID pid)
        {
            return pids.Contains(pid);
        }

        public bool IsFull()
        {
            return pids.Count == MAX_SLOTS;
        }

        public void AddPlayer(ActualPlayerID pid, string name, bool ready=false, bool host=false, bool self=false)
        {
            if (!pids.Contains(pid) && pids.Count<MAX_SLOTS)
            {
                AssignSlot(pids.Count, pid, name, ready, host, self);
                pids.Add(pid);
            }
        }

        public void SetPlayer(ActualPlayerID pid, string name, bool ready=false, bool host=false, bool self=false)
        {
            if (pids.Contains(pid))
            {
                AssignSlot(pids.IndexOf(pid), pid, name, ready, host, self);
            }
        }

        public void RemovePlayer(ActualPlayerID pid)
        {
            if (pids.Contains(pid))
            {
                int idx = pids.IndexOf(pid);
                pids.Remove(pid);

                for (int i=idx; i<pids.Count; i++)
                {
                    CopySlot(i+1, i);
                }

                GetSlot(pids.Count).gameObject.SetActive(false);
            }
        }

        public void ShowButtonAddAI(bool show)
        {
            transform.Find("AreaAddAI").gameObject.SetActive(show);
        }

        public void ShowButtonDelete(bool show)
        {
            GetSlot(0).Find("ButtonDelMask").gameObject.SetActive(!show);
            GetSlot(1).Find("ButtonDelMask").gameObject.SetActive(!show);
            GetSlot(2).Find("ButtonDelMask").gameObject.SetActive(!show);
            GetSlot(3).Find("ButtonDelMask").gameObject.SetActive(!show);
        }

        public void EnableButtonJoin(bool enabled)
        {
            transform.Find("ButtonJoin").GetComponent<Button>().interactable = enabled;
        }

        public void Clear()
        {
            pids.Clear();
            GetSlot(0).gameObject.SetActive(false);
            GetSlot(1).gameObject.SetActive(false);
            GetSlot(2).gameObject.SetActive(false);
            GetSlot(3).gameObject.SetActive(false);
            ShowButtonAddAI(false);
            ShowButtonDelete(false);
        }

        private void AssignSlot(int slotIdx, ActualPlayerID pid, string name, bool ready, bool host, bool self)
        {
            var slot = GetSlot(slotIdx);
            TMP_Text textName = slot.Find("TextName").GetComponent<TMP_Text>();
            textName.text = name;
            textName.fontStyle = self? FontStyles.Bold : FontStyles.Normal;
            slot.Find("Ready").gameObject.SetActive(ready && !host);
            slot.Find("Host").gameObject.SetActive(host);
            slot.gameObject.SetActive(true);

        }

        private void CopySlot(int fromIdx, int toIdx)
        {
            var slotF = GetSlot(fromIdx);
            var slotT = GetSlot(toIdx);
            TMP_Text textNameF = slotF.Find("TextName").GetComponent<TMP_Text>();
            TMP_Text textNameT = slotT.Find("TextName").GetComponent<TMP_Text>();
            textNameT.text = textNameF.text;
            textNameT.fontStyle = textNameF.fontStyle;
            slotT.Find("Ready").gameObject.SetActive(slotF.Find("Ready").gameObject.activeSelf);
            slotT.Find("Host").gameObject.SetActive(slotF.Find("Host").gameObject.activeSelf);
        }

        public void OnButtonDelete(int slotIdx)
        {
            if (slotIdx < pids.Count)
                deleteCallback?.Invoke(pids[slotIdx]);
        }

    }

}
