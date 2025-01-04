using Game.SceneObjects;
using TMPro;
using UnityEngine;


namespace Game.UI.SceneObject
{
    public class UIHandler : SceneObjectHandler
    {
        [SerializeField] private TextMeshProUGUI damageDisplay;

        #region Initialize
    
        public override void Setup()
        {
            Debug.Assert(damageDisplay != null, "Damage display not hooked up.", gameObject);
        }


        public override void RegisterToEvents()
        { }

        public override void UnregisterToEvents()
        { }

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
}
