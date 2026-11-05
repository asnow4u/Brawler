using UnityEngine;

/// <summary>Implementing OnAnimatorMove stops Unity from applying root motion to this transform. Root motion is read by AnimationHandler after each graph evaluation.</summary>
[RequireComponent(typeof(Animator))]
internal class RootMotionRelay : MonoBehaviour
{
    private void OnAnimatorMove() { }
}
