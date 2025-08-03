using Game.SceneObjects.Attack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interactable.Factory
{
    public class WeaponFactory : MonoBehaviour
    {
        public static WeaponFactory instance;

        [Header("Container Prefanbs")]
        [SerializeField] private GameObject weaponContainer;

        [Header("Sword")]
        [SerializeField] private GameObject sword;
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
                WeaponData attackData = new WeaponData(
                        weaponData.GetRandomUpTiltAttack(),
                        weaponData.GetRandomUpAirAttack(),
                        weaponData.GetRandomDownTiltAttack(),
                        weaponData.GetRandomDownAirAttack(),
                        weaponData.GetRandomForwardTiltAttack(),
                        weaponData.GetRandomForwardAirAttack());

                //Get attack points
                List<GameObject> attackPoints = new List<GameObject>();
                foreach (AttackDamageCollider attackPoint in weaponGO.GetComponentsInChildren<AttackDamageCollider>())
                {
                    attackPoints.Add(attackPoint.gameObject);
                }

                //Create attack collection
                AttackCollection attackCollection = new AttackCollection(attackData);
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
            ApplyNewWeaponData(spawnedSword, swordData);
            SetContainer(spawnedSword);
        }

    }
}
