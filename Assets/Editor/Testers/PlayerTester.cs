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

        GUILayout.Label("-------------Test-------------");

        if (GUILayout.Button("Cancel Movement And Jump"))
        {
            CancelMovementAndJumpTest(sceneObject);
        }


        if (GUILayout.Button("Cancel Movement And Attack"))
        {
            CancelMovementAndAttackTest(sceneObject);
        }

        if (GUILayout.Button("Jump and Movement Bug"))
            JumpMovementBug(sceneObject);


        if (GUILayout.Button("Perform UpTilt Attack"))
        {
            sceneObject.AttackInputHandler.PerformUpAttack();
        }

        if (GUILayout.Button("Perform ForwardTilt Attack"))
        {
            sceneObject.AttackInputHandler.PerformRightAttack();
        }

        if (GUILayout.Button("Perform DownTilt Attack"))
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
                movementHandler.PerformMovement(new Vector2(1, 0));
                frameCount++;
                await Task.Yield();
            }

            movementHandler.PerformMovement(new Vector2(0, 0));
            movementHandler.PerformJump(1);
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
                moveHandler.PerformMovement(new Vector2(1, 0));
                frameCount++;
                await Task.Yield();
            }

            attackHandler.PerformRightAttack();
            moveHandler.PerformMovement(new Vector2(0, 0));
        }
    }


    public async Task JumpMovementBug(SceneObject sceneObject)
    {
        if (sceneObject.TryGetComponent(out MovementInputHandler moveHandler))
        {
            moveHandler.PerformMovement(new Vector2(1, 0));                        
            await Task.Yield();
            
            moveHandler.PerformJump(1);
            await Task.Yield();
            
            moveHandler.PerformMovement(new Vector2(1, 0));
            await Task.Yield();            

            moveHandler.PerformMovement(Vector2.zero);
        }
    }
}