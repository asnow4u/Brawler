using Cinemachine;
using UnityEngine;

namespace CameraSystem
{    
    public class CameraZoomController : MonoBehaviour
    {
        private CinemachineVirtualCamera followVCam;
        private CameraConfig config;

        private CinemachineFramingTransposer framingTransposer;
        private IZoomExtentProvider extentProvider;

        private float baseDistance;
        private float zoomVelocity;
        private bool hold;

        public void Initialize(CinemachineVirtualCamera followVCam, CameraConfig config)
        {
            this.followVCam = followVCam;
            this.config = config;

            framingTransposer = followVCam.GetCinemachineComponent<CinemachineFramingTransposer>();
            extentProvider = GetComponent<IZoomExtentProvider>();
            baseDistance = framingTransposer.m_CameraDistance;
        }
        
        public void SetHold(bool value)
        {
            hold = value;
            if (!value)
                zoomVelocity = 0f;
        }

        private void LateUpdate()
        {
            if (hold || framingTransposer == null || extentProvider == null)
                return;

            float extent = Mathf.Clamp01(extentProvider.GetZoomExtent());
            float targetDistance = baseDistance * Mathf.Lerp(config.ZoomInMultiplier, config.ZoomOutMultiplier, extent);
            framingTransposer.m_CameraDistance = Mathf.SmoothDamp(framingTransposer.m_CameraDistance, targetDistance, ref zoomVelocity, config.ZoomResponseTime);
        }
    }
}
