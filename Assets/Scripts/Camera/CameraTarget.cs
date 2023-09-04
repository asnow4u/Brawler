using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public List<Transform> Targets;
    public float yOffset;


    void FixedUpdate()
    {
        if (Targets.Count > 1)
            FollowMultipleTargets(Targets);

        else if (Targets.Count == 1)
            FollowSingleTarget(Targets[0]);            
    }

    private void FollowSingleTarget(Transform transform)
    {
        SetPosition(transform.position.x, transform.position.y);
    }


    /// <summary>
    /// Average X position based on multiple transforms
    /// Y position is based on first transform
    /// </summary>
    /// <param name="transforms"></param>
    private void FollowMultipleTargets(List<Transform> transforms)
    {
        float xPos = 0;

        foreach (Transform t in transforms)
        {
            xPos += t.transform.position.x;
        }

        xPos /= Targets.Count;
        

        SetPosition(xPos, transforms[0].position.y);
    }


    private void SetPosition(float xPos, float yPos)
    {
        transform.position = new Vector3(xPos, yPos + yOffset, transform.position.z);
    }
}
