using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponFactory : MonoBehaviour
{
    public static WeaponFactory instance;

    [SerializeField] private GameObject sword;
    [SerializeField] private SwordData swordData;

    [SerializeField] private GameObject bow;
    [SerializeField] private GameObject spear;
    [SerializeField] private GameObject hammer;
    [SerializeField] private GameObject axe;
    [SerializeField] private GameObject crossbow;
    [SerializeField] private GameObject pistol;

    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
