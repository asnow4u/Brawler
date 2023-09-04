using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTarget : MonoBehaviour
{
    public List<Transform> followTargets;
    public float yOffset;


    void FixedUpdate()
    {
        if (followTargets.Count > 1)
            FollowTargets(followTargets);

        else if (followTargets.Count == 1)
            FollowTarget(followTargets[0]);            
    }

    private void FollowTarget(Transform transform)
    {
        SetPosition(transform.position.x, transform.position.y);
    }


    /// <summary>
    /// Average X position based on multiple transforms
    /// Y position is based on first transform
    /// </summary>
    /// <param name="transforms"></param>
    private void FollowTargets(List<Transform> transforms)
    {
        float xPos = 0;

        foreach (Transform t in transforms)
        {
            xPos += t.transform.position.x;
        }

        xPos /= followTargets.Count;
        

        SetPosition(xPos, transforms[0].position.y);
    }


    private void SetPosition(float xPos, float yPos)
    {
        transform.position = new Vector3(xPos, yPos + yOffset, transform.position.z);
    }


    public void AddFollower(Transform transform)
    {

    }

    public void RemoveFollower(Transform transform)
    {

    }
}
