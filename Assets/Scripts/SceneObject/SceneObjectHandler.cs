using UnityEngine;

namespace Game.SceneObjects 
{
    public abstract class SceneObjectHandler : MonoBehaviour
    {
        protected SceneObject sceneObject => GetComponent<SceneObject>();

        public abstract void RegisterToEvents();
        public abstract void UnregisterToEvents();

        public abstract void Setup();
    }
}
