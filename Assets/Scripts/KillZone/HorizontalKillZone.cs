using UnityEngine;

public class HorizontalKillZone : KillZone
{
    private const float OFFSCREENDISTANCE = 1.5f;

    public override void Initialize(KillZoneType type, Transform objTransform, GameObject deathVFX)
    {
        base.Initialize(type, objTransform, deathVFX);                       
        UpdatePosition();
    }


    protected override void Update()
    {
        UpdatePosition();
        base.Update();     
    }


    /// <summary>
    /// Update the position of the killzone based on the camera view
    /// </summary>
    private void UpdatePosition()
    {
        (Vector3 leftView, Vector3 rightView) cameraView = GetCameraViewport();

        if (type == KillZoneType.Left && transform.position.x > cameraView.leftView.x)
            transform.position = cameraView.leftView;

        else if (type == KillZoneType.Right && transform.position.x < cameraView.rightView.x)
            transform.position = cameraView.rightView;
    }


    /// <summary>
    /// Get the left and right side of the camera view
    /// </summary>
    private (Vector3, Vector3) GetCameraViewport()
    {
        Camera cam = Camera.main;
        float depth = Mathf.Abs(cam.transform.position.z);
        float cameraWidth = depth * Mathf.Tan((Camera.main.fieldOfView / 2) * Mathf.Deg2Rad) * Camera.main.aspect;

        Vector3 leftSide = new Vector3(cam.transform.position.x - cameraWidth, cam.transform.position.y, 0) - Vector3.one * OFFSCREENDISTANCE;
        Vector3 rightSide = new Vector3(cam.transform.position.x + cameraWidth, cam.transform.position.y, 0) + Vector3.one * OFFSCREENDISTANCE;

        return (leftSide, rightSide);
    }

}
