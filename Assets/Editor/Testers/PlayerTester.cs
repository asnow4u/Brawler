using Game.SceneObjects;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Movement;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Player))]
public class PlayerTester : Editor
{   
    public override void OnInspectorGUI()
    {
        SceneObject sceneObject = (SceneObject)target;
        
        DrawDefaultInspector();

        

        if (GUILayout.Button("Turn Around"))
        {
            sceneObject.TurnAround();
        }

        //Attack
        GUILayout.Label("---Attack---");
        if (GUILayout.Button("Perform Up Attack"))
        {
            sceneObject.AttackInputHandler.PerformUpAttack();
        }

        if (GUILayout.Button("Perform Forward Attack"))
        {
            sceneObject.AttackInputHandler.PerformRightAttack();
        }

        if (GUILayout.Button("Perform Down Attack"))
        {
            sceneObject.AttackInputHandler.PerformDownAttack();
        }       
    }


    public async Task CancelMovementAndJumpTest(SceneObject sceneObject)
    {
        if (sceneObject.TryGetComponent(out MovementInputHandler movementHandler))
        {
            int frameCount = 0;

            while (frameCount < 60)
            {
                movementHandler.SetMovementInfluence(new Vector2(1, 0));
                frameCount++;
                await Task.Yield();
            }

            movementHandler.SetMovementInfluence(new Vector2(0, 0));
            movementHandler.SetJumpInfluence(1);
        }
    }


    public async Task CancelMovementAndAttackTest(SceneObject sceneObject)
    {
        if (sceneObject.TryGetComponent(out MovementInputHandler moveHandler) &&
            sceneObject.TryGetComponent(out AttackInputHandler attackHandler))
        {
            int frameCount = 0;
            
            while (frameCount < 60)
            {
                moveHandler.SetMovementInfluence(new Vector2(1, 0));
                frameCount++;
                await Task.Yield();
            }

            attackHandler.PerformRightAttack();
            moveHandler.SetMovementInfluence(new Vector2(0, 0));
        }
    }


    public async Task JumpMovementBug(SceneObject sceneObject)
    {
        if (sceneObject.TryGetComponent(out MovementInputHandler moveHandler))
        {
            moveHandler.SetMovementInfluence(new Vector2(1, 0));                        
            await Task.Yield();
            
            moveHandler.SetJumpInfluence(1);
            await Task.Yield();
            
            moveHandler.SetMovementInfluence(new Vector2(1, 0));
            await Task.Yield();            

            moveHandler.SetMovementInfluence(Vector2.zero);
        }
    }
}