using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{    
    private const float minVelocity = 20f;
    private bool isRightDependent;
    private string uniqueObjID;
    private event Action<SceneObject> destroyEvent;

    public void SetUp(bool isRightDependent, bool isSolid = false, string objID = null, Action<SceneObject> destroyListener = null) 
    {
        GetComponent<Collider>().isTrigger = !isSolid;
        this.isRightDependent = isRightDependent;
        this.uniqueObjID = objID;

        if (destroyListener != null)
            destroyEvent += destroyListener;
    }


    private void Update()
    {
        Tuple<Vector3, Vector3> cameraView = GetCameraViewport();

        if (isRightDependent && transform.position.x < cameraView.Item2.x) 
        {
            transform.position = cameraView.Item2;            
        }

        else if (!isRightDependent && transform.position.x > cameraView.Item1.x)
        {
            transform.position = cameraView.Item1;
        }            
    }


    //NOTE: This should probably be moved to a diffent script dealing with the cameras
    private Tuple<Vector3, Vector3> GetCameraViewport()
    {
        Camera cam = Camera.main;
        float depth = Mathf.Abs(cam.transform.position.z);
        float cameraWidth = depth * Mathf.Tan((Camera.main.fieldOfView / 2) * Mathf.Deg2Rad) * Camera.main.aspect;

        Vector3 leftSide = new Vector3(cam.transform.position.x - cameraWidth, cam.transform.position.y, 0);
        Vector3 rightSide = new Vector3(cam.transform.position.x + cameraWidth, cam.transform.position.y, 0);

        return new Tuple<Vector3, Vector3>(leftSide, rightSide);
    }


    private void OnCollisionEnter(Collision col)
    {
        if (col.transform.TryGetComponent(out SceneObject sceneObj))
        {
            CollisionWithSceneObject(sceneObj);
        }
    }


    private void OnTriggerEnter(Collider col)
    {
        if (col.transform.TryGetComponent(out SceneObject sceneObj))
        {
            CollisionWithSceneObject(sceneObj);
        }
    }


    private void CollisionWithSceneObject(SceneObject sceneObject)
    {
        if (uniqueObjID == null || sceneObject.UniqueId == uniqueObjID)
        {
            Debug.Log("Kill Velocity: " + sceneObject.rb.velocity.magnitude, gameObject);
            
            if (sceneObject.rb.velocity.magnitude > minVelocity)
            {                
                destroyEvent?.Invoke(sceneObject);
                Destroy(sceneObject.gameObject);
            }
        }
    }
}
