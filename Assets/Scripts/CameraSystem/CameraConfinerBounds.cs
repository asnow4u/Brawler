using Cinemachine;
using UnityEngine;

namespace CameraSystem
{    
    [RequireComponent(typeof(PolygonCollider2D))]
    public class CameraConfinerBounds : MonoBehaviour
    {
        [SerializeField] private CinemachineConfiner2D confiner;

        private void Awake()
        {
            ApplyBounds();
        }

        public void ApplyBounds()
        {
            PolygonCollider2D boundsCollider = GetComponent<PolygonCollider2D>();

            if (confiner != null)
            {
                confiner.m_BoundingShape2D = boundsCollider;
                confiner.InvalidateCache();
            }
        }
    }
}
