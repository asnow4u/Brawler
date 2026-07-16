using System;
using System.Collections;
using Cinemachine;
using UnityEngine;

namespace CameraSystem
{    
    public class CameraFreezeController : MonoBehaviour
    {
        private CinemachineBrain brain;
        private CinemachineVirtualCamera followVCam;
        private CinemachineVirtualCamera frozenVCam;
        private CameraConfig config;
        
        private const int frozenPriorityOffset = 10; // NOTE: Make sure this has higher priority than the followVCam

        public bool IsFrozen { get; private set; }

        public event Action FreezeStartedEvent;
        public event Action FreezeEndedEvent;

        private CinemachineBlendDefinition storedDefaultBlend;
        private bool blendSwapPending;
        private Coroutine restoreBlendRoutine;

        public void Initialize(CinemachineVirtualCamera followVCam, CinemachineVirtualCamera frozenVCam, CameraConfig config)
        {
            brain = GetComponentInChildren<CinemachineBrain>();
            if (brain == null)
                Debug.LogError("CameraFreezeController requiers a CinemachineBrain reference", gameObject);

            this.followVCam = followVCam;
            this.frozenVCam = frozenVCam;
            this.config = config;

            frozenVCam.m_Priority = followVCam.m_Priority + frozenPriorityOffset;
            frozenVCam.gameObject.SetActive(false);
        }
        
        public void Freeze()
        {
            if (IsFrozen)
                return;

            SnapFrozenVCamToLivePose();
            
            storedDefaultBlend = brain.m_DefaultBlend;
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);
            blendSwapPending = true;

            frozenVCam.gameObject.SetActive(true);
            IsFrozen = true;

            restoreBlendRoutine = StartCoroutine(RestoreDefaultBlendAfterCut());

            FreezeStartedEvent?.Invoke();
        }

        public void Unfreeze()
        {
            if (!IsFrozen)
                return;            

            RestoreDefaultBlend();

            frozenVCam.gameObject.SetActive(false);
            IsFrozen = false;
            FreezeEndedEvent?.Invoke();
        }

        private void SnapFrozenVCamToLivePose()
        {
            CameraState state = brain.CurrentCameraState;
            frozenVCam.transform.SetPositionAndRotation(state.FinalPosition, state.FinalOrientation);
            frozenVCam.m_Lens = state.Lens;
        }

        private IEnumerator RestoreDefaultBlendAfterCut()
        {
            //Wait for the brain to latch the cut before putting the blend back
            yield return null;
            yield return null;
            restoreBlendRoutine = null;
            RestoreDefaultBlend();
        }

        private void RestoreDefaultBlend()
        {
            if (!blendSwapPending)
                return;

            if (restoreBlendRoutine != null)
            {
                StopCoroutine(restoreBlendRoutine);
                restoreBlendRoutine = null;
            }

            brain.m_DefaultBlend = storedDefaultBlend;
            blendSwapPending = false;
        }
    }
}
