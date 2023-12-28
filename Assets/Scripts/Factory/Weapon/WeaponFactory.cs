using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponFactory : MonoBehaviour
{
    public static WeaponFactory instance;

    [Header("Container Prefanbs")]
    [SerializeField] private GameObject weaponContainer;

    [Header("Weapon Prefabs")]
    [SerializeField] private GameObject sword;
    [SerializeField] private GameObject bow;
    [SerializeField] private GameObject spear;
    [SerializeField] private GameObject hammer;
    [SerializeField] private GameObject axe;
    [SerializeField] private GameObject crossbow;
    [SerializeField] private GameObject pistol;

    [Header("Data")]
    [SerializeField] private WeaponCollectionData swordData;

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


    private GameObject SpawnWeapon(GameObject weapon)
    {
        return Instantiate(weapon);
    }


    private void ApplyNewWeaponData(GameObject weaponGO, WeaponCollectionData weaponData)
    {
        if (weaponGO.TryGetComponent(out Weapon weapon))
        {
            //Randomize attack data
            WeaponData attackData = new WeaponData (
                    weaponData.GetRandomUpTilt(),
                    weaponData.GetRandomUpAir(),
                    weaponData.GetRandomDownTilt(),
                    weaponData.GetRandomDownAir(),
                    weaponData.GetRandomForwardTilt(),
                    weaponData.GetRandomForwardAir(),
                    weaponData.GetRandomBackAir());

            //Get attack points
            List<GameObject> attackPoints = new List<GameObject>();
            foreach (AttackPoint attackPoint in weaponGO.GetComponentsInChildren<AttackPoint>())
            {
                attackPoints.Add(attackPoint.gameObject);
            }

            //Create attack collection
            AttackCollection attackCollection = new AttackCollection(attackData, attackPoints);

            weapon.AttackCollection = attackCollection;
        }
    }


    private void SetContainer(GameObject weapon)
    {
        GameObject container = Instantiate(weaponContainer);
        weapon.transform.SetParent(container.transform, true);      

        //TODO: This is temp. Will set position at a later point
        container.transform.localPosition = new Vector3(0, 1f, 0);
    }

  
    public void SpawnSword()
    {
        GameObject spawnedSword = SpawnWeapon(sword);
        ApplyNewWeaponData(spawnedSword, swordData );
        SetContainer(spawnedSword);
    }

}
