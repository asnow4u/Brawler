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
    [SerializeField] private float exGrowth = 2.73f;

    [Tooltip("The base acceleration of the object being launched before additional force is applied")]
    [SerializeField] private float baseAcceleration = 7f;

    [Tooltip("The angle at which the object will be launched")]
    [SerializeField] private float angle = 45;

    [SerializeField] private float maxXVelocity = 5f;
    [SerializeField] private float maxYVelocity = 5f;

    [SerializeField] private float decelerationXValue = 0.1f;
    [SerializeField] private float decelerationYValue = 0.1f;

    private void OnDrawGizmos()
    {
        if (enabled)
        {
            Vector3 startPoint = transform.position;

            // Define the range of damage values
            int[] damageValues = { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

            foreach (int damage in damageValues)
            {
                // Calculate the force
                float minForce = mass * baseAcceleration;
                float damageForce = minForce + influence * (Mathf.Pow(damage, exGrowth) / mass);

                // Calculate the launch angle
                float xLaunch = Mathf.Cos(angle * Mathf.Deg2Rad);
                float yLaunch = Mathf.Sin(angle * Mathf.Deg2Rad);

                // Calculate the initial velocity components
                float initialXVelocity = damageForce * xLaunch / mass;
                float initialYVelocity = damageForce * yLaunch / mass;

                // Time step for the simulation
                float timeStep = 0.1f;
                float time = 0f;

                // Gravity
                float gravity = Physics.gravity.y;

                // Previous point in the trajectory
                Vector3 previousPoint = startPoint;
                Vector3 apexPoint = startPoint;
                float maxY = float.MinValue;

                // Calculate the color based on the damage value
                float t = (damage - 10) / 90f; // Normalize to range [0, 1]
                Color currentColor = Color.Lerp(Color.green, Color.red, t);

                // Draw the trajectory
                while (true)
                {
                    // Calculate the position at the current time
                    float x = initialXVelocity * time;
                    float y = initialYVelocity * time + 0.5f * gravity * time * time;

                    // Break if the object hits the ground
                    if (y < 0)
                        break;

                    Vector3 currentPoint = startPoint + new Vector3(x, y, 0);

                    // Draw the line segment
                    Gizmos.color = currentColor;
                    Gizmos.DrawLine(previousPoint, currentPoint);

                    // Update the previous point and time
                    previousPoint = currentPoint;
                    time += timeStep;

                    // Check for the apex
                    if (y > maxY)
                    {
                        maxY = y;
                        apexPoint = currentPoint;
                    }

                    // Apply deceleration if velocities exceed max velocities
                    if (initialXVelocity > maxXVelocity)
                        initialXVelocity -= decelerationXValue * timeStep;
                    if (initialXVelocity < maxXVelocity)
                        initialXVelocity = maxXVelocity;


                    initialYVelocity += gravity * timeStep;

                    if (initialYVelocity > 0)
                        initialYVelocity -= decelerationYValue * timeStep;
                    else if (initialYVelocity < 0)
                        initialYVelocity += decelerationYValue * timeStep;

                    if (initialYVelocity < -maxYVelocity)
                        initialYVelocity = -maxYVelocity;
                }

                // Draw a sphere at the apex
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(apexPoint, 0.1f);
            }
        }
    }
}
