using UnityEngine;

internal partial class MovementHandler
{
    #region Grounded Movement

    /// <summary>
    /// Accelerate on the ground by an acceleration value to a max velocity from <see cref="currentMovementCollection"/>
    /// </summary>
    private void UpdateGroundedAcceleration()
    {
        float currentX = rb.linearVelocity.x;

        // 1. Kill momentum if reversing
        if (horizontalInfluence != 0 && Mathf.Sign(horizontalInfluence) != Mathf.Sign(currentX))
        {
            currentX = 0;
        }

        // 2. Apply acceleration
        currentX += horizontalInfluence * curMovementData.GroundedAcceleration * Time.fixedDeltaTime;

        // 3. Clamp
        float maxSpeed = curMovementData.MaxGroundedVelocity;
        currentX = Mathf.Clamp(currentX, -maxSpeed, maxSpeed);

        rb.linearVelocity = new Vector3(currentX, rb.linearVelocity.y, 0);

        if (sceneObject.IsFacingRightDirection && rb.linearVelocity.x < 0 ||
            !sceneObject.IsFacingRightDirection && rb.linearVelocity.x > 0)
        {
            sceneObject.TurnAround();
        }
    }

    /// <summary>
    /// Deccelerate grounded movement based on base deccelration value and the sceneObject's mass.
    /// </summary>
    private void DeccelerateGroundedMovement()
    {
        //Positive Decceleration
        if (rb.linearVelocity.x > 0)
        {
            float decceleratedXValue = rb.linearVelocity.x - curMovementData.GroundedDecceleration * Time.fixedDeltaTime;

            if (decceleratedXValue < 0)
                decceleratedXValue = 0;

            rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
        }

        //Negative Decceleration
        else if (rb.linearVelocity.x < 0)
        {
            float decceleratedXValue = rb.linearVelocity.x + curMovementData.GroundedDecceleration * Time.fixedDeltaTime;

            if (decceleratedXValue > 0)
                decceleratedXValue = 0;

            rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
        }
    }

    #endregion


    #region Aerial Movement

    /// <summary>
    /// Accelerate in the air on the X axis
    /// </summary>
    private void AccelerateAerialXMovement()
    {
        float maxXVelocity = curMovementData.MaxAerialXVelocity;
        float acceleration = curMovementData.AerialXAcceleration;

        //Positive Acceleration
        if (horizontalInfluence > 0)
        {
            float acceleratedXValue = rb.linearVelocity.x + (acceleration * Time.fixedDeltaTime);

            if (acceleratedXValue > maxXVelocity)
                acceleratedXValue = maxXVelocity;

            rb.linearVelocity = new Vector3(acceleratedXValue, rb.linearVelocity.y, 0);
        }

        //Negative Acceleration
        else if (horizontalInfluence < 0)
        {
            float acceleratedXValue = rb.linearVelocity.x - (acceleration * Time.fixedDeltaTime);

            if (acceleratedXValue < -maxXVelocity)
                acceleratedXValue = -maxXVelocity;

            rb.linearVelocity = new Vector3(acceleratedXValue, rb.linearVelocity.y, 0);
        }
    }

    /// <summary>
    /// Accelerate in the air on the Y axis
    /// </summary>
    private void AccelerateAerialYMovement()
    {
        float maxYVelocity = curMovementData.MaxAerialYVelocity;
        float acceleration = curMovementData.AerialYAcceleration;

        float acceleratedYValue = rb.linearVelocity.y - (acceleration * Time.fixedDeltaTime);

        if (acceleratedYValue < -maxYVelocity)
            acceleratedYValue = -maxYVelocity;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, acceleratedYValue, 0);
    }

    /// <summary>
    /// Deccelerate in the air on the X axis 
    /// </summary>
    private void DeccelerateAerialXMovement()
    {
        //Only deccelerate if velocity is greater than max velocity
        if (Mathf.Abs(rb.linearVelocity.x) > curMovementData.MaxAerialXVelocity)
        {
            //Positive Decceleration
            if (rb.linearVelocity.x > 0)
            {
                float decceleratedXValue = rb.linearVelocity.x - curMovementData.AerialXDecceleration * Time.fixedDeltaTime;

                if (decceleratedXValue < 0)
                    decceleratedXValue = 0;

                rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
            }

            //Negative Decceleration
            else if (rb.linearVelocity.x < 0)
            {
                float decceleratedXValue = rb.linearVelocity.x + curMovementData.AerialXDecceleration * Time.fixedDeltaTime;

                if (decceleratedXValue > 0)
                    decceleratedXValue = 0;

                rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
            }
        }
    }

    /// <summary>
    /// Deccelerate in the air on the Y axis 
    /// </summary>
    private void DeccelerateAerialYMovement()
    {
        if (Mathf.Abs(rb.linearVelocity.y) > curMovementData.MaxAerialYVelocity)
        {
            //Positive Decceleration
            if (rb.linearVelocity.y > 0)
            {
                float decceleratedYValue = rb.linearVelocity.y - curMovementData.AerialUpYDecceleration * Time.fixedDeltaTime;

                if (decceleratedYValue < 0)
                    decceleratedYValue = 0;

                rb.linearVelocity = new Vector3(rb.linearVelocity.x, decceleratedYValue, 0);
            }

            //Negative Decceleration
            else if (rb.linearVelocity.y < 0)
            {
                float decceleratedYValue = rb.linearVelocity.y + curMovementData.AerialDownYDecceleration * Time.fixedDeltaTime;

                if (decceleratedYValue > 0)
                    decceleratedYValue = 0;

                rb.linearVelocity = new Vector3(rb.linearVelocity.x, decceleratedYValue, 0);
            }
        }

        //Gravity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0);
    }    

    #endregion
}
