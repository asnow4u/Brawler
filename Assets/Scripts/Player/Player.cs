using UnityEngine;
using UnityEngine.InputSystem;


public class Player : SceneObject
{
    private float numLives;
    private float numDeaths;

    private PlayerInputHandler inputHandler;

    protected override void Initialize()
    {
        base.Initialize();

        ObjectType = SceneObjectType.Player;
                    
        InitializeInput(); 
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

        //Interact
        inputHandler.input.PlayerActions.Interaction.performed += InteractInput;

        //Weapon Switch
        inputHandler.input.PlayerActions.WeaponSwitch1.performed += SwitchWeapon1;
        inputHandler.input.PlayerActions.WeaponSwitch2.performed += SwitchWeapon2;
        inputHandler.input.PlayerActions.WeaponSwitch3.performed += SwitchWeapon3;
        inputHandler.input.PlayerActions.WeaponSwitch4.performed += SwitchWeapon4;
        inputHandler.input.PlayerActions.WeaponSwitch5.performed += SwitchWeapon5;
    }


    private void OnDisable()
    {
        inputHandler.DisableInputEvents();
    }


    #region Movement Input Events

    private void HorizontalMovementInput(InputAction.CallbackContext obj)
    {
        movementInputHandler.PerformMovement(obj.ReadValue<Vector2>());
    }

    private void MovementCanceled(InputAction.CallbackContext obj)
    {
        movementInputHandler.PerformMovement(Vector2.zero);
    }

    private void JumpInput(InputAction.CallbackContext obj)
    {
        movementInputHandler.PerformJump(obj.ReadValue<float>());
    }

    private void JumpCanceled(InputAction.CallbackContext obj)
    {
        //TODO: Determine if needed, can add to interface
    }
    #endregion

    #region Attack Input Events

    private void AttackUpwardInput(InputAction.CallbackContext obj)
    {
        attackInputHandler.PerformUpAttack();
    }

    private void AttackDownwardInput(InputAction.CallbackContext obj)
    {
        attackInputHandler.PerformDownAttack();
    }

    private void AttackLeftInput(InputAction.CallbackContext obj)
    {
        attackInputHandler.PerformLeftAttack();
    }

    private void AttackRightInput(InputAction.CallbackContext obj)
    {
        attackInputHandler.PerformRightAttack();
    }

    #endregion

    #region Interaction Input Events

    private void InteractInput(InputAction.CallbackContext obj)
    {
        InteractionHandler.ReceiveInput(this);
    }

    #endregion

    #region Weapon Selection Input

    private void SwitchWeaponTo(int index)
    {
        if (StateHandler.ChangeState(ActionState.State.Moving))
        {
            EquipmentHandler.Weapons.SwapWeaponTo(index);
        }
    }

    private void SwitchWeapon1(InputAction.CallbackContext obj)
    {
        SwitchWeaponTo(0);
    }

    private void SwitchWeapon2(InputAction.CallbackContext obj)
    {
        SwitchWeaponTo(1);
    }

    private void SwitchWeapon3(InputAction.CallbackContext obj)
    {
        SwitchWeaponTo(2);
    }

    private void SwitchWeapon4(InputAction.CallbackContext obj)
    {
        SwitchWeaponTo(3);
    }

    private void SwitchWeapon5(InputAction.CallbackContext obj)
    {
        SwitchWeaponTo(4);
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

