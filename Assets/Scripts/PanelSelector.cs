using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelSelector : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> Panels;

    public void ChoosePanel(int index) {
        for (int i = 0; i < Panels.Count; i++) {
            Panels[i].SetActive((i == index));
        }
    }
}
