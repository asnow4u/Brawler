using Cinemachine;
using UnityEngine;

namespace CameraSystem
{    
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public class CameraShakeController : MonoBehaviour
    {
        [SerializeField] private Vector3 shakeDirection = new Vector3(0.2f, -1f, 0f);
        
        private CinemachineImpulseSource impulseSource;        

        public void Initialize()
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null)
                Debug.LogError("CameraShake needs CinemachineImpulseSource reference");
        }

        public void Shake(float magnitude)
        {
            impulseSource.GenerateImpulseWithVelocity(shakeDirection.normalized * magnitude);
        }
    }
}
