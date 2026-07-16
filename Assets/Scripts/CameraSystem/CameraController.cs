using System;
using Cinemachine;
using UnityEngine;

namespace CameraSystem
{
    [RequireComponent(typeof(CameraFreezeController))]
    [RequireComponent(typeof(CameraShakeController))]
    [RequireComponent(typeof(CameraZoomController))]
    public class CameraController : MonoBehaviour, ICamera
    {
        //IDEAS: A few ideas for consideration to add to this.
        // - Be able to control the magnitude of the shake. A harder hitting move should shake the screen harder.
        // - To simulate hard hitting moves we could also do what smash ult does with a quick zoom in during the hit pause before zooming back out.
        // - Experiment with shake direction being based on direction of hit
        // - Will need to add an observer componenet that can track what is in frame to better run the zoom effects

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private CinemachineVirtualCamera followVCam;
        [SerializeField] private CinemachineVirtualCamera freezeVCam;
        [SerializeField] private CameraConfig config;

        private CameraFreezeController freezeController;
        private CameraShakeController shakeController;
        private CameraZoomController zoomController;

        private void Awake()
        {
            if (player == null)
                Debug.LogError("CameraController needs a player reference!", gameObject);

            if (config == null)
                Debug.LogError("CameraController needs a CameraConfig!", gameObject);

            if (followVCam == null)
                Debug.LogError("CameraController needs Follow VCam reference!", gameObject);

            if (freezeVCam == null)
                Debug.LogError("CameraController needs Freeze VCam reference!", gameObject);

            freezeController = GetComponent<CameraFreezeController>();
            freezeController.Initialize(followVCam, freezeVCam, config);

            shakeController = GetComponent<CameraShakeController>();
            shakeController.Initialize();

            zoomController = GetComponent<CameraZoomController>();
            zoomController.Initialize(followVCam, config);

            if (followVCam != null && player != null && followVCam.Follow == null)
                followVCam.Follow = player;

            freezeController.FreezeStartedEvent += OnFreezeStarted;
            freezeController.FreezeEndedEvent += OnFreezeEnded;
        }

        private void OnDestroy()
        {
            freezeController.FreezeStartedEvent -= OnFreezeStarted;
            freezeController.FreezeEndedEvent -= OnFreezeEnded;
        }
        
        public void SimulateHit()
        {
            shakeController.Shake(config.ShakeMagnitude);
            Freeze();
        }

        private void Freeze()
        {
            freezeController.Freeze();
        }
        
        private void Unfreeze()
        {
            freezeController.Unfreeze();
        }

        private void OnFreezeStarted()
        {
            zoomController.SetHold(true);
        }

        private void OnFreezeEnded()
        {
            zoomController.SetHold(false);
        }
    }
}
