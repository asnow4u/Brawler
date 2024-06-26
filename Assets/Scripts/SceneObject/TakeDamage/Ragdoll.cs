using RayAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



public struct RagdollBone
{
    public Vector3 LocalPos;
    public Quaternion LocalRot;

    public RagdollBone(Vector3 pos, Quaternion rot)
    {
        this.LocalPos = pos;
        this.LocalRot = rot;
    }
}


public class Ragdoll : MonoBehaviour
{
    private bool isInitialized;
    private SceneObject sceneObject;    
    private Vector3 posOffset;
    
    private List<GameObject> ragdollParts = new List<GameObject>();

    //Exit transition
    private float exitTransitionTime = 0.5f;
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

        isInitialized = true;
    }

    #endregion


    #region Enable / Disable

    private void OnEnable()
    {
        if (isInitialized)
        {
            Debug.Log("RAGDOLL: Enabled");
            DisableSceneObjectComponents();
            EnableRagdollParts();
        }
    }


    private async void OnDisable()
    {
        if ( isInitialized)
        {            
            Debug.Log("RAGDOLL: Disabled");
            DisableRagdollParts();

            sceneObject.GetComponent<Collider>().enabled = true;

            await TransitionToAnimation();

            EnableSceneObjectComponenets();
        }
    }


    private void EnableSceneObjectComponenets()
    {
        if (sceneObject != null)
        {
            sceneObject.GetComponent<Collider>().enabled = true;
            sceneObject.CoreRigidBody.velocity = Vector3.zero;
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
                rb.isKinematic = false;
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
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    #endregion

    
    private async Task TransitionToAnimation()
    {
        ragdollStartTransform.Clear();
        ragdollEndTransform.Clear();

        PopulateRagdollBones(ref ragdollStartTransform);

        AnimationClip clip = GetTransitionAnimationClip();
        if (clip != null)
        {
            clip.SampleAnimation(sceneObject.AnimationStateHandler.Animator.gameObject, 0);     
            PopulateRagdollBones(ref ragdollEndTransform);
        }                 

        float transitionTimer = 0f;
        while (transitionTimer < exitTransitionTime)
        {
            for (int i = 0; i < ragdollParts.Count; i++)
            {                
                ragdollParts[i].transform.localPosition = Vector3.Lerp(ragdollStartTransform[i].LocalPos, ragdollEndTransform[i].LocalPos, transitionTimer / exitTransitionTime);
                RagdollParts[i].transform.localRotation = Quaternion.Lerp(ragdollStartTransform[i].LocalRot, ragdollEndTransform[i].LocalRot, transitionTimer / exitTransitionTime);
            }
            
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


    private void PopulateRagdollBones(ref List<RagdollBone> ragdollBoneList)
    {
        foreach (GameObject part in ragdollParts)
        {
            RagdollBone bone = new RagdollBone(part.transform.localPosition, part.transform.localRotation);
            ragdollBoneList.Add(bone);
        }
    }
}




/*NOTE:
 *  Need to have lerping pos move with root
 *  Forces need to be transfered to sceneobject on disable
 *  
 *  When disabled the ragdoll moves before lerping
*/