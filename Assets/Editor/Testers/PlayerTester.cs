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
}