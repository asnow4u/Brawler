using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Interactable.Factory
{
    public class PortalFactory : MonoBehaviour
    {
        [SerializeField] private List<LevelPortal> portals;


        public void SpawnRandomLevelPortal(Vector3 pos)
        {
            List<LevelPortal> possibleDestinations = new List<LevelPortal>();

            //TODO: Track previous levels?
            //Grab possible levels
            foreach (LevelPortal portal in portals)
            {
                if (portal.Destination != GameManager.instance.CurLevelType)
                {
                    possibleDestinations.Add(portal);
                }
            }

            int rand = Random.Range(0, possibleDestinations.Count);
            Instantiate(portals[rand], pos, Quaternion.identity);
        }
    }
}
