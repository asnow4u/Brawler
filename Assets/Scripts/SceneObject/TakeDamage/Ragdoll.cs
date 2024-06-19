using RayAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



public struct RagdollBone
{
    public Vector3 Pos;
    public Quaternion Rot;

    public RagdollBone(Transform transform)
    {
        this.Pos = transform.position;
        this.Rot = transform.rotation;
    }
}


public class Ragdoll : MonoBehaviour
{
    private SceneObject sceneObject;    
    private Vector3 posOffset;
    
    private List<GameObject> ragdollParts = new List<GameObject>();

    //Exit transition
    private float exitTransitionTime = 1f;
    List<RagdollBone> ragdollStartTransform = new List<RagdollBone>();
    List<RagdollBone> ragdollEndTransform = new List<RagdollBone>();


    //Getter
    public List<GameObject> RagdollParts => ragdollParts;
    public Vector3 PosOffset => posOffset;
    public float ExitTransitionTime => exitTransitionTime;


    #region Initialize

    public void Initialize(SceneObject sceneObject)
    {
        this.sceneObject = sceneObject;
        
        posOffset = gameObject.transform.localPosition;

        ragdollParts.Add(gameObject);
        foreach (Joint joint in GetComponentsInChildren<Joint>())
        {
            joint.gameObject.layer = LayerMask.NameToLayer("Ragdoll");
            ragdollParts.Add(joint.gameObject);
        }

        this.enabled = false;
    }

    #endregion


    #region Enable / Disable

    private void OnEnable()
    {
        Debug.Log("RAGDOLL: Enabled");
        DisableSceneObjectComponents();
        EnableRagdollParts();
    }


    private async void OnDisable()
    {
        Debug.Log("RAGDOLL: Disabled");
        DisableRagdollParts();

        sceneObject.GetComponent<Collider>().enabled = true;

        await TransitionToAnimation();

        //EnableSceneObjectComponenets();
    }


    private void EnableSceneObjectComponenets()
    {
        if (sceneObject != null)
        {
            sceneObject.GetComponent<Collider>().enabled = true;   
            sceneObject.AnimationStateHandler.Animator.enabled = true;
        }        
    }


    private void DisableSceneObjectComponents()
    {
        if (sceneObject != null)
        {
            sceneObject.GetComponent<Collider>().enabled = false;
            sceneObject.AnimationStateHandler.Animator.enabled = false;
        }
    }


    private void EnableRagdollParts()
    {
        foreach (GameObject part in ragdollParts)
        {
            if (part.TryGetComponent(out Collider collider))
                collider.isTrigger = false;

            if (part.TryGetComponent(out Rigidbody rb))
            {
                rb.useGravity = true;
            }
        }
    }


    private void DisableRagdollParts()
    {
        foreach (GameObject part in ragdollParts)
        {
            if (part.TryGetComponent(out Collider collider))
                collider.isTrigger = true;

            if (part.TryGetComponent(out Rigidbody rb))
            {
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    #endregion


    //NOTE: Working on the transition of ragdoll to animation. The current issue is when sampling the animationclip,
    //there are still forces being applied to the rbs. Need to find a way to remove any force influence on them.
    //Those forces need to instead be placed on the core rigidbody (Honestly probably just needs to worry about the hips)

    //Once the force issue is resolved and we can sample an animation clip correctly, than we need to lerp to that position from the pos we start in.

    private async Task TransitionToAnimation()
    {
        ragdollStartTransform.Clear();
        ragdollEndTransform.Clear();

        float transitionTimer = 0f;

        AnimationClip clip = GetTransitionAnimationClip();

        if (clip != null)
        {
            clip.SampleAnimation(sceneObject.AnimationStateHandler.Animator.gameObject, 0);
            Debug.Log("RAGDOLL: Animation Clip " + clip.name + " Sampled");
        }                 

        while (transitionTimer < exitTransitionTime)
        {
            transitionTimer += Time.deltaTime;
            
            await Task.Yield();
        }
    }


    private AnimationClip GetTransitionAnimationClip()
    {
        foreach (AnimationClip clip in sceneObject.AnimationStateHandler.Animator.runtimeAnimatorController.animationClips)
        {

            //TODO: Need to fix dummy animations to match correct flow
            if (clip.name == "BaseIdle")
                return clip;

            //if (sceneObject.GroundedState == GroundedState.Airborn && clip.name == gameObject.name + "BaseAirIdle")
            //    return clip;

            //else if (clip.name == gameObject.name + "BaseIdle")
            //    return clip;
        }

        return null;
    }
}
