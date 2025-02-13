using Game.SceneObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmosLaunchForceViewer : MonoBehaviour
{
    [Header("Launch Properties")]
    
    [Tooltip("Percentage of the force to be applied")]
    [Range(0, 1)]
    [SerializeField] private float influence = 1;

    [Tooltip("The mass of the object being launched")]
    [SerializeField] private float mass = 100;

    [Tooltip("Define the exponental growth of the force")]
    [SerializeField] private float exGrowth = 2.8f;

    [Tooltip("The base acceleration of the object being launched before additional force is applied")]
    [SerializeField] private float baseAcceleration = 10f;

    [Tooltip("The angle at which the object will be launched")]
    [SerializeField] private float angle = 45;

    [SerializeField] private float maxXVelocity = 2.5f;
    [SerializeField] private float maxYVelocity = 2.5f;

    [SerializeField] private float decelerationXValue = 4f;
    [SerializeField] private float decelerationYValue = 2f;

    [SerializeField] private int numIterations = 100;

    private void OnDrawGizmos()
    {
        if (enabled)
        {
            try
            {
                Vector3 startPoint = transform.position;

                // Define the range of damage values
                int[] damageValues = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

                foreach (int damage in damageValues)
                {
                    //if (damage != 60)
                    //    continue;                               

                    // Calculate the force
                    float minForce = mass * baseAcceleration;
                    float damageForce = minForce + influence * (Mathf.Pow(damage, exGrowth) / mass);

                    // Calculate the launch angle
                    float xLaunch = Mathf.Cos(angle * Mathf.Deg2Rad);
                    float yLaunch = Mathf.Sin(angle * Mathf.Deg2Rad);

                    // Calculate the initial velocity components
                    float xVelocity = damageForce * xLaunch / mass;
                    float yVelocity = damageForce * yLaunch / mass;

                    // Previous point in the trajectory
                    Vector3 currentPoint = startPoint;
                    Vector3 previousPoint = startPoint;

                    // Calculate the color based on the damage value
                    float t = (damage - 10) / 90f; // Normalize to range [0, 1]
                    Color currentColor = Color.Lerp(Color.green, Color.red, t);
                
                    // Draw the trajectory
                    for(int i=0; i<numIterations; i++)
                    {
                        // Apply deceleration if velocities exceed max velocities
                        if (xVelocity > maxXVelocity)
                            xVelocity -= decelerationXValue * Time.fixedDeltaTime; ;
                        if (xVelocity < maxXVelocity)
                            xVelocity = maxXVelocity;


                        yVelocity += Physics.gravity.y * Time.fixedDeltaTime;

                        if (yVelocity > 0)
                            yVelocity -= decelerationYValue * Time.fixedDeltaTime;
                        //else if (yVelocity < 0)
                        //    yVelocity += decelerationYValue * Time.fixedDeltaTime;

                        if (yVelocity < -maxYVelocity)
                            yVelocity = -maxYVelocity;


                        currentPoint.x += xVelocity * Time.fixedDeltaTime;
                        currentPoint.y += yVelocity * Time.fixedDeltaTime;

                        if (currentPoint.y < startPoint.y)
                            break;

                        // Draw the line segment
                        Gizmos.color = currentColor;
                        Gizmos.DrawLine(previousPoint, currentPoint);

                        // Update the previous point and time
                        previousPoint = currentPoint;                    
                    }
                }
            }

            catch (System.Exception e)
            {
                Debug.LogException(e);
                enabled = false;
            }
        }
    }    
}
