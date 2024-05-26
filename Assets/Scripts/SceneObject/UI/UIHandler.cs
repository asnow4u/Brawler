using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
    private TextMeshProUGUI damageDisplay;


    public void Initialize()
    {
        TextMeshProUGUI[] textMeshs = GetComponentsInChildren<TextMeshProUGUI>();

        foreach (TextMeshProUGUI textMesh in textMeshs)
        {
            if (textMesh.gameObject.tag == "UIDamageDisplay")
                damageDisplay = textMesh;
        }       
    }


    public void RotateDisplayText()
    {
        damageDisplay.transform.Rotate(transform.up, 180f);
    }


    public void UpdateDamageDisplay(float damageValue)
    {
        damageDisplay.text = (Mathf.Round(damageValue * 10f) * 0.1f).ToString();   
    }
}
