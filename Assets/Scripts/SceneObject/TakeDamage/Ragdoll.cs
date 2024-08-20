using RayAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditorInternal;
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
    public Rigidbody RB => GetComponent<Rigidbody>();


    #region Initialize

    public void Initialize(SceneObject sceneObject)
    {        
        this.sceneObject = sceneObject;
        posOffset = gameObject.transform.localPosition;
        
        //Root
        gameObject.layer = LayerMask.NameToLayer("Ragdoll");
        ragdollParts.Add(gameObject);

        //Parts
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
        Debug.Log("RAGDOLL: Enabled");
        
        if (isInitialized)
        {
            EnableRagdollParts();           
            DisableSceneObjectComponents();
        }
    }


    private async void OnDisable()
    {
        Debug.Log("RAGDOLL: Disabled");           
        
        if (isInitialized)
        {            
            sceneObject.GetComponent<Collider>().enabled = true;
            sceneObject.CoreRigidBody.isKinematic = false;
            sceneObject.CoreRigidBody.velocity = RB.velocity;                 

            DisableRagdollParts();

            await TransitionToAnimation();

            EnableSceneObjectComponenets();
        }
    }


    private void EnableSceneObjectComponenets()
    {
        if (sceneObject != null)
        {
            sceneObject.GetComponent<Collider>().enabled = true;                        
            sceneObject.AnimationHandler.Animator.enabled = true;
        }        
    }


    private void DisableSceneObjectComponents()
    {
        if (sceneObject != null)
        {
            sceneObject.CoreRigidBody.isKinematic = true;
            sceneObject.GetComponent<Collider>().enabled = false;
            sceneObject.AnimationHandler.Animator.enabled = false;
        }
    }


    private void EnableRagdollParts()
    {
        foreach (GameObject part in ragdollParts)
        {
            if (part.TryGetComponent(out Collider collider))
                collider.isTrigger = false;

            if (part.TryGetComponent(out Rigidbody rb))
                rb.velocity = sceneObject.CoreRigidBody.velocity;            

            RB.isKinematic = false;            
        }
    }


    private void DisableRagdollParts()
    {
        foreach (GameObject part in ragdollParts)
        {
            if (part.TryGetComponent(out Collider collider))
                collider.isTrigger = true;
            
            RB.isKinematic = true;            
        }
    }

    #endregion


    /// <summary>
    /// Determine if a bounce is going to occure and adjust velocity accordingly
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="bounceDegrade"></param>
    public void CheckRagdollBounce(Bounds bounds, float bounceDegrade)
    {
        Vector3 velocity = RB.velocity;
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        Vector3 direction = velocity.normalized;

        if (Physics.BoxCast(bounds.center, bounds.extents, direction, out RaycastHit hit, Quaternion.identity, distance, LayerMask.GetMask("Environment")))
        {
            Vector3 bounceVelocity = Vector3.Reflect(velocity, hit.normal) * bounceDegrade;

            foreach (GameObject part in ragdollParts)
            {
                if (part.TryGetComponent(out Rigidbody rigidbody))
                    rigidbody.velocity = bounceVelocity;
            }            
        }
    }



    private async Task TransitionToAnimation()
    {
        ragdollStartTransform.Clear();
        ragdollEndTransform.Clear();

        PopulateRagdollBones(ref ragdollStartTransform);

        AnimationClip clip = GetTransitionAnimationClip();
        if (clip != null)
        {
            clip.SampleAnimation(sceneObject.AnimationHandler.Animator.gameObject, 0);     
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
        //TODO: Need to fix dummy animations to match correct flow

        if (sceneObject.GroundedState == GroundedState.Grounded)
            return sceneObject.AnimationHandler.GroundIdleAnimation;
        else
            return sceneObject.AnimationHandler.AirIdleAnimation;            
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