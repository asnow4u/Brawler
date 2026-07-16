using UnityEngine;

namespace CameraSystem
{
    public class PlayerMotionExtentProvider : MonoBehaviour, IZoomExtentProvider
    {
        [SerializeField] private Transform player;

        [Header("Tuning")]
        [SerializeField] private float speedForMaxExtent = 14f;
        [SerializeField] private float verticalWeight = 1.5f;
        [SerializeField] private float smoothingTime = 0.35f;

        private Vector3 lastPosition;
        private float smoothedExtent;
        private float extentVelocity;
        private bool hasLastPosition;

        public float GetZoomExtent() => smoothedExtent;

        private void LateUpdate()
        {
            if (player == null || Time.deltaTime <= 0f)
                return;

            if (!hasLastPosition)
            {
                lastPosition = player.position;
                hasLastPosition = true;
                return;
            }

            Vector3 velocity = (player.position - lastPosition) / Time.deltaTime;
            lastPosition = player.position;

            float rawExtent = Mathf.Clamp01((Mathf.Abs(velocity.x) + Mathf.Abs(velocity.y) * verticalWeight) / speedForMaxExtent);
            smoothedExtent = Mathf.SmoothDamp(smoothedExtent, rawExtent, ref extentVelocity, smoothingTime);
        }
    }
}
