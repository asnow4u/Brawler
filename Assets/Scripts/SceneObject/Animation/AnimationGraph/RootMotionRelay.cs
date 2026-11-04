using System;
using UnityEngine;

/// <summary>Receives root motion from the Animator and raises it as a velocity. Implementing OnAnimatorMove stops Unity from applying root motion to this transform.</summary>
[RequireComponent(typeof(Animator))]
internal class RootMotionRelay : MonoBehaviour
{
    private Animator animator;

    public event Action<Vector3> RootMotionEvaluatedEvent;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnAnimatorMove()
    {
        RootMotionEvaluatedEvent?.Invoke(animator.velocity);
    }
}
