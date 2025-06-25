using UnityEngine;
using Game.SceneObjects;
using System;

namespace Game.Interactable
{
    public abstract class Interactable : MonoBehaviour, IInteractable
    {
        private Collider interactionCollider;

        protected virtual void Awake()
        {
            if (!TryGetComponent(out interactionCollider))
                Debug.LogException(new MissingComponentException("Interactable " + name + " must have a collider"), this);
        }


        public void EnableInteraction()
        {
            interactionCollider.enabled = true;
        }


        public void DisableInteraction()
        {
            interactionCollider.enabled = false;
        }
    }
}
