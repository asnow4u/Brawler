using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class Player : SceneObject
{
    private float numLives;
    private float numDeaths;

    private PlayerInputHandler inputHandler;
    private IMovement movementHandler;
    private IAttack attackHandler;


    //CurrentEquipment


    protected override void Initialize()
    {
        base.Initialize();
            
        
        InitializeInput();
        InitializeMovement();
        InitializeAttack();
    }

    private void InitializeInput()
    {
        inputHandler = new PlayerInputHandler();

        inputHandler.input.PlayerActions.Movement.performed += HorizontalMovementInput;
        inputHandler.input.PlayerActions.Movement.canceled += MovementCanceled;

        inputHandler.input.PlayerActions.Jump.performed += JumpInput;
        inputHandler.input.PlayerActions.Jump.canceled += JumpCanceled;

        //inputHandler.input.PlayerActions.Couch.performed += Couch_performed;
        //inputHandler.input.PlayerActions.Couch.canceled += Couch_canceled;

        //Attack
        inputHandler.input.PlayerActions.RightAttack.performed += AttackRightInput;
        inputHandler.input.PlayerActions.LeftAttack.performed += AttackLeftInput;
        inputHandler.input.PlayerActions.DownAttack.performed += AttackDownwardInput;
        inputHandler.input.PlayerActions.UpAttack.performed += AttackUpwardInput;
    }

    private void InitializeMovement()
    {
        movementHandler = GetComponent<IMovement>();
        movementHandler.SetUp(this);
        
        //TODO: Will need to set the movement collection based on the cur weapon
        movementHandler.SetCollection(transform.GetChild(0).gameObject.GetComponent<MovementCollection>());
    }

    private void InitializeAttack()
    {
        attackHandler = GetComponent<IAttack>();
        attackHandler.Setup(this);

        //TODO: SetWeapon with cur weapon
        
    }


    private void OnDisable()
    {
        inputHandler.DisableInputEvents();
    }


    #region Movement Input Events

    private void HorizontalMovementInput(InputAction.CallbackContext obj)
    {
        movementHandler.PerformMovement(obj.ReadValue<Vector2>());
    }

    private void MovementCanceled(InputAction.CallbackContext obj)
    {
        movementHandler.PerformMovement(new Vector2(0, 0));
    }

    private void JumpInput(InputAction.CallbackContext obj)
    {
        movementHandler.PerformJump();
    }

    private void JumpCanceled(InputAction.CallbackContext obj)
    {
        //TODO: Determine if needed, can add to interface
    }
    #endregion

    #region Attack Input Events

    private void AttackUpwardInput(InputAction.CallbackContext obj)
    {
        attackHandler.PerformUpAttack();
    }

    private void AttackDownwardInput(InputAction.CallbackContext obj)
    {
        attackHandler.PerformDownAttack();
    }

    private void AttackLeftInput(InputAction.CallbackContext obj)
    {
        attackHandler.PerformLeftAttack();
    }

    private void AttackRightInput(InputAction.CallbackContext obj)
    {
        attackHandler.PerformRightAttack();
    }

    #endregion



    //public float bounceDampener;
    //public float stallTimer;

    //TODO: move this to platforms
    //private void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Platforms"))
    //    {
    //        if (stallTimer > 0) 
    //        {

    //            //F = m(v/t) || t=1
    //            Vector3 bounceForce = Vector3.Reflect(-1 * collision.relativeVelocity, collision.transform.up) * rb.mass;

    //            //Debug.Log("Bounce");
    //            //Debug.Log(bounceForce);

    //            rb.AddForce(bounceForce * bounceDampener, ForceMode.Impulse);
    //        }
    //    }
    //}
}

