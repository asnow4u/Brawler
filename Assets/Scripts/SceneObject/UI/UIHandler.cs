using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageDisplay;

    #region Initialize
    
    public void Setup()
    {
        Debug.Assert(damageDisplay != null, "Damage display not hooked up.", gameObject);
    }


    public void Initialize()
    {        

    }

    #endregion

    public void RotateDisplayText()
    {
        damageDisplay.transform.Rotate(transform.up, 180f);
    }


    public void UpdateDamageDisplay(float damageValue)
    {
        damageDisplay.text = (Mathf.Round(damageValue * 10f) * 0.1f).ToString();   
    }
}
