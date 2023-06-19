using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public PlayerButtonMap input;

    public PlayerInputHandler()
    {
        input = new PlayerButtonMap();
        input.Enable();
    }

    public void DisableInputEvents()
    {
        input.Disable();
    }   
}
