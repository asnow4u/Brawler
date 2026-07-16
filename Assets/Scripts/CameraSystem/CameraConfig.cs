using UnityEngine;

namespace CameraSystem
{
    /// <summary>
    /// Tuning knobs for the camera system. SerializeField while tuning; values that
    /// settle migrate to consts in their owning controller.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Camera System/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [Header("Shake")]
        [Tooltip("Impulse velocity magnitude on hit.")]
        [SerializeField] private float shakeMagnitude = 0.4f;

        [Header("Zoom")]
        [Tooltip("Camera distance multiplier at zoom extent 0 (calm).")]
        [SerializeField] private float zoomInMultiplier = 0.95f;
        [Tooltip("Camera distance multiplier at zoom extent 1. Keep the band tight (~10-15%).")]
        [SerializeField] private float zoomOutMultiplier = 1.12f;
        [Tooltip("SmoothDamp time for the zoom response, in seconds.")]
        [SerializeField] private float zoomResponseTime = 0.6f;

        public float ShakeMagnitude => shakeMagnitude;
        public float ZoomInMultiplier => zoomInMultiplier;
        public float ZoomOutMultiplier => zoomOutMultiplier;
        public float ZoomResponseTime => zoomResponseTime;
    }
}
